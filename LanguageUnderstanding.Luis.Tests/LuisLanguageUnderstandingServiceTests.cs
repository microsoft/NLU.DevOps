// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace LanguageUnderstanding.Luis.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Models;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using NUnit.Framework;

    /// <summary>
    /// Test suite for <see cref="LuisEntity"/> class.
    /// </summary>
    [TestFixture]
    internal static class LuisLanguageUnderstandingServiceTests
    {
        /// <summary>
        /// Epsilon used to accomodate for clock accuracy.
        /// </summary>
        private static readonly TimeSpan Epsilon = TimeSpan.FromMilliseconds(100);

        [Test]
        public static void ThrowsArgumentNull()
        {
            Action nullAppName = () => new LuisLanguageUnderstandingService(null, string.Empty, string.Empty, new MockLuisClient());
            Action nullLuisClient = () => new LuisLanguageUnderstandingService(string.Empty, string.Empty, string.Empty, default(ILuisClient));
            nullAppName.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("appName");
            nullLuisClient.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("luisClient");

            using (var luis = GetTestLuisBuilder().Build())
            {
                Func<Task> nullUtterances = () => luis.TrainAsync(null, Array.Empty<EntityType>());
                Func<Task> nullUtterance = () => luis.TrainAsync(new LabeledUtterance[] { null }, Array.Empty<EntityType>());
                Func<Task> nullEntityTypes = () => luis.TrainAsync(Array.Empty<LabeledUtterance>(), null);
                Func<Task> nullEntityType = () => luis.TrainAsync(Array.Empty<LabeledUtterance>(), new EntityType[] { null });
                Func<Task> nullTestUtterance = () => luis.TestAsync(null, Array.Empty<EntityType>());
                Func<Task> nullTestEntityTypes = () => luis.TestAsync(string.Empty, null);
                Func<Task> nullTestEntityType = () => luis.TestAsync(string.Empty, new EntityType[] { null });
                Func<Task> nullTestSpeechUtterance = () => luis.TestSpeechAsync(null, Array.Empty<EntityType>());
                Func<Task> nullTestSpeechEntityTypes = () => luis.TestSpeechAsync(string.Empty, null);
                Func<Task> nullTestSpeechEntityType = () => luis.TestSpeechAsync(string.Empty, new EntityType[] { null });
                nullUtterances.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("utterances");
                nullUtterance.Should().Throw<ArgumentException>().And.ParamName.Should().Be("utterances");
                nullEntityTypes.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("entityTypes");
                nullEntityType.Should().Throw<ArgumentException>().And.ParamName.Should().Be("entityTypes");
                nullTestUtterance.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("utterance");
                nullTestEntityTypes.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("entityTypes");
                nullTestEntityType.Should().Throw<ArgumentException>().And.ParamName.Should().Be("entityTypes");
                nullTestSpeechUtterance.Should().Throw<ArgumentException>().And.ParamName.Should().Be("speechFile");
                nullTestSpeechEntityTypes.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("entityTypes");
                nullTestSpeechEntityType.Should().Throw<ArgumentException>().And.ParamName.Should().Be("entityTypes");
            }
        }

        [Test]
        public static void ThrowsInvalidOperationWhenUntrained()
        {
            var builder = GetTestLuisBuilder();
            builder.AppId = null;
            using (var luis = builder.Build())
            {
                Func<Task> testAsync = () => luis.TestAsync(string.Empty, Array.Empty<EntityType>());
                Func<Task> testSpeechAsync = () => luis.TestSpeechAsync(string.Empty, Array.Empty<EntityType>());
                Func<Task> cleanupAsync = () => luis.CleanupAsync();
                testAsync.Should().Throw<InvalidOperationException>()
                    .And.Message.Should().Contain(nameof(LuisLanguageUnderstandingService.TestAsync))
                    .And.Contain(nameof(LuisLanguageUnderstandingService.AppId));
                testSpeechAsync.Should().Throw<InvalidOperationException>()
                    .And.Message.Should().Contain(nameof(LuisLanguageUnderstandingService.TestSpeechAsync))
                    .And.Contain(nameof(LuisLanguageUnderstandingService.AppId));
                cleanupAsync.Should().Throw<InvalidOperationException>()
                    .And.Message.Should().Contain(nameof(LuisLanguageUnderstandingService.CleanupAsync))
                    .And.Contain(nameof(LuisLanguageUnderstandingService.AppId));
            }
        }

        [Test]
        public static void LuisEntityInitializes()
        {
            var entityType = "Location";
            var startCharIndex = 2;
            var endCharIndex = 8;
            var luisEntity = new LuisEntity(entityType, startCharIndex, endCharIndex);
            luisEntity.EntityName.Should().Be("Location");
            luisEntity.StartCharIndex.Should().Be(startCharIndex);
            luisEntity.EndCharIndex.Should().Be(endCharIndex);
        }

        [Test]
        public static void LuisEntitySerializes()
        {
            var entityType = "Location";
            var startCharIndex = 2;
            var endCharIndex = 8;
            var luisEntity = new LuisEntity(entityType, startCharIndex, endCharIndex);
            var actualString = JsonConvert.SerializeObject(luisEntity);
            var actual = JObject.Parse(actualString);
            actual.Value<string>("entity").Should().Be(entityType);
            actual.Value<int>("startPos").Should().Be(2);
            actual.Value<int>("endPos").Should().Be(8);
        }

        [Test]
        public static void EntityConvertsToLuisEntity()
        {
            var utterance = "Engineer is the job I want!";
            var entity = new Entity("String", null, "Engineer", 0);
            var expected = new LuisEntity("String", 0, 7);
            var entityType = new EntityType("String", "simple", null);
            var actual = LuisEntity.FromEntity(entity, utterance, entityType);
            new LuisEntityComparer().Equals(actual, expected).Should().BeTrue();
        }

        /* LuisLabeledUtteranceTests */

        [Test]
        public static void LuisLabeledUtteranceInitializes()
        {
            var text = "I want to fly from New York to Seattle";
            var intent = "bookFlight";
            var luisEntities = new List<LuisEntity>
            {
                new LuisEntity("Location::From", 20, 27),
                new LuisEntity("Location::To", 32, 38),
            };

            var luisLabeledUtterance = new LuisLabeledUtterance(text, intent, luisEntities);
            luisLabeledUtterance.Text.Should().Be(text);
            luisLabeledUtterance.Intent.Should().Be(intent);
            luisLabeledUtterance.LuisEntities.Should().BeEquivalentTo(luisEntities);
        }

        [Test]
        public static void LuisLabeledUtteranceSerializes()
        {
            var text = "My name is Bill Gates.";
            var intent = "updateName";
            var luisEntities = new List<LuisEntity>
            {
                new LuisEntity("FirstName", 11, 14),
                new LuisEntity("LastName", 16, 20),
            };

            var luisLabeledUtterance = new LuisLabeledUtterance(text, intent, luisEntities);
            var actualString = JsonConvert.SerializeObject(luisLabeledUtterance);
            var actual = JObject.Parse(actualString);
            actual.Value<string>("text").Should().Be(text);
            actual.Value<string>("intent").Should().Be(intent);
            actual["entities"].As<JArray>().Count.Should().Be(2);
            actual.SelectToken(".entities[0].entity").Value<string>().Should().Be(luisEntities[0].EntityName);
            actual.SelectToken(".entities[0].startPos").Value<int>().Should().Be(luisEntities[0].StartCharIndex);
            actual.SelectToken(".entities[0].endPos").Value<int>().Should().Be(luisEntities[0].EndCharIndex);
            actual.SelectToken(".entities[1].entity").Value<string>().Should().Be(luisEntities[1].EntityName);
            actual.SelectToken(".entities[1].startPos").Value<int>().Should().Be(luisEntities[1].StartCharIndex);
            actual.SelectToken(".entities[1].endPos").Value<int>().Should().Be(luisEntities[1].EndCharIndex);
        }

        [Test]
        public static void LabeledUtteranceToLuisLabeledUtterance()
        {
            var text = "My name is Bill Gates.";
            var intent = "updateName";
            List<LuisEntity> luisEntities = new List<LuisEntity>
            {
                new LuisEntity("FirstName", 11, 14),
                new LuisEntity("LastName", 16, 20),
            };

            var expected = new LuisLabeledUtterance(text, intent, luisEntities);

            var entities = new List<Entity>
            {
                new Entity("FirstName", null, "Bill", 0),
                new Entity("LastName", null, "Gates", 0),
            };

            var entityTypes = new[]
            {
                new EntityType("FirstName", "simple", null),
                new EntityType("LastName", "simple", null),
            };

            var labeledUtterance = new LabeledUtterance(text, intent, entities);
            var actual = LuisLabeledUtterance.FromLabeledUtterance(labeledUtterance, entityTypes);
            new LuisLabeledUtteranceComparer().Equals(actual, expected).Should().BeTrue();
        }

        [Test]
        public static async Task TrainEmptyModel()
        {
            var mockClient = new MockLuisClient();
            var builder = GetTestLuisBuilder();
            builder.IsStaging = true;
            builder.AppVersion = Guid.NewGuid().ToString();
            builder.LuisClient = mockClient;
            using (var luis = builder.Build())
            {
                var utterances = Enumerable.Empty<LabeledUtterance>();
                var entityTypes = Enumerable.Empty<EntityType>();
                await luis.TrainAsync(utterances, entityTypes).ConfigureAwait(false);

                // Assert correct import request
                var importRequest = mockClient.Requests.FirstOrDefault(request => request.Method == nameof(ILuisClient.ImportVersionAsync));
                importRequest.Should().NotBeNull();
                importRequest.Arguments[2].Should().NotBeNull();
                var importBody = importRequest.Arguments[2].As<JObject>();

                // Expects 3 intents
                var intents = importBody.SelectToken(".intents").As<JArray>();
                intents.Count.Should().Be(1);
                intents.FirstOrDefault(token => token.Value<string>("name") == "None").Should().NotBeNull();

                // Assert train request
                var trainRequest = mockClient.Requests.FirstOrDefault(request => request.Method == nameof(ILuisClient.TrainAsync));
                trainRequest.Should().NotBeNull();

                // Assert publish request
                var publishRequest = mockClient.Requests.FirstOrDefault(request => request.Method == nameof(ILuisClient.PublishAppAsync));
                publishRequest.Should().NotBeNull();

                // Expects publish settings:
                publishRequest.Arguments[0].Should().Be(builder.AppId);
                publishRequest.Arguments[1].Should().Be(builder.AppVersion);
            }
        }

        [Test]
        public static async Task TrainModelWithUtterances()
        {
            var mockClient = new MockLuisClient();
            var builder = GetTestLuisBuilder();
            builder.LuisClient = mockClient;
            using (var luis = builder.Build())
            {
                var utterances = new[]
                {
                    new LabeledUtterance("Book me a flight.", "BookFlight", Array.Empty<Entity>()),
                    new LabeledUtterance("Cancel my flight.", "CancelFlight", Array.Empty<Entity>())
                };

                var entityTypes = Enumerable.Empty<EntityType>();
                await luis.TrainAsync(utterances, entityTypes).ConfigureAwait(false);

                // Assert correct import request
                var importRequest = mockClient.Requests.FirstOrDefault(request => request.Method == nameof(ILuisClient.ImportVersionAsync));
                importRequest.Should().NotBeNull();
                var jsonBody = importRequest.Arguments[2].As<JObject>();

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
        public static async Task TrainModelWithUtterancesAndSimpleEntities()
        {
            var mockClient = new MockLuisClient();
            var builder = GetTestLuisBuilder();
            builder.LuisClient = mockClient;
            using (var luis = builder.Build())
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
                    new EntityType("Name", "simple", null),
                    new EntityType("Subject", "simple", null),
                };

                await luis.TrainAsync(utterances, entityTypes).ConfigureAwait(false);

                // Assert correct import request
                var importRequest = mockClient.Requests.FirstOrDefault(request => request.Method == nameof(ILuisClient.ImportVersionAsync));
                importRequest.Should().NotBeNull();
                var jsonBody = importRequest.Arguments[2].As<JObject>();

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
                bookUtterance.SelectToken(".entities[0].startPos").Value<int>().Should().Be(5);
                bookUtterance.SelectToken(".entities[0].endPos").Value<int>().Should().Be(6);

                var cancelUtterance = importUtterances.FirstOrDefault(token => token.Value<string>("intent") == utterances[1].Intent);
                cancelUtterance.Should().NotBeNull();
                cancelUtterance.Value<string>("text").Should().Be(utterances[1].Text);
                cancelUtterance.SelectToken(".entities[0].entity").Value<string>().Should().Be(utterances[1].Entities[0].EntityType);
                cancelUtterance.SelectToken(".entities[0].startPos").Value<int>().Should().Be(10);
                cancelUtterance.SelectToken(".entities[0].endPos").Value<int>().Should().Be(15);

                // Expect 2 entities
                var entities = jsonBody.SelectToken(".entities").As<JArray>();
                entities.Count.Should().Be(2);
                entities.FirstOrDefault(token => token.Value<string>("name") == entityTypes[0].Name).Should().NotBeNull();
                entities.FirstOrDefault(token => token.Value<string>("name") == entityTypes[1].Name).Should().NotBeNull();
            }
        }

        [Test]
        public static async Task CleanupModel()
        {
            var mockClient = new MockLuisClient();
            var builder = GetTestLuisBuilder();
            builder.LuisClient = mockClient;
            using (var luis = builder.Build())
            {
                await luis.CleanupAsync().ConfigureAwait(false);
                var cleanupRequest = mockClient.Requests.FirstOrDefault(request => request.Method == nameof(ILuisClient.DeleteAppAsync));
                cleanupRequest.Should().NotBeNull();
            }
        }

        [Test]
        public static async Task TestModel()
        {
            var test = "the quick brown fox jumped over the lazy dog";

            var mockClient = new MockLuisClient();
            mockClient.OnRequestResponse = request =>
            {
                if (request.Method == nameof(ILuisClient.QueryAsync))
                {
                    return new JObject
                    {
                        { "query", test },
                        { "topScoringIntent", new JObject { { "intent", "intent" } } },
                        {
                            "entities",
                            new JArray
                            {
                                new JObject
                                {
                                    { "entity", "the" },
                                    { "type", "type" },
                                    { "startIndex", 32 },
                                    { "endIndex", 34 },
                                },
                            }
                        },
                    };
                }

                return null;
            };

            var builder = GetTestLuisBuilder();
            builder.LuisClient = mockClient;

            using (var luis = builder.Build())
            {
                var result = await luis.TestAsync(test, Array.Empty<EntityType>()).ConfigureAwait(false);
                result.Text.Should().Be(test);
                result.Intent.Should().Be("intent");
                result.Entities.Count.Should().Be(1);
                result.Entities[0].EntityType.Should().Be("type");
                result.Entities[0].EntityValue.Should().Be(default(string));
                result.Entities[0].MatchText.Should().Be("the");
                result.Entities[0].MatchIndex.Should().Be(1);
            }
        }

        [Test]
        public static async Task TestModelWithEntityResolution()
        {
            var test = "the quick brown fox jumped over the lazy dog";

            var mockClient = new MockLuisClient();
            mockClient.OnRequestResponse = request =>
            {
                if (request.Method == nameof(ILuisClient.QueryAsync))
                {
                    return JObject.Parse(@"{
                            ""query"": ""the quick brown fox jumped over the lazy dog today"",
                            ""topScoringIntent"": {
                                ""intent"": ""Calendar.Add"",
                                ""score"": 0.718678534
                            },
                            ""entities"": [
                                {
                                    ""entity"": ""today"",
                                    ""type"": ""builtin.datetimeV2.date"",
                                    ""startIndex"": 45,
                                    ""endIndex"": 49,
                                    ""resolution"": {
                                        ""values"": [
                                            {
                                                ""timex"": ""2018-11-16"",
                                                ""type"": ""date"",
                                                ""value"": ""2018-11-16""
                                            }
                                        ]
                                    }
                                },
                                {
                                    ""entity"": ""brown fox"",
                                    ""type"": ""builtin.personName"",
                                    ""startIndex"": 10,
                                    ""endIndex"": 18,
                                    ""resolution"": {
                                        ""values"": [
                                            ""Fox""
                                        ]
                                    }
                                },
                                {
                                    ""entity"": ""the"",
                                    ""type"": ""thetype"",
                                    ""startIndex"": 0,
                                    ""endIndex"": 2,
                                    ""resolution"": {
                                        ""value"": ""THE""
                                    }
                                }
                            ]
                        }");
                }

                return null;
            };

            var builder = GetTestLuisBuilder();
            builder.LuisClient = mockClient;

            using (var luis = builder.Build())
            {
                var result = await luis.TestAsync(test, Array.Empty<EntityType>()).ConfigureAwait(false);
                result.Entities.Count.Should().Be(3);
                result.Entities[0].EntityValue.Should().Be("2018-11-16");
                result.Entities[1].EntityValue.Should().Be("Fox");
                result.Entities[2].EntityValue.Should().Be("THE");
            }
        }

        [Test]
        public static async Task TestSpeech()
        {
            var test = "the quick brown fox jumped over the lazy dog entity";

            var mockClient = new MockLuisClient();
            mockClient.OnRequestResponse = request =>
            {
                if (request.Method == nameof(ILuisClient.RecognizeSpeechAsync))
                {
                    return new JObject
                    {
                        { "query", test },
                        { "topScoringIntent", new JObject { { "intent", "intent" } } },
                        {
                            "entities",
                            new JArray
                            {
                                new JObject
                                {
                                    { "entity", "entity" },
                                    { "type", "type" },
                                    { "startIndex", 45 },
                                    { "endIndex", 50 },
                                },
                            }
                        },
                    };
                }

                return null;
            };

            var builder = GetTestLuisBuilder();
            builder.LuisClient = mockClient;

            using (var luis = builder.Build())
            {
                var result = await luis.TestSpeechAsync("somefile", new List<EntityType>()).ConfigureAwait(false);
                result.Text.Should().Be(test);
                result.Intent.Should().Be("intent");
                result.Entities.Count.Should().Be(1);
                result.Entities[0].EntityType.Should().Be("type");

                result.Entities[0].MatchText.Should().Be("entity");
                result.Entities[0].MatchIndex.Should().Be(0);
            }
        }

        [Test]
        public static async Task TestWithBuiltinEntity()
        {
            var test = "the quick brown fox jumped over the lazy dog";

            var mockClient = new MockLuisClient();
            mockClient.OnRequestResponse = request =>
            {
                if (request.Method == nameof(ILuisClient.QueryAsync))
                {
                    return new JObject
                    {
                        { "query", test },
                        { "topScoringIntent", new JObject { { "intent", "intent" } } },
                        {
                            "entities",
                            new JArray
                            {
                                new JObject
                                {
                                    { "entity", "the" },
                                    { "type", "builtin.test" },
                                    { "startIndex", 32 },
                                    { "endIndex", 34 },
                                },
                            }
                        },
                    };
                }

                return null;
            };

            var entityType = new EntityType("type", "builtin", new JObject { { "name", "test" } });

            var builder = GetTestLuisBuilder();
            builder.LuisClient = mockClient;

            using (var luis = builder.Build())
            {
                var result = await luis.TestAsync(test, new[] { entityType }).ConfigureAwait(false);
                result.Text.Should().Be(test);
                result.Intent.Should().Be("intent");
                result.Entities.Count.Should().Be(1);
                result.Entities[0].EntityType.Should().Be("type");
                result.Entities[0].EntityValue.Should().Be(default(string));
                result.Entities[0].MatchText.Should().Be("the");
                result.Entities[0].MatchIndex.Should().Be(1);
            }
        }

        [Test]
        public static async Task TrainingStatusDelayBetweenPolling()
        {
            var count = 0;
            string[] statusArray = { "Queued", "InProgress", "Success" };
            var mockClient = new MockLuisClient();
            mockClient.OnRequestResponse = request =>
            {
                if (IsTrainingStatusRequest(request))
                {
                    return new JArray
                    {
                        new JObject
                        {
                            { "details", new JObject { { "status", statusArray[count++] } } }
                        }
                    };
                }

                return null;
            };

            var builder = GetTestLuisBuilder();
            builder.LuisClient = mockClient;

            using (var luis = builder.Build())
            {
                await luis.TrainAsync(Array.Empty<LabeledUtterance>(), Array.Empty<EntityType>()).ConfigureAwait(false);

                // Ensure correct number of training status requests are made.
                mockClient.Requests.Where(IsTrainingStatusRequest).Count().Should().Be(statusArray.Length);

                // Ensure 2 second delay between requests
                var previousRequest = mockClient.TimestampedRequests.Where(t => IsTrainingStatusRequest(t.Instance)).First();
                for (var i = 1; i < statusArray.Length; ++i)
                {
                    var nextRequest = mockClient.TimestampedRequests.Where(t => IsTrainingStatusRequest(t.Instance)).Skip(i).First();
                    var timeDifference = nextRequest.Timestamp - previousRequest.Timestamp;
                    previousRequest = nextRequest;
                    timeDifference.Should().BeGreaterThan(TimeSpan.FromSeconds(2) - Epsilon);
                }
            }
        }

        [Test]
        public static void TrainingFailedThrowsInvalidOperation()
        {
            var mockClient = new MockLuisClient();
            mockClient.OnRequestResponse = request =>
            {
                if (IsTrainingStatusRequest(request))
                {
                    return new JArray
                    {
                        new JObject
                        {
                            { "details", new JObject { { "status", "Fail" } } }
                        }
                    };
                }

                return null;
            };

            var builder = GetTestLuisBuilder();
            builder.LuisClient = mockClient;

            using (var luis = builder.Build())
            {
                Func<Task> trainAsync = () => luis.TrainAsync(Array.Empty<LabeledUtterance>(), Array.Empty<EntityType>());
                trainAsync.Should().Throw<InvalidOperationException>();
            }
        }

        [Test]
        public static async Task CreatesAppIfAppIdNotProvided()
        {
            var appId = Guid.NewGuid().ToString();
            var mockClient = new MockLuisClient();
            mockClient.OnRequestResponse = request =>
            {
                if (request.Method == nameof(ILuisClient.CreateAppAsync))
                {
                    return appId;
                }

                return null;
            };

            var builder = GetTestLuisBuilder();
            builder.LuisClient = mockClient;
            builder.AppId = null;
            using (var luis = builder.Build())
            {
                await luis.TrainAsync(Array.Empty<LabeledUtterance>(), Array.Empty<EntityType>()).ConfigureAwait(false);
                luis.AppId.Should().Be(appId);
            }
        }

        [Test]
        public static async Task TestSpeechAsyncNoMatchResponse()
        {
            var utterance = Guid.NewGuid().ToString();
            var builder = GetTestLuisBuilder();
            using (var luis = builder.Build())
            {
                var results = await luis.TestSpeechAsync(utterance, Array.Empty<EntityType>()).ConfigureAwait(false);
                results.Intent.Should().BeNull();
                results.Text.Should().BeNull();
                results.Entities.Should().BeNull();
            }
        }

        private static LuisLanguageUnderstandingServiceBuilder GetTestLuisBuilder()
        {
            return new LuisLanguageUnderstandingServiceBuilder
            {
                AppName = "test",
                AppId = Guid.NewGuid().ToString(),
                AppVersion = "0.1",
                LuisClient = new MockLuisClient(),
            };
        }

        private static bool IsTrainingStatusRequest(LuisRequest request)
        {
            return request.Method == nameof(ILuisClient.GetTrainingStatusAsync);
        }

        /// <summary>
        /// A helper class for creating <see cref="Timestamped{T}"/> instances.
        /// </summary>
        private static class Timestamped
        {
            /// <summary>
            /// Create the timestamped instance.
            /// </summary>
            /// <returns>The timestamped instance.</returns>
            /// <param name="instance">Instance.</param>
            /// <typeparam name="T">The type of instance.</typeparam>
            public static Timestamped<T> Create<T>(T instance)
            {
                return new Timestamped<T>(instance);
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
            public Action<LuisRequest> OnRequest { get; set; }

            public Func<LuisRequest, object> OnRequestResponse { get; set; }

            public IEnumerable<LuisRequest> Requests => this.RequestsInternal.Select(x => x.Instance);

            public IEnumerable<Timestamped<LuisRequest>> TimestampedRequests => this.RequestsInternal;

            private List<Timestamped<LuisRequest>> RequestsInternal { get; } = new List<Timestamped<LuisRequest>>();

            public Task<string> CreateAppAsync(string appName, CancellationToken cancellationToken)
            {
                return this.ProcessRequestAsync<string>(appName);
            }

            public Task DeleteAppAsync(string appId, CancellationToken cancellationToken)
            {
                return this.ProcessRequestAsync(appId);
            }

            public Task<JArray> GetTrainingStatusAsync(string appId, string appVersion, CancellationToken cancellationToken)
            {
                return this.ProcessRequestAsync<JArray>(appId, appVersion);
            }

            public Task ImportVersionAsync(string appId, string appVersion, JObject importJson, CancellationToken cancellationToken)
            {
                return this.ProcessRequestAsync(appId, appVersion, importJson);
            }

            public Task PublishAppAsync(string appId, string appVersion, CancellationToken cancellationToken)
            {
                return this.ProcessRequestAsync(appId, appVersion);
            }

            public Task<JObject> QueryAsync(string appId, string text, CancellationToken cancellationToken)
            {
                return this.ProcessRequestAsync<JObject>(appId, text);
            }

            public Task<JObject> RecognizeSpeechAsync(string appId, string speechFile, CancellationToken cancellationToken)
            {
                return this.ProcessRequestAsync<JObject>(appId, speechFile);
            }

            public Task TrainAsync(string appId, string appVersion, CancellationToken cancellationToken)
            {
                return this.ProcessRequestAsync(appId, appVersion);
            }

            public void Dispose()
            {
            }

            private Task ProcessRequestAsync(object arg0, object arg1 = null, object arg2 = null, [CallerMemberName] string methodName = null)
            {
                return this.ProcessRequestAsync<object>(arg0, arg1, arg2, methodName);
            }

            private Task<T> ProcessRequestAsync<T>(object arg0, object arg1 = null, object arg2 = null, [CallerMemberName] string methodName = null)
            {
                var request = new LuisRequest
                {
                    Method = methodName,
                    Arguments = new[] { arg0, arg1, arg2 },
                };

                this.RequestsInternal.Add(Timestamped.Create(request));

                this.OnRequest?.Invoke(request);

                var response = this.OnRequestResponse?.Invoke(request);
                if (response == null && IsTrainingStatusRequest(request))
                {
                    response = new JArray();
                }

                return Task.FromResult((T)response);
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
            public object[] Arguments { get; set; }
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

        /// <summary>
        /// A helper class for timestamping values.
        /// </summary>
        /// <typeparam name="T">Type of instance.</typeparam>
        private sealed class Timestamped<T>
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="Timestamped{T}"/> class.
            /// </summary>
            /// <param name="instance">Instance.</param>
            public Timestamped(T instance)
            {
                this.Instance = instance;
                this.Timestamp = DateTimeOffset.Now;
            }

            /// <summary>
            /// Gets the instance.
            /// </summary>
            public T Instance { get; }

            /// <summary>
            /// Gets the timestamp.
            /// </summary>
            public DateTimeOffset Timestamp { get; }
        }
    }
}
