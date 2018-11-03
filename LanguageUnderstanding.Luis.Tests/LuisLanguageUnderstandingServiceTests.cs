// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace LanguageUnderstanding.Luis.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using FluentAssertions;
    using LanguageUnderstanding.Luis;
    using Models;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using NUnit.Framework;

    /// <summary>
    /// Test suite for <see cref="LuisEntity"/> class.
    /// </summary>
    [TestFixture]
    internal class LuisLanguageUnderstandingServiceTests
    {
        /// <summary>
        /// Compares two <see cref="LuisEntity"/> instances.
        /// </summary>
        private readonly LuisEntityComparer luisEntityComparer = new LuisEntityComparer();

        /// <summary>
        /// Compares two <see cref="LuisLabeledUtterance"/> instances.
        /// </summary>
        private readonly LuisLabeledUtteranceComparer luisLabeledUtteranceComparer = new LuisLabeledUtteranceComparer();

        /* LuisEntity Tets. */

        [Test]
        public void ArgumentNullChecks()
        {
            Action nullAppName = () => new LuisLanguageUnderstandingService(null, string.Empty, string.Empty, string.Empty, string.Empty);
            Action nullAppId = () => new LuisLanguageUnderstandingService(string.Empty, null, string.Empty, string.Empty, string.Empty);
            Action nullAppVersion = () => new LuisLanguageUnderstandingService(string.Empty, string.Empty, null, string.Empty, string.Empty);
            Action nullRegion = () => new LuisLanguageUnderstandingService(string.Empty, string.Empty, string.Empty, null, string.Empty);
            Action nullAuthoringKey = () => new LuisLanguageUnderstandingService(string.Empty, string.Empty, string.Empty, string.Empty, default(string));
            Action nullLuisClient = () => new LuisLanguageUnderstandingService(string.Empty, string.Empty, string.Empty, string.Empty, default(ILuisClient));
            nullAppName.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("appName");
            nullAppId.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("appId");
            nullAppVersion.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("appVersion");
            nullRegion.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("region");
            nullAuthoringKey.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("authoringKey");
            nullLuisClient.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("luisClient");

            using (var luis = new LuisLanguageUnderstandingService(string.Empty, string.Empty, string.Empty, string.Empty, string.Empty))
            {
                Func<Task> nullUtterances = () => luis.TrainAsync(null, Array.Empty<EntityType>());
                Func<Task> nullEntityTypes = () => luis.TrainAsync(Array.Empty<LabeledUtterance>(), null);
                nullUtterances.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("utterances");
                nullEntityTypes.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("entityTypes");
            }
        }

        [Test]
        public void LuisEntityInitializes()
        {
            string entityType = "Location";
            string entityValue = "City";
            int startCharIndex = 2;
            int endCharIndex = 8;
            LuisEntity luisEntity = new LuisEntity(entityType, entityValue, startCharIndex, endCharIndex);
            luisEntity.EntityName.Should().Be("Location::City");
            luisEntity.StartCharIndex.Should().Be(startCharIndex);
            luisEntity.EndCharIndex.Should().Be(endCharIndex);
        }

        [Test]
        public void LuisEntitySerializes()
        {
            string expected = "{\"entity\":\"Location::City\",\"startCharIndex\":2,\"endCharIndex\":8}";
            LuisEntity luisEntity = new LuisEntity("Location", "City", 2, 8);
            string actual = JsonConvert.SerializeObject(luisEntity);
            actual.Should().Be(expected);
        }

        [Test]
        public void LuisEntityDeserializes()
        {
            LuisEntity expected = new LuisEntity("Location", "City", 2, 8);
            string json = "{\"entity\":\"Location::City\",\"startCharIndex\":2,\"endCharIndex\":8}";
            LuisEntity actual = JsonConvert.DeserializeObject<LuisEntity>(json);
            this.luisEntityComparer.Equals(actual, expected).Should().BeTrue();
        }

        [Test]
        public void LuisEntityListSerializes()
        {
            string expected = "[{\"entity\":\"Location::City\",\"startCharIndex\":2,\"endCharIndex\":8}]";
            List<LuisEntity> luisEntities = new List<LuisEntity>();
            luisEntities.Add(new LuisEntity("Location", "City", 2, 8));
            string actual = JsonConvert.SerializeObject(luisEntities);
            actual.Should().Be(expected);
        }

        [Test]
        public void LuisEntityListDeserializes()
        {
            List<LuisEntity> expected = new List<LuisEntity>();
            expected.Add(new LuisEntity("Location", "City", 2, 8));
            string json = "[{\"entity\":\"Location::City\",\"startCharIndex\":2,\"endCharIndex\":8}]";
            IReadOnlyList<LuisEntity> actual = JsonConvert.DeserializeObject<IReadOnlyList<LuisEntity>>(json);
            actual.Should().BeEquivalentTo(expected);
        }

        [Test]
        public void EntityConvertsToLuisEntity()
        {
            string utterance = "Engineer is the job I want!";
            Entity entity = new Entity("String", string.Empty, "Engineer", 0);
            LuisEntity expected = new LuisEntity("String", 0, 7);
            LuisEntity actual = LuisEntity.FromEntity(entity, utterance);
            this.luisEntityComparer.Equals(actual, expected).Should().BeTrue();
        }

        /* LuisLabeledUtteranceTests */

        [Test]
        public void LuisLabeledUtteranceInitializes()
        {
            string text = "I want to fly from New York to Seattle";
            string intent = "bookFlight";
            List<LuisEntity> luisEntities = new List<LuisEntity>();
            luisEntities.Add(new LuisEntity("Location::From", 20, 27));
            luisEntities.Add(new LuisEntity("Location::To", 32, 38));
            LuisLabeledUtterance luisLabeledUtterance = new LuisLabeledUtterance(text, intent, luisEntities);
            luisLabeledUtterance.Text.Should().Be(text);
            luisLabeledUtterance.Intent.Should().Be(intent);
            luisLabeledUtterance.LuisEntities.Should().BeEquivalentTo(luisEntities);
        }

        [Test]
        public void LuisLabeledUtteranceSerializes()
        {
            string expected = "{\"text\":\"My name is Bill Gates.\",\"intent\":\"updateName\"," +
                "\"entities\":[{\"entity\":\"Name::First\",\"startCharIndex\":11,\"endCharIndex\":" +
                "14},{\"entity\":\"Name::Last\",\"startCharIndex\":16,\"endCharIndex\":20}]}";
            string text = "My name is Bill Gates.";
            string intent = "updateName";
            List<LuisEntity> luisEntities = new List<LuisEntity>();
            luisEntities.Add(new LuisEntity("Name::First", 11, 14));
            luisEntities.Add(new LuisEntity("Name::Last", 16, 20));
            LuisLabeledUtterance luisLabeledUtterance = new LuisLabeledUtterance(text, intent, luisEntities);
            string actual = JsonConvert.SerializeObject(luisLabeledUtterance);
            actual.Should().Be(expected);
        }

        [Test]
        public void LuisLabeledUtteranceDeserializes()
        {
            string text = "My name is Bill Gates.";
            string intent = "updateName";
            List<LuisEntity> luisEntities = new List<LuisEntity>();
            luisEntities.Add(new LuisEntity("Name::First", 11, 14));
            luisEntities.Add(new LuisEntity("Name::Last", 16, 20));
            LuisLabeledUtterance expected = new LuisLabeledUtterance(text, intent, luisEntities);
            string json = "{\"text\":\"My name is Bill Gates.\",\"intent\":\"updateName\"," +
                "\"entityLabels\":[{\"entityName\":\"Name::First\",\"startCharIndex\":11,\"endCharIndex\":" +
                "14},{\"entityName\":\"Name::Last\",\"startCharIndex\":16,\"endCharIndex\":20}]}";
            LuisLabeledUtterance actual = JsonConvert.DeserializeObject<LuisLabeledUtterance>(json);
            this.luisLabeledUtteranceComparer.Equals(actual, expected).Should().BeTrue();
        }

        [Test]
        public void LabeledUtteranceToLuisLabeledUtterance()
        {
            string text = "My name is Bill Gates.";
            string intent = "updateName";
            List<LuisEntity> luisEntities = new List<LuisEntity>();
            luisEntities.Add(new LuisEntity("Name::First", 11, 14));
            luisEntities.Add(new LuisEntity("Name::Last", 16, 20));
            LuisLabeledUtterance expected = new LuisLabeledUtterance(text, intent, luisEntities);

            List<Entity> entities = new List<Entity>();
            entities.Add(new Entity("Name::First", string.Empty, "Bill", 0));
            entities.Add(new Entity("Name::Last", string.Empty, "Gates", 0));
            LabeledUtterance labeledUtterance = new LabeledUtterance(text, intent, entities);
            LuisLabeledUtterance actual = new LuisLabeledUtterance(labeledUtterance);
            this.luisLabeledUtteranceComparer.Equals(actual, expected).Should().BeTrue();
        }

        [Test]
        public void LuisLabeledUtteranceListSerializes()
        {
            string expected = "[{\"text\":\"My name is Bill Gates.\",\"intent\":\"updateName\"," +
                "\"entities\":[{\"entity\":\"Name::First\",\"startCharIndex\":11,\"endCharIndex\":" +
                "14},{\"entity\":\"Name::Last\",\"startCharIndex\":16,\"endCharIndex\":20}]}]";
            string text = "My name is Bill Gates.";
            string intent = "updateName";
            List<LuisEntity> luisEntities = new List<LuisEntity>();
            luisEntities.Add(new LuisEntity("Name::First", 11, 14));
            luisEntities.Add(new LuisEntity("Name::Last", 16, 20));
            List<LuisLabeledUtterance> luisLabeledUtterances = new List<LuisLabeledUtterance>();
            luisLabeledUtterances.Add(new LuisLabeledUtterance(text, intent, luisEntities));
            string actual = JsonConvert.SerializeObject(luisLabeledUtterances);
            actual.Should().Be(expected);
        }

        [Test]
        public void LuisLabeledUtteranceListDeserializes()
        {
            string text = "My name is Bill Gates.";
            string intent = "updateName";
            List<LuisEntity> luisEntities = new List<LuisEntity>();
            luisEntities.Add(new LuisEntity("Name::First", 11, 14));
            luisEntities.Add(new LuisEntity("Name::Last", 16, 20));
            List<LuisLabeledUtterance> expected = new List<LuisLabeledUtterance>();
            expected.Add(new LuisLabeledUtterance(text, intent, luisEntities));
            string json = "[{\"text\":\"My name is Bill Gates.\",\"intent\":\"updateName\"," +
                "\"entityLabels\":[{\"entityName\":\"Name::First\",\"startCharIndex\":11,\"endCharIndex\":" +
                "14},{\"entityName\":\"Name::Last\",\"startCharIndex\":16,\"endCharIndex\":20}]}]";
            List<LuisLabeledUtterance> actual = JsonConvert.DeserializeObject<List<LuisLabeledUtterance>>(json);
            actual.Should().BeEquivalentTo(expected);
        }

        [Test]
        public async Task TrainEmptyModel()
        {
            var appName = string.Empty;
            var appId = Guid.NewGuid().ToString();
            var versionId = string.Empty;
            var region = "westus";
            var importUri = $"https://{region}.api.cognitive.microsoft.com/luis/api/v2.0/apps/{appId}/versions/import?versionId={versionId}";
            var trainUri = $"https://{region}.api.cognitive.microsoft.com/luis/api/v2.0/apps/{appId}/versions/{versionId}/train";
            var mockClient = new MockLuisClient();
            using (var luisService = new LuisLanguageUnderstandingService(appName, appId, versionId, region, mockClient))
            {
                var utterances = Enumerable.Empty<LabeledUtterance>();
                var entityTypes = Enumerable.Empty<EntityType>();
                await luisService.TrainAsync(utterances, entityTypes);

                // Assert correct import request
                var importRequest = mockClient.Requests.FirstOrDefault(request => request.Uri == importUri);
                importRequest.Should().NotBeNull();
                var jsonBody = JObject.Parse(importRequest.Body);

                // Expects 3 intents
                var intents = jsonBody.SelectToken(".intents").As<JArray>();
                intents.Count.Should().Be(1);
                intents.FirstOrDefault(token => token.Value<string>("name") == "None").Should().NotBeNull();

                // Assert train request
                var trainRequest = mockClient.Requests.FirstOrDefault(request => request.Uri == trainUri);
                trainRequest.Should().NotBeNull();
            }
        }

        [Test]
        public async Task TrainModelWithUtterances()
        {
            var appName = string.Empty;
            var appId = Guid.NewGuid().ToString();
            var versionId = string.Empty;
            var region = "westus";
            var importUri = $"https://{region}.api.cognitive.microsoft.com/luis/api/v2.0/apps/{appId}/versions/import?versionId={versionId}";
            var mockClient = new MockLuisClient();
            using (var luisService = new LuisLanguageUnderstandingService(appName, appId, versionId, region, mockClient))
            {
                var utterances = new[]
                {
                    new LabeledUtterance("Book me a flight.", "BookFlight", new Entity[] { }),
                    new LabeledUtterance("Cancel my flight.", "CancelFlight", new Entity[] { })
                };

                var entityTypes = Enumerable.Empty<EntityType>();
                await luisService.TrainAsync(utterances, entityTypes);

                await luisService.TrainAsync(utterances, entityTypes);

                // Assert correct import request
                var importRequest = mockClient.Requests.FirstOrDefault(request => request.Uri == importUri);
                importRequest.Should().NotBeNull();
                var jsonBody = JObject.Parse(importRequest.Body);

                // Expects 3 intents
                var intents = jsonBody.SelectToken(".intents").As<JArray>();
                intents.Count.Should().Be(3);
                intents.FirstOrDefault(token => token.Value<string>("name") == "None").Should().NotBeNull();
                intents.FirstOrDefault(token => token.Value<string>("name") == utterances[0].Intent).Should().NotBeNull();
                intents.FirstOrDefault(token => token.Value<string>("name") == utterances[1].Intent).Should().NotBeNull();

                // Expect 2 utterances
                var importUtterances = jsonBody.SelectToken(".utterances").As<JArray>();
                importUtterances.Count.Should().Be(2);

                var bookUtterance = importUtterances.FirstOrDefault(token => token.Value<string>("intent") == utterances[0].Intent);
                bookUtterance.Should().NotBeNull();
                bookUtterance.Value<string>("text").Should().Be(utterances[0].Text);
                bookUtterance.SelectToken(".entities").As<JArray>().Count.Should().Be(0);

                var cancelUtterance = importUtterances.FirstOrDefault(token => token.Value<string>("intent") == utterances[1].Intent);
                cancelUtterance.Should().NotBeNull();
                cancelUtterance.Value<string>("text").Should().Be(utterances[1].Text);
                cancelUtterance.SelectToken(".entities").As<JArray>().Count.Should().Be(0);
            }
        }

        [Test]
        public async Task TrainModelWithUtterancesAndSimpleEntities()
        {
            var appName = string.Empty;
            var appId = Guid.NewGuid().ToString();
            var versionId = string.Empty;
            var region = "westus";
            var importUri = $"https://{region}.api.cognitive.microsoft.com/luis/api/v2.0/apps/{appId}/versions/import?versionId={versionId}";
            var mockClient = new MockLuisClient();
            using (var luisService = new LuisLanguageUnderstandingService(appName, appId, versionId, region, mockClient))
            {
                var utterances = new[]
                {
                    new LabeledUtterance(
                        "Book me a flight.",
                        "BookFlight",
                        new Entity[] { new Entity("Name", string.Empty, "me", 0) }),
                    new LabeledUtterance(
                        "Cancel my flight.",
                        "CancelFlight",
                        new Entity[] { new Entity("Subject", string.Empty, "flight", 0) })
                };

                var entityTypes = new[]
                {
                    new SimpleEntityType("Name"),
                    new SimpleEntityType("Subject")
                };

                await luisService.TrainAsync(utterances, entityTypes);

                // Assert correct import request
                var importRequest = mockClient.Requests.FirstOrDefault(request => request.Uri == importUri);
                importRequest.Should().NotBeNull();
                var jsonBody = JObject.Parse(importRequest.Body);

                // Expects 3 intents
                var intents = jsonBody.SelectToken(".intents").As<JArray>();
                intents.Count.Should().Be(3);
                intents.FirstOrDefault(token => token.Value<string>("name") == "None").Should().NotBeNull();
                intents.FirstOrDefault(token => token.Value<string>("name") == utterances[0].Intent).Should().NotBeNull();
                intents.FirstOrDefault(token => token.Value<string>("name") == utterances[1].Intent).Should().NotBeNull();

                // Expect 2 utterances
                var importUtterances = jsonBody.SelectToken(".utterances").As<JArray>();
                importUtterances.Count.Should().Be(2);

                var bookUtterance = importUtterances.FirstOrDefault(token => token.Value<string>("intent") == utterances[0].Intent);
                bookUtterance.Should().NotBeNull();
                bookUtterance.Value<string>("text").Should().Be(utterances[0].Text);
                bookUtterance.SelectToken(".entities[0].entity").Value<string>().Should().Be(utterances[0].Entities[0].EntityType);
                bookUtterance.SelectToken(".entities[0].startCharIndex").Value<int>().Should().Be(5);
                bookUtterance.SelectToken(".entities[0].endCharIndex").Value<int>().Should().Be(6);

                var cancelUtterance = importUtterances.FirstOrDefault(token => token.Value<string>("intent") == utterances[1].Intent);
                cancelUtterance.Should().NotBeNull();
                cancelUtterance.Value<string>("text").Should().Be(utterances[1].Text);
                cancelUtterance.SelectToken(".entities[0].entity").Value<string>().Should().Be(utterances[1].Entities[0].EntityType);
                cancelUtterance.SelectToken(".entities[0].startCharIndex").Value<int>().Should().Be(10);
                cancelUtterance.SelectToken(".entities[0].endCharIndex").Value<int>().Should().Be(15);

                // Expect 2 entities
                var entities = jsonBody.SelectToken(".entities").As<JArray>();
                entities.Count.Should().Be(2);
                entities.FirstOrDefault(token => token.Value<string>("name") == entityTypes[0].Name).Should().NotBeNull();
                entities.FirstOrDefault(token => token.Value<string>("name") == entityTypes[1].Name).Should().NotBeNull();
            }
        }

        [Test]
        public async Task CleanupModel()
        {
            var appName = string.Empty;
            var appId = Guid.NewGuid().ToString();
            var versionId = string.Empty;
            var region = "westus";
            var cleanupUri = $"https://{region}.api.cognitive.microsoft.com/luis/api/v2.0/apps/{appId}/versions/{versionId}/";
            var expectedUris = new string[] { cleanupUri };
            var mockClient = new MockLuisClient();
            using (var luisService = new LuisLanguageUnderstandingService(appName, appId, versionId, region, mockClient))
            {
                await luisService.CleanupAsync();
                mockClient.Requests.FirstOrDefault(request => request.Uri == cleanupUri).Should().NotBeNull();
            }
        }

        /// <summary>
        /// Mock version of <see cref="ILuisClient"/> for testing.
        /// Enables the verification of LUIS http requests. Returns a
        /// successful request response when the request matches expectation.
        /// Returns a bad request response when the request does not match.
        /// </summary>
        private sealed class MockLuisClient : ILuisClient
        {
            /// <summary>
            /// Gets a collection of requests made against the LUIS client.
            /// </summary>
            public IReadOnlyList<LuisRequest> Requests => this.RequestsInternal;

            /// <summary>
            /// Gets or sets callback when a request is made against the LUIS client.
            /// </summary>
            public Action<LuisRequest> OnRequest { get; set; }

            /// <summary>
            /// Gets a collection of requests made against the LUIS client.
            /// </summary>
            private List<LuisRequest> RequestsInternal { get; } = new List<LuisRequest>();

            /// <inheritdoc />
            public Task<HttpResponseMessage> GetAsync(string uri, CancellationToken cancellationToken)
            {
                return this.ProcessRequest(new LuisRequest
                {
                    Method = HttpMethod.Get.Method,
                    Uri = uri,
                });
            }

            /// <inheritdoc />
            public Task<HttpResponseMessage> PostAsync(string uri, string requestBody, CancellationToken cancellationToken)
            {
                return this.ProcessRequest(new LuisRequest
                {
                    Method = HttpMethod.Post.Method,
                    Uri = uri,
                    Body = requestBody,
                });
            }

            /// <inheritdoc />
            public Task<HttpResponseMessage> DeleteAsync(string uri, CancellationToken cancellationToken)
            {
                return this.ProcessRequest(new LuisRequest
                {
                    Method = HttpMethod.Delete.Method,
                    Uri = uri,
                });
            }

            /// <inheritdoc />
            public void Dispose()
            {
            }

            /// <summary>
            /// Process the LUIS request.
            /// </summary>
            /// <param name="request">LUIS <paramref name="request"/>.</param>
            /// <returns>HTTP response.</returns>
            private Task<HttpResponseMessage> ProcessRequest(LuisRequest request)
            {
                this.RequestsInternal.Add(request);
                this.OnRequest?.Invoke(request);
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
            }
        }

        /// <summary>
        /// LUIS request data.
        /// </summary>
        private sealed class LuisRequest
        {
            /// <summary>
            /// Gets or sets the HTTP method.
            /// </summary>
            public string Method { get; set; }

            /// <summary>
            /// Gets or sets the URI of the request.
            /// </summary>
            public string Uri { get; set; }

            /// <summary>
            /// Gets or sets the request body.
            /// </summary>
            public string Body { get; set; }
        }

        /// <summary>
        /// An <see cref="IEqualityComparer{T}"/> for <see cref="LuisLabeledUtterance"/>.
        /// </summary>
        private sealed class LuisLabeledUtteranceComparer : IEqualityComparer<LuisLabeledUtterance>
        {
            /// <inheritdoc />
            public bool Equals(LuisLabeledUtterance u1, LuisLabeledUtterance u2)
            {
                return (u1.Text == u2.Text) &&
                    (u1.Intent == u2.Intent) &&
                    u1.LuisEntities.SequenceEqual(u2.LuisEntities, new LuisEntityComparer());
            }

            /// <inheritdoc />
            public int GetHashCode(LuisLabeledUtterance utterance)
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// An <see cref="IEqualityComparer{T}"/> for <see cref="LuisLabeledUtterance"/>.
        /// </summary>
        private sealed class LuisEntityComparer : IEqualityComparer<LuisEntity>
        {
            /// <inheritdoc />
            public bool Equals(LuisEntity e1, LuisEntity e2)
            {
                return (e1.EntityName == e2.EntityName) &&
                    (e1.StartCharIndex == e2.StartCharIndex) &&
                    (e1.EndCharIndex == e2.EndCharIndex);
            }

            /// <inheritdoc />
            public int GetHashCode(LuisEntity entity)
            {
                throw new NotImplementedException();
            }
        }
    }
}
