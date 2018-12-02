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
    using Microsoft.Azure.CognitiveServices.Language.LUIS.Runtime.Models;
    using Models;
    using Newtonsoft.Json.Linq;
    using NUnit.Framework;

    [TestFixture]
    internal static class LuisNLUServiceTests
    {
        /// <summary>
        /// Epsilon used to accomodate for clock accuracy.
        /// </summary>
        private static readonly TimeSpan Epsilon = TimeSpan.FromMilliseconds(100);

        [Test]
        public static void ThrowsArgumentNull()
        {
            using (var luis = GetTestLuisBuilder().Build())
            {
                Func<Task> nullUtterances = () => luis.TrainAsync(null, Array.Empty<EntityType>());
                Func<Task> nullUtterance = () => luis.TrainAsync(new Models.LabeledUtterance[] { null }, Array.Empty<EntityType>());
                Func<Task> nullEntityTypes = () => luis.TrainAsync(Array.Empty<Models.LabeledUtterance>(), null);
                Func<Task> nullEntityType = () => luis.TrainAsync(Array.Empty<Models.LabeledUtterance>(), new EntityType[] { null });
                Func<Task> nullTestUtterance = () => luis.TestAsync(null);
                Func<Task> nullTestEntityTypes = () => luis.TestAsync(string.Empty, null);
                Func<Task> nullTestEntityType = () => luis.TestAsync(string.Empty, new EntityType[] { null });
                Func<Task> nullTestSpeechUtterance = () => luis.TestSpeechAsync(null);
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
                Func<Task> testAsync = () => luis.TestAsync(string.Empty);
                Func<Task> testSpeechAsync = () => luis.TestSpeechAsync(string.Empty);
                Func<Task> cleanupAsync = () => luis.CleanupAsync();
                testAsync.Should().Throw<InvalidOperationException>()
                    .And.Message.Should().Contain(nameof(LuisNLUService.TestAsync))
                    .And.Contain(nameof(LuisNLUService.LuisAppId));
                testSpeechAsync.Should().Throw<InvalidOperationException>()
                    .And.Message.Should().Contain(nameof(LuisNLUService.TestSpeechAsync))
                    .And.Contain(nameof(LuisNLUService.LuisAppId));
                cleanupAsync.Should().Throw<InvalidOperationException>()
                    .And.Message.Should().Contain(nameof(LuisNLUService.CleanupAsync))
                    .And.Contain(nameof(LuisNLUService.LuisAppId));
            }
        }

        [Test]
        public static async Task TrainEmptyModel()
        {
            var mockClient = new MockLuisClient();
            var builder = GetTestLuisBuilder();
            builder.AppVersion = Guid.NewGuid().ToString();
            builder.LuisClient = mockClient;
            using (var luis = builder.Build())
            {
                var utterances = Enumerable.Empty<Models.LabeledUtterance>();
                var entityTypes = Enumerable.Empty<EntityType>();
                await luis.TrainAsync(utterances, entityTypes).ConfigureAwait(false);

                // Assert correct import request
                var importRequest = mockClient.Requests.FirstOrDefault(request => request.Method == nameof(ILuisClient.ImportVersionAsync));
                importRequest.Should().NotBeNull();
                importRequest.Arguments[2].Should().NotBeNull();
                var luisApp = importRequest.Arguments[2].As<LuisApp>();

                // Expects 3 intents
                luisApp.Intents.Count.Should().Be(1);
                luisApp.Intents.First().Name.Should().Be("None");

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
                    new Models.LabeledUtterance("Book me a flight.", "BookFlight", Array.Empty<Entity>()),
                    new Models.LabeledUtterance("Cancel my flight.", "CancelFlight", Array.Empty<Entity>())
                };

                var entityTypes = Enumerable.Empty<EntityType>();
                await luis.TrainAsync(utterances, entityTypes).ConfigureAwait(false);

                // Assert correct import request
                var importRequest = mockClient.Requests.FirstOrDefault(request => request.Method == nameof(ILuisClient.ImportVersionAsync));
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
            var mockClient = new MockLuisClient();
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

                var entityTypes = new[]
                {
                    new EntityType("Name", "entities", null),
                    new EntityType("Subject", "entities", null),
                };

                await luis.TrainAsync(utterances, entityTypes).ConfigureAwait(false);

                // Assert correct import request
                var importRequest = mockClient.Requests.FirstOrDefault(request => request.Method == nameof(ILuisClient.ImportVersionAsync));
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

                // Expect 2 entities
                luisApp.Entities.Count.Should().Be(2);
                luisApp.Entities.Should().Contain(entity => entity.Name == entityTypes[0].Name);
                luisApp.Entities.Should().Contain(entity => entity.Name == entityTypes[1].Name);
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
                    return new LuisResult
                    {
                        Query = test,
                        TopScoringIntent = new IntentModel { Intent = "intent" },
                        Entities = new[]
                        {
                            new EntityModel
                            {
                                Entity = "the",
                                Type = "type",
                                StartIndex = 32,
                                EndIndex = 34,
                            },
                        },
                    };
                }

                return null;
            };

            var builder = GetTestLuisBuilder();
            builder.LuisClient = mockClient;

            using (var luis = builder.Build())
            {
                var result = await luis.TestAsync(test).ConfigureAwait(false);
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
                    return new LuisResult
                    {
                        Query = "the quick brown fox jumped over the lazy dog today",
                        Entities = new[]
                        {
                            new EntityModel
                            {
                                Entity = "today",
                                StartIndex = 45,
                                EndIndex = 49,
                                AdditionalProperties = new Dictionary<string, object>
                                {
                                    {
                                        "resolution",
                                        new JObject
                                        {
                                            { "values", new JArray { new JObject { { "value", "2018-11-16" } } } },
                                        }
                                    },
                                },
                            },
                            new EntityModel
                            {
                                Entity = "brown fox",
                                StartIndex = 10,
                                EndIndex = 18,
                                AdditionalProperties = new Dictionary<string, object>
                                {
                                    { "resolution", new JObject { { "values", new JArray { "Fox" } } } },
                                },
                            },
                            new EntityModel
                            {
                                Entity = "the",
                                StartIndex = 0,
                                EndIndex = 2,
                                AdditionalProperties = new Dictionary<string, object>
                                {
                                    { "resolution", new JObject { { "value", "THE" } } },
                                },
                            }
                        }
                    };
                }

                return null;
            };

            var builder = GetTestLuisBuilder();
            builder.LuisClient = mockClient;

            using (var luis = builder.Build())
            {
                var result = await luis.TestAsync(test).ConfigureAwait(false);
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
                    return new LuisResult
                    {
                        Query = test,
                        TopScoringIntent = new IntentModel { Intent = "intent" },
                        Entities = new[]
                        {
                            new EntityModel
                            {
                                Entity = "entity",
                                Type = "type",
                                StartIndex = 45,
                                EndIndex = 50,
                            },
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
                    return new LuisResult
                    {
                        Query = test,
                        TopScoringIntent = new IntentModel { Intent = "intent" },
                        Entities = new[]
                        {
                            new EntityModel
                            {
                                Entity = "the",
                                Type = "builtin.test",
                                StartIndex = 32,
                                EndIndex = 34
                            },
                        },
                    };
                }

                return null;
            };

            var entityType = new EntityType("type", "prebuiltEntities", new JObject { { "name", "test" } });

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
                await luis.TrainAsync(Array.Empty<Models.LabeledUtterance>(), Array.Empty<EntityType>()).ConfigureAwait(false);

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
                Func<Task> trainAsync = () => luis.TrainAsync(Array.Empty<Models.LabeledUtterance>(), Array.Empty<EntityType>());
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
                await luis.TrainAsync(Array.Empty<Models.LabeledUtterance>(), Array.Empty<EntityType>()).ConfigureAwait(false);
                luis.LuisAppId.Should().Be(appId);
            }
        }

        [Test]
        public static async Task TestSpeechAsyncNoMatchResponse()
        {
            var utterance = Guid.NewGuid().ToString();
            var builder = GetTestLuisBuilder();
            using (var luis = builder.Build())
            {
                var results = await luis.TestSpeechAsync(utterance).ConfigureAwait(false);
                results.Intent.Should().BeNull();
                results.Text.Should().BeNull();
                results.Entities.Should().BeNull();
            }
        }

        private static LuisNLUServiceBuilder GetTestLuisBuilder()
        {
            return new LuisNLUServiceBuilder
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

        private static class Timestamped
        {
            public static Timestamped<T> Create<T>(T instance)
            {
                return new Timestamped<T>(instance);
            }
        }

        private class LuisNLUServiceBuilder
        {
            public string AppName { get; set; }

            public string AppId { get; set; }

            public string AppVersion { get; set; }

            public ILuisClient LuisClient { get; set; }

            public LuisNLUService Build()
            {
                return new LuisNLUService(this.AppName, this.AppId, this.AppVersion, this.LuisClient);
            }
        }

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

            public Task<LuisResult> QueryAsync(string appId, string text, CancellationToken cancellationToken)
            {
                return this.ProcessRequestAsync<LuisResult>(appId, text);
            }

            public Task<LuisResult> RecognizeSpeechAsync(string appId, string speechFile, CancellationToken cancellationToken)
            {
                return this.ProcessRequestAsync<LuisResult>(appId, speechFile);
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
