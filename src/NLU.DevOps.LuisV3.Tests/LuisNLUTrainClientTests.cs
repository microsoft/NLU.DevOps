// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace NLU.DevOps.Luis.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Microsoft.Azure.CognitiveServices.Language.LUIS.Authoring.Models;
    using Models;
    using NUnit.Framework;

    [TestFixture]
    internal static class LuisNLUTrainClientTests
    {
        /// <summary>
        /// Epsilon used to accomodate for clock accuracy.
        /// </summary>
        private static readonly TimeSpan Epsilon = TimeSpan.FromMilliseconds(100);

        [Test]
        public static void ThrowsArgumentNull()
        {
            Action nullAppName = () => new LuisNLUTrainClient(null, null, null, new LuisSettings(), new MockLuisTrainClient());
            Action nullLuisSettings = () => new LuisNLUTrainClient(string.Empty, null, null, null, new MockLuisTrainClient());
            Action nullLuisClient = () => new LuisNLUTrainClient(string.Empty, null, null, new LuisSettings(), null);
            nullAppName.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("appName");
            nullLuisSettings.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("luisSettings");
            nullLuisClient.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("luisClient");

            using (var luis = GetTestLuisBuilder().Build())
            {
                Func<Task> nullUtterances = () => luis.TrainAsync(null);
                Func<Task> nullUtterance = () => luis.TrainAsync(new Models.LabeledUtterance[] { null });
                nullUtterances.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("utterances");
                nullUtterance.Should().Throw<ArgumentException>().And.ParamName.Should().Be("utterances");
            }
        }

        [Test]
        public static async Task TrainEmptyModel()
        {
            var mockClient = new MockLuisTrainClient();
            var builder = GetTestLuisBuilder();
            builder.AppVersion = Guid.NewGuid().ToString();
            builder.LuisClient = mockClient;
            using (var luis = builder.Build())
            {
                var utterances = Array.Empty<Models.LabeledUtterance>();
                await luis.TrainAsync(utterances).ConfigureAwait(false);

                // Assert correct import request
                var importRequest = mockClient.Requests.FirstOrDefault(request => request.Method == nameof(ILuisTrainClient.ImportVersionAsync));
                importRequest.Should().NotBeNull();
                importRequest.Arguments[2].Should().NotBeNull();
                var luisApp = importRequest.Arguments[2].As<LuisApp>();

                // Expects 3 intents
                luisApp.Intents.Count.Should().Be(1);
                luisApp.Intents.First().Name.Should().Be("None");

                // Assert train request
                var trainRequest = mockClient.Requests.FirstOrDefault(request => request.Method == nameof(ILuisTrainClient.TrainAsync));
                trainRequest.Should().NotBeNull();

                // Assert publish request
                var publishRequest = mockClient.Requests.FirstOrDefault(request => request.Method == nameof(ILuisTrainClient.PublishAppAsync));
                publishRequest.Should().NotBeNull();

                // Expects publish settings:
                publishRequest.Arguments[0].Should().Be(builder.AppId);
                publishRequest.Arguments[1].Should().Be(builder.AppVersion);
            }
        }

        [Test]
        public static async Task TrainModelWithUtterances()
        {
            var mockClient = new MockLuisTrainClient();
            var builder = GetTestLuisBuilder();
            builder.LuisClient = mockClient;
            using (var luis = builder.Build())
            {
                var utterances = new[]
                {
                    new Models.LabeledUtterance("Book me a flight.", "BookFlight", Array.Empty<Entity>()),
                    new Models.LabeledUtterance("Cancel my flight.", "CancelFlight", Array.Empty<Entity>())
                };

                await luis.TrainAsync(utterances).ConfigureAwait(false);

                // Assert correct import request
                var importRequest = mockClient.Requests.FirstOrDefault(request => request.Method == nameof(ILuisTrainClient.ImportVersionAsync));
                importRequest.Should().NotBeNull();
                var luisApp = importRequest.Arguments[2].As<LuisApp>();

                // Expects 3 intents
                luisApp.Intents.Count.Should().Be(3);
                luisApp.Intents.Should().Contain(intent => intent.Name == "None");
                luisApp.Intents.Should().Contain(intent => intent.Name == utterances[0].Intent);
                luisApp.Intents.Should().Contain(intent => intent.Name == utterances[1].Intent);

                // Expect 2 utterances
                luisApp.Utterances.Count.Should().Be(2);

                var bookUtterance = luisApp.Utterances.FirstOrDefault(utterance => utterance.Intent == utterances[0].Intent);
                bookUtterance.Should().NotBeNull();
                bookUtterance.Text.Should().Be(utterances[0].Text);
                bookUtterance.Entities.Count.Should().Be(0);

                var cancelUtterance = luisApp.Utterances.FirstOrDefault(utterance => utterance.Intent == utterances[1].Intent);
                cancelUtterance.Should().NotBeNull();
                cancelUtterance.Text.Should().Be(utterances[1].Text);
                cancelUtterance.Entities.Count.Should().Be(0);
            }
        }

        [Test]
        public static async Task TrainModelWithUtterancesAndSimpleEntities()
        {
            var mockClient = new MockLuisTrainClient();
            var builder = GetTestLuisBuilder();
            builder.LuisClient = mockClient;
            using (var luis = builder.Build())
            {
                var utterances = new[]
                {
                    new Models.LabeledUtterance(
                        "Book me a flight.",
                        "BookFlight",
                        new Entity[] { new Entity("Name", string.Empty, "me", 0) }),
                    new Models.LabeledUtterance(
                        "Cancel my flight.",
                        "CancelFlight",
                        new Entity[] { new Entity("Subject", string.Empty, "flight", 0) })
                };

                await luis.TrainAsync(utterances).ConfigureAwait(false);

                // Assert correct import request
                var importRequest = mockClient.Requests.FirstOrDefault(request => request.Method == nameof(ILuisTrainClient.ImportVersionAsync));
                importRequest.Should().NotBeNull();
                var luisApp = importRequest.Arguments[2].As<LuisApp>();

                // Expects 3 intents
                luisApp.Intents.Count.Should().Be(3);
                luisApp.Intents.Should().Contain(intent => intent.Name == "None");
                luisApp.Intents.Should().Contain(intent => intent.Name == utterances[0].Intent);
                luisApp.Intents.Should().Contain(intent => intent.Name == utterances[1].Intent);

                // Expect 2 utterances
                luisApp.Utterances.Count.Should().Be(2);

                var bookUtterance = luisApp.Utterances.FirstOrDefault(utterance => utterance.Intent == utterances[0].Intent);
                bookUtterance.Should().NotBeNull();
                bookUtterance.Text.Should().Be(utterances[0].Text);
                bookUtterance.Entities.Count.Should().Be(1);
                bookUtterance.Entities.First().Entity.Should().Be(utterances[0].Entities[0].EntityType);
                bookUtterance.Entities.First().StartPos.Should().Be(5);
                bookUtterance.Entities.First().EndPos.Should().Be(6);

                var cancelUtterance = luisApp.Utterances.FirstOrDefault(utterance => utterance.Intent == utterances[1].Intent);
                cancelUtterance.Should().NotBeNull();
                cancelUtterance.Text.Should().Be(utterances[1].Text);
                cancelUtterance.Entities.Count.Should().Be(1);
                cancelUtterance.Entities.First().Entity.Should().Be(utterances[1].Entities[0].EntityType);
                cancelUtterance.Entities.First().StartPos.Should().Be(10);
                cancelUtterance.Entities.First().EndPos.Should().Be(15);
            }
        }

        [Test]
        public static async Task CleanupModel()
        {
            var mockClient = new MockLuisTrainClient();
            var builder = GetTestLuisBuilder();
            builder.LuisClient = mockClient;
            using (var luis = builder.Build())
            {
                await luis.CleanupAsync().ConfigureAwait(false);
                var cleanupRequest = mockClient.Requests.FirstOrDefault(request => request.Method == nameof(ILuisTrainClient.DeleteAppAsync));
                cleanupRequest.Should().NotBeNull();
            }
        }

        [Test]
        public static async Task TrainingStatusDelayBetweenPolling()
        {
            var count = 0;
            string[] statusArray = { "Queued", "InProgress", "Success" };
            var mockClient = new MockLuisTrainClient();
            mockClient.OnRequestResponse = request =>
            {
                if (IsTrainingStatusRequest(request))
                {
                    return new[]
                    {
                        new ModelTrainingInfo
                        {
                            Details = new ModelTrainingDetails { Status = statusArray[count++] }
                        }
                    };
                }

                return null;
            };

            var builder = GetTestLuisBuilder();
            builder.LuisClient = mockClient;

            using (var luis = builder.Build())
            {
                await luis.TrainAsync(Array.Empty<Models.LabeledUtterance>()).ConfigureAwait(false);

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
            var mockClient = new MockLuisTrainClient();
            mockClient.OnRequestResponse = request =>
            {
                if (IsTrainingStatusRequest(request))
                {
                    return new[]
                    {
                        new ModelTrainingInfo
                        {
                            Details = new ModelTrainingDetails { Status = "Fail" }
                        }
                    };
                }

                return null;
            };

            var builder = GetTestLuisBuilder();
            builder.LuisClient = mockClient;

            using (var luis = builder.Build())
            {
                Func<Task> trainAsync = () => luis.TrainAsync(Array.Empty<Models.LabeledUtterance>());
                trainAsync.Should().Throw<InvalidOperationException>();
            }
        }

        [Test]
        public static async Task CreatesAppIfAppIdNotProvided()
        {
            var appId = Guid.NewGuid().ToString();
            var mockClient = new MockLuisTrainClient();
            mockClient.OnRequestResponse = request =>
            {
                if (request.Method == nameof(ILuisTrainClient.CreateAppAsync))
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
                await luis.TrainAsync(Array.Empty<Models.LabeledUtterance>()).ConfigureAwait(false);
                luis.LuisAppId.Should().Be(appId);
            }
        }

        [Test]
        public static async Task DoesNotOverwriteTemplateIntents()
        {
            var role = Guid.NewGuid().ToString();
            var intentName = Guid.NewGuid().ToString();
            var appTemplate = new LuisApp
            {
                ClosedLists = new List<ClosedList>(),
                Entities = new List<HierarchicalModel>(),
                Intents = new List<HierarchicalModel>
                {
                    new HierarchicalModel
                    {
                        Name = intentName,
                        Roles = new List<string> { role },
                    },
                },
                ModelFeatures = new List<JSONModelFeature>(),
                PrebuiltEntities = new List<PrebuiltEntity>(),
            };

            var mockClient = new MockLuisTrainClient();
            var builder = GetTestLuisBuilder();
            builder.LuisSettings = new LuisSettings(appTemplate);
            builder.LuisClient = mockClient;
            using (var luis = builder.Build())
            {
                var utterance = new Models.LabeledUtterance(null, intentName, null);
                await luis.TrainAsync(new[] { utterance }).ConfigureAwait(false);

                // Ensure LUIS app intent still has role
                var importRequest = mockClient.Requests.FirstOrDefault(request => request.Method == nameof(ILuisTrainClient.ImportVersionAsync));
                importRequest.Should().NotBeNull();
                var luisApp = importRequest.Arguments[2].As<LuisApp>();
                luisApp.Intents.Should().Contain(intent => intent.Name == intentName);
                luisApp.Intents.First(intent => intent.Name == intentName).Roles.Count.Should().Be(1);
                luisApp.Intents.First(intent => intent.Name == intentName).Roles.First().Should().Be(role);
            }
        }

        [Test]
        public static async Task TagsPrebuiltEntityWithReplacementName()
        {
            var text = Guid.NewGuid().ToString();
            var entityTypeName1 = Guid.NewGuid().ToString();
            var entityTypeName2 = Guid.NewGuid().ToString();
            var prebuiltEntityTypes = new Dictionary<string, string>
            {
                { entityTypeName1, entityTypeName2 },
            };

            var mockClient = new MockLuisTrainClient();
            var builder = GetTestLuisBuilder();
            builder.LuisClient = mockClient;
            builder.LuisSettings = new LuisSettings(prebuiltEntityTypes);

            using (var luis = builder.Build())
            {
                var entity1 = new Entity(entityTypeName1, null, text, 0);
                var entity2 = new Entity(entityTypeName2, null, text, 0);
                var utterance = new Models.LabeledUtterance(text, string.Empty, new[] { entity1, entity2 });
                await luis.TrainAsync(new[] { utterance }).ConfigureAwait(false);

                // Ensure LUIS app intent still has role
                var importRequest = mockClient.Requests.FirstOrDefault(request => request.Method == nameof(ILuisTrainClient.ImportVersionAsync));
                importRequest.Should().NotBeNull();
                var luisApp = importRequest.Arguments[2].As<LuisApp>();
                luisApp.Utterances.Should().Contain(u => u.Text == text);
                luisApp.Utterances.First(u => u.Text == text).Entities.Count().Should().Be(2);
                luisApp.Utterances.First(u => u.Text == text).Entities.Should().Contain(e => e.Entity == entityTypeName2);
                luisApp.Utterances.First(u => u.Text == text).Entities.Should().Contain(e => e.Entity == entityTypeName2);
            }
        }

        private static LuisNLUServiceBuilder GetTestLuisBuilder()
        {
            return new LuisNLUServiceBuilder
            {
                AppName = "test",
                AppId = Guid.NewGuid().ToString(),
                AppVersion = "0.1",
                LuisSettings = new LuisSettings(null, null),
                LuisClient = new MockLuisTrainClient(),
            };
        }

        private static bool IsTrainingStatusRequest(LuisRequest request)
        {
            return request.Method == nameof(ILuisTrainClient.GetTrainingStatusAsync);
        }

        private static class Timestamped
        {
            public static Timestamped<T> Create<T>(T instance)
            {
                return new Timestamped<T>(instance);
            }
        }

        private class LuisNLUServiceBuilder
        {
            public string AppId { get; set; }

            public string AppVersion { get; set; }

            public string AppName { get; set; }

            public LuisSettings LuisSettings { get; set; }

            public ILuisTrainClient LuisClient { get; set; }

            public LuisNLUTrainClient Build()
            {
                return new LuisNLUTrainClient(this.AppName, this.AppId, this.AppVersion, this.LuisSettings, this.LuisClient);
            }
        }

        private sealed class MockLuisTrainClient : ILuisTrainClient
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

            public Task<IList<ModelTrainingInfo>> GetTrainingStatusAsync(string appId, string versionId, CancellationToken cancellationToken)
            {
                return this.ProcessRequestAsync<IList<ModelTrainingInfo>>(appId, versionId);
            }

            public Task ImportVersionAsync(string appId, string versionId, LuisApp luisApp, CancellationToken cancellationToken)
            {
                return this.ProcessRequestAsync(appId, versionId, luisApp);
            }

            public Task PublishAppAsync(string appId, string versionId, CancellationToken cancellationToken)
            {
                return this.ProcessRequestAsync(appId, versionId);
            }

            public Task TrainAsync(string appId, string versionId, CancellationToken cancellationToken)
            {
                return this.ProcessRequestAsync(appId, versionId);
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
                    response = Array.Empty<ModelTrainingInfo>();
                }

                return Task.FromResult((T)response);
            }
        }

        private sealed class LuisRequest
        {
            public string Method { get; set; }

            public object[] Arguments { get; set; }
        }

        private sealed class Timestamped<T>
        {
            public Timestamped(T instance)
            {
                this.Instance = instance;
                this.Timestamp = DateTimeOffset.Now;
            }

            public T Instance { get; }

            public DateTimeOffset Timestamp { get; }
        }
    }
}
