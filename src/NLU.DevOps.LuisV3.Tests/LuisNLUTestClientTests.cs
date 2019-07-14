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
                    return new PredictionResponse
                    {
                        Query = test,
                        Prediction = new Prediction
                        {
                            TopIntent = "intent",
                            Entities = ToEntityDictionary(new[]
                            {
                                new EntityModel
                                {
                                    Entity = "the",
                                    Type = "type",
                                    StartIndex = 32,
                                    EndIndex = 34,
                                },
                            }),
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
                    return new PredictionResponse
                    {
                        Query = "the quick brown fox jumped over the lazy dog today",
                        Prediction = new Prediction
                        {
                            Entities = new Dictionary<string, object>
                            {
                                {
                                    "type",
                                    new JArray
                                    {
                                        "2018-11-16",
                                        new JArray { new JArray { "Fox" } },
                                        new JArray { new JArray { "foo", "bar" } },
                                        new JArray { new JObject() }
                                    }
                                },
                                {
                                    "$instance",
                                    new JObject
                                    {
                                        {
                                            "type",
                                            new JArray
                                            {
                                                new JObject
                                                {
                                                    { "startIndex", 45 },
                                                    { "length", 5 },
                                                },
                                                new JObject
                                                {
                                                    { "startIndex", 10 },
                                                    { "length", 9 },
                                                },
                                                new JObject
                                                {
                                                    { "startIndex", 0 },
                                                    { "length", 3 },
                                                },
                                                new JObject
                                                {
                                                    { "startIndex", 4 },
                                                    { "length", 5 },
                                                },
                                            }
                                        },
                                    }
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
                var result = await luis.TestAsync(test).ConfigureAwait(false);
                result.Entities.Count.Should().Be(4);
                result.Entities[0].MatchText.Should().Be("today");
                result.Entities[0].EntityValue.Should().Be("2018-11-16");
                result.Entities[1].MatchText.Should().Be("brown fox");
                result.Entities[1].EntityValue.Should().Be(null);
                result.Entities[2].MatchText.Should().Be("the");
                result.Entities[2].EntityValue.Should().Be(null);
                result.Entities[3].MatchText.Should().Be("quick");
                result.Entities[3].EntityValue.Should().Be(null);
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
                    return new PredictionResponse
                    {
                        Query = test,
                        Prediction = new Prediction
                        {
                            TopIntent = "intent",
                            Entities = ToEntityDictionary(new[]
                            {
                                new EntityModel
                                {
                                    Entity = "entity",
                                    Type = "type",
                                    StartIndex = 45,
                                    EndIndex = 50,
                                },
                            }),
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
                    return new PredictionResponse
                    {
                        Query = test,
                        Prediction = new Prediction
                        {
                            TopIntent = "intent",
                            Entities = ToEntityDictionary(new[]
                            {
                                new EntityModel
                                {
                                    Entity = "the",
                                    Type = "builtin.test",
                                    StartIndex = 32,
                                    EndIndex = 34
                                },
                            }),
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
                    return new PredictionResponse
                    {
                        Query = test,
                        Prediction = new Prediction
                        {
                            TopIntent = "intent",
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
                    return new PredictionResponse
                    {
                        Query = test,
                        Prediction = new Prediction
                        {
                            TopIntent = "intent",
                            Intents = new Dictionary<string, Intent>
                            {
                                { "intent", new Intent { Score = 0.42 } },
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
                    return new PredictionResponse
                    {
                        Query = test,
                        Prediction = new Prediction
                        {
                            TopIntent = "intent",
                            Entities = ToEntityDictionary(new[]
                            {
                                new EntityModel
                                {
                                    Entity = "the",
                                    Type = "type",
                                    StartIndex = 32,
                                    EndIndex = 34,
                                },
                            }),
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
                    return new PredictionResponse
                    {
                        Query = test,
                        Prediction = new Prediction
                        {
                            TopIntent = "intent",
                            Entities = ToEntityDictionary(new[]
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
                            }),
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
                result.Entities.Count.Should().Be(1);
                result.Entities[0].Should().BeOfType(typeof(ScoredEntity));
                result.Entities[0].As<ScoredEntity>().Score.Should().Be(0.42);
            }
        }

        private static LuisNLUServiceBuilder GetTestLuisBuilder()
        {
            return new LuisNLUServiceBuilder
            {
                LuisSettings = new LuisSettings(null, null),
                LuisClient = new MockLuisTestClient(),
            };
        }

        private static IDictionary<string, object> ToEntityDictionary(IEnumerable<EntityModel> entities)
        {
            var result = new Dictionary<string, object>();
            foreach (var entity in entities)
            {
                if (!result.TryGetValue(entity.Type, out var value))
                {
                    result.Add(entity.Type, new JArray());
                    if (!result.TryGetValue("$instance", out var instanceValue))
                    {
                        instanceValue = new JObject();
                        result.Add("$instance", instanceValue);
                    }

                    var instanceJson = (JObject)instanceValue;
                    instanceJson.Add(entity.Type, new JArray());
                }

                var entityMetadata = new JObject
                {
                    { "startIndex", entity.StartIndex },
                    { "length", entity.EndIndex - entity.StartIndex + 1 }
                };

                if (entity.AdditionalProperties != null && entity.AdditionalProperties.TryGetValue("score", out var score))
                {
                    entityMetadata.Add("score", (double)score);
                }

                ((JArray)result[entity.Type]).Add(null);
                ((JArray)((JObject)result["$instance"])[entity.Type]).Add(entityMetadata);
            }

            return result;
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
            public LuisSettings LuisSettings { get; set; }

            public ILuisTestClient LuisClient { get; set; }

            public LuisNLUTestClient Build()
            {
                return new LuisNLUTestClient(this.LuisSettings, this.LuisClient);
            }
        }

        private sealed class MockLuisTestClient : ILuisTestClient
        {
            public Action<LuisRequest> OnRequest { get; set; }

            public Func<LuisRequest, object> OnRequestResponse { get; set; }

            public IEnumerable<LuisRequest> Requests => this.RequestsInternal.Select(x => x.Instance);

            public IEnumerable<Timestamped<LuisRequest>> TimestampedRequests => this.RequestsInternal;

            private List<Timestamped<LuisRequest>> RequestsInternal { get; } = new List<Timestamped<LuisRequest>>();

            public Task<PredictionResponse> QueryAsync(PredictionRequest predictionRequest, CancellationToken cancellationToken)
            {
                return this.ProcessRequestAsync<PredictionResponse>(predictionRequest);
            }

            public Task<PredictionResponse> RecognizeSpeechAsync(string speechFile, PredictionRequest predictionRequest, CancellationToken cancellationToken)
            {
                return this.ProcessRequestAsync<PredictionResponse>(speechFile, predictionRequest);
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

        private sealed class EntityModel
        {
            public string Entity { get; set; }

            public string Type { get; set; }

            public int StartIndex { get; set; }

            public int EndIndex { get; set; }

            public IDictionary<string, object> AdditionalProperties { get; set; }
        }
    }
}
