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
    internal static class LuisNLUTestClientTests
    {
        [Test]
        public static void ThrowsArgumentNull()
        {
            Action nullLuisSettings = () => new LuisNLUTestClient(null, new MockLuisTestClient());
            Action nullLuisClient = () => new LuisNLUTestClient(new LuisSettings(), null);
            nullLuisSettings.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("luisSettings");
            nullLuisClient.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("luisClient");

            using (var luis = GetTestLuisBuilder().Build())
            {
                Func<Task> nullTestUtterance = () => luis.TestAsync(default(INLUQuery));
                Func<Task> nullTestSpeechUtterance = () => luis.TestSpeechAsync(null);
                nullTestUtterance.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("query");
                nullTestSpeechUtterance.Should().Throw<ArgumentException>().And.ParamName.Should().Be("speechFile");
            }
        }

        [Test]
        public static async Task TestModel()
        {
            var test = "the quick brown fox jumped over the lazy dog";

            var mockClient = new MockLuisTestClient();
            mockClient.OnRequestResponse = request =>
            {
                if (request.Method == nameof(ILuisTestClient.QueryAsync))
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

            var mockClient = new MockLuisTestClient();
            mockClient.OnRequestResponse = request =>
            {
                if (request.Method == nameof(ILuisTestClient.QueryAsync))
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

            var mockClient = new MockLuisTestClient();
            mockClient.OnRequestResponse = request =>
            {
                if (request.Method == nameof(ILuisTestClient.RecognizeSpeechAsync))
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
                var result = await luis.TestSpeechAsync("somefile").ConfigureAwait(false);
                result.Text.Should().Be(test);
                result.Intent.Should().Be("intent");
                result.Entities.Count.Should().Be(1);
                result.Entities[0].EntityType.Should().Be("type");

                result.Entities[0].MatchText.Should().Be("entity");
                result.Entities[0].MatchIndex.Should().Be(0);
            }
        }

        [Test]
        public static async Task TestWithPrebuiltEntity()
        {
            var test = "the quick brown fox jumped over the lazy dog";
            var builtinType = Guid.NewGuid().ToString();
            var mockClient = new MockLuisTestClient();
            mockClient.OnRequestResponse = request =>
            {
                if (request.Method == nameof(ILuisTestClient.QueryAsync))
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

            var prebuiltEntityTypes = new Dictionary<string, string>
            {
                { "type", "test" },
            };

            var builder = GetTestLuisBuilder();
            builder.LuisClient = mockClient;
            builder.LuisSettings = new LuisSettings(prebuiltEntityTypes);
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

        [Test]
        public static async Task NoLabeledIntentScore()
        {
            var test = "the quick brown fox jumped over the lazy dog";

            var mockClient = new MockLuisTestClient();
            mockClient.OnRequestResponse = request =>
            {
                if (request.Method == nameof(ILuisTestClient.QueryAsync))
                {
                    return new LuisResult
                    {
                        Query = test,
                        TopScoringIntent = new IntentModel { Intent = "intent" },
                    };
                }

                return null;
            };

            var builder = GetTestLuisBuilder();
            builder.LuisClient = mockClient;

            using (var luis = builder.Build())
            {
                var result = await luis.TestAsync(test).ConfigureAwait(false);
                result.Should().BeOfType(typeof(Models.LabeledUtterance));
            }
        }

        [Test]
        public static async Task WithLabeledIntentScore()
        {
            var test = "the quick brown fox jumped over the lazy dog";

            var mockClient = new MockLuisTestClient();
            mockClient.OnRequestResponse = request =>
            {
                if (request.Method == nameof(ILuisTestClient.QueryAsync))
                {
                    return new LuisResult
                    {
                        Query = test,
                        TopScoringIntent = new IntentModel { Intent = "intent", Score = 0.42 },
                    };
                }

                return null;
            };

            var builder = GetTestLuisBuilder();
            builder.LuisClient = mockClient;

            using (var luis = builder.Build())
            {
                var result = await luis.TestAsync(test).ConfigureAwait(false);
                result.Should().BeOfType(typeof(ScoredLabeledUtterance));
                result.As<ScoredLabeledUtterance>().Score.Should().Be(0.42);
            }
        }

        [Test]
        public static async Task NoEntityScore()
        {
            var test = "the quick brown fox jumped over the lazy dog";

            var mockClient = new MockLuisTestClient();
            mockClient.OnRequestResponse = request =>
            {
                if (request.Method == nameof(ILuisTestClient.QueryAsync))
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
                result.Entities.Count.Should().Be(1);
                result.Entities[0].Should().BeOfType(typeof(Entity));
            }
        }

        [Test]
        public static async Task WithEntityScore()
        {
            var test = "the quick brown fox jumped over the lazy dog";

            var mockClient = new MockLuisTestClient();
            mockClient.OnRequestResponse = request =>
            {
                if (request.Method == nameof(ILuisTestClient.QueryAsync))
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
                                AdditionalProperties = new Dictionary<string, object>
                                {
                                    { "score", 0.42 },
                                },
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
                result.Entities.Count.Should().Be(1);
                result.Entities[0].Should().BeOfType(typeof(ScoredEntity));
                result.Entities[0].As<ScoredEntity>().Score.Should().Be(0.42);
            }
        }

        private static LuisNLUTestClientBuilder GetTestLuisBuilder()
        {
            return new LuisNLUTestClientBuilder
            {
                LuisSettings = new LuisSettings(null, null),
                LuisClient = new MockLuisTestClient(),
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

        private class LuisNLUTestClientBuilder
        {
            public LuisSettings LuisSettings { get; set; }

            public ILuisTestClient LuisClient { get; set; }

            public LuisNLUTestClient Build() =>
                new LuisNLUTestClient(this.LuisSettings, this.LuisClient);
        }

        private sealed class MockLuisTestClient : ILuisTestClient
        {
            public Action<LuisRequest> OnRequest { get; set; }

            public Func<LuisRequest, object> OnRequestResponse { get; set; }

            public IEnumerable<LuisRequest> Requests => this.RequestsInternal.Select(x => x.Instance);

            public IEnumerable<Timestamped<LuisRequest>> TimestampedRequests => this.RequestsInternal;

            private List<Timestamped<LuisRequest>> RequestsInternal { get; } = new List<Timestamped<LuisRequest>>();

            public Task<LuisResult> QueryAsync(string text, CancellationToken cancellationToken)
            {
                return this.ProcessRequestAsync<LuisResult>(text);
            }

            public Task<LuisResult> RecognizeSpeechAsync(string speechFile, CancellationToken cancellationToken)
            {
                return this.ProcessRequestAsync<LuisResult>(speechFile);
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
