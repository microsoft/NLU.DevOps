// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace NLU.DevOps.Luis.Tests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using Core;
    using FluentAssertions;
    using FluentAssertions.Json;
    using Microsoft.Azure.CognitiveServices.Language.LUIS.Runtime.Models;
    using Models;
    using Moq;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using Newtonsoft.Json.Serialization;
    using NUnit.Framework;

    [TestFixture]
    internal static class LuisNLUTestClientTests
    {
        [Test]
        public static void ThrowsArgumentNull()
        {
            var luisTestClient = new Mock<ILuisTestClient>().Object;
            Action nullLuisClient = () => new LuisNLUTestClient(null);
            nullLuisClient.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("luisClient");

            using (var luis = new LuisNLUTestClientBuilder().Build())
            {
                Func<Task> nullTestUtterance = () => luis.TestAsync(default(JToken));
                Func<Task> nullTestSpeechUtterance = () => luis.TestSpeechAsync(null);
                nullTestUtterance.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("query");
                nullTestSpeechUtterance.Should().Throw<ArgumentException>().And.ParamName.Should().Be("speechFile");
            }
        }

        [Test]
        public static async Task TestModel()
        {
            var test = "the quick brown fox jumped over the lazy dog";

            var builder = new LuisNLUTestClientBuilder();
            builder.LuisTestClientMock
                .Setup(luis => luis.QueryAsync(
                    It.Is<PredictionRequest>(query => query.Query == test),
                    It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult(new PredictionResponse
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
                }));

            using (var luis = builder.Build())
            {
                var result = await luis.TestAsync(test).ConfigureAwait(false);
                result.Text.Should().Be(test);
                result.Intent.Should().Be("intent");
                result.Entities.Count.Should().Be(1);
                result.Entities[0].EntityType.Should().Be("type");
                result.Entities[0].EntityValue.Should().BeEquivalentTo(new JValue("the"));
                result.Entities[0].MatchText.Should().Be("the");
                result.Entities[0].MatchIndex.Should().Be(1);
            }
        }

        [Test]
        public static async Task TestSpeech()
        {
            var test = "the quick brown fox jumped over the lazy dog entity";
            var testFile = "somefile";

            var builder = new LuisNLUTestClientBuilder();
            builder.LuisTestClientMock
                .Setup(luis => luis.RecognizeSpeechAsync(
                    It.Is<string>(speechFile => speechFile == testFile),
                    It.IsAny<PredictionRequest>(),
                    It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult(new SpeechPredictionResponse(
                    new PredictionResponse
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
                    },
                    0)));

            using (var luis = builder.Build())
            {
                var result = await luis.TestSpeechAsync(testFile).ConfigureAwait(false);
                result.Text.Should().Be(test);
                result.Intent.Should().Be("intent");
                result.Entities.Count.Should().Be(1);
                result.Entities[0].EntityType.Should().Be("type");

                result.Entities[0].MatchText.Should().Be("entity");
                result.Entities[0].MatchIndex.Should().Be(0);
            }
        }

        [Test]
        public static async Task TestSpeechWithTextScore()
        {
            var test = "the quick brown fox jumped over the lazy dog entity";
            var testFile = "somefile";

            var builder = new LuisNLUTestClientBuilder();
            builder.LuisTestClientMock
                .Setup(luis => luis.RecognizeSpeechAsync(
                    It.Is<string>(speechFile => speechFile == testFile),
                    It.IsAny<PredictionRequest>(),
                    It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult(new SpeechPredictionResponse(
                    new PredictionResponse
                    {
                        Query = test,
                        Prediction = new Prediction
                        {
                            TopIntent = "intent",
                        },
                    },
                    0.5)));

            using (var luis = builder.Build())
            {
                var result = await luis.TestSpeechAsync(testFile).ConfigureAwait(false);
                result.Text.Should().Be(test);
                result.Intent.Should().Be("intent");
                result.GetTextScore().Should().Be(0.5);
                result.GetScore().Should().BeNull();
            }
        }

        [Test]
        public static async Task TestSpeechAsyncNoMatchResponse()
        {
            var utterance = Guid.NewGuid().ToString();
            using (var luis = new LuisNLUTestClientBuilder().Build())
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

            var builder = new LuisNLUTestClientBuilder();
            builder.LuisTestClientMock
                .Setup(luis => luis.QueryAsync(
                    It.Is<PredictionRequest>(query => query.Query == test),
                    It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult(new PredictionResponse
                {
                    Query = test,
                    Prediction = new Prediction
                    {
                        TopIntent = "intent",
                    },
                }));

            using (var luis = builder.Build())
            {
                var result = await luis.TestAsync(test).ConfigureAwait(false);
                result.GetScore().Should().BeNull();
            }
        }

        [Test]
        public static async Task WithLabeledIntentScore()
        {
            var test = "the quick brown fox jumped over the lazy dog";

            var builder = new LuisNLUTestClientBuilder();
            builder.LuisTestClientMock
                .Setup(luis => luis.QueryAsync(
                    It.Is<PredictionRequest>(query => query.Query == test),
                    It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult(new PredictionResponse
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
                }));

            using (var luis = builder.Build())
            {
                var result = await luis.TestAsync(test).ConfigureAwait(false);
                result.GetScore().Should().Be(0.42);
            }
        }

        [Test]
        public static async Task NoEntityScore()
        {
            var test = "the quick brown fox jumped over the lazy dog";

            var builder = new LuisNLUTestClientBuilder();
            builder.LuisTestClientMock
                .Setup(luis => luis.QueryAsync(
                    It.Is<PredictionRequest>(query => query.Query == test),
                    It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult(new PredictionResponse
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
                }));

            using (var luis = builder.Build())
            {
                var result = await luis.TestAsync(test).ConfigureAwait(false);
                result.Entities.Count.Should().Be(1);
                result.Entities[0].GetScore().Should().BeNull();
            }
        }

        [Test]
        public static async Task WithEntityScore()
        {
            var test = "the quick brown fox jumped over the lazy dog";

            var builder = new LuisNLUTestClientBuilder();
            builder.LuisTestClientMock
                .Setup(luis => luis.QueryAsync(
                    It.Is<PredictionRequest>(query => query.Query == test),
                    It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult(new PredictionResponse
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
                }));

            using (var luis = builder.Build())
            {
                var result = await luis.TestAsync(test).ConfigureAwait(false);
                result.Entities.Count.Should().Be(1);
                result.Entities[0].GetScore().Should().Be(0.42);
            }
        }

        [Test]
        public static async Task WithMultipleIntents()
        {
            var test = "the quick brown fox jumped over the lazy dog";

            var builder = new LuisNLUTestClientBuilder();
            builder.LuisTestClientMock
                .Setup(luis => luis.QueryAsync(
                    It.Is<PredictionRequest>(query => query.Query == test),
                    It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult(new PredictionResponse
                {
                    Query = test,
                    Prediction = new Prediction
                    {
                        TopIntent = "intent",
                        Intents = new Dictionary<string, Intent>
                        {
                            { "intent", new Intent { Score = 0.42 } },
                            { "foo", new Intent { Score = 0.07 } },
                        },
                    },
                }));

            using (var luis = builder.Build())
            {
                var result = await luis.TestAsync(test).ConfigureAwait(false);
                var serializer = JsonSerializer.CreateDefault();
                serializer.ContractResolver = new CamelCasePropertyNamesContractResolver();
                var intents = JArray.FromObject(result.GetProperty<object>("intents"), serializer);
                intents.Count.Should().Be(2);
                intents[0].Value<string>("intent").Should().Be("intent");
                intents[0].Value<double>("score").Should().Be(0.42);
                intents[1].Value<string>("intent").Should().Be("foo");
                intents[1].Value<double>("score").Should().Be(0.07);
            }
        }

        [Test]
        public static async Task EntityTextDoesNotMatch()
        {
            var test = "show me past - due my past-due tasks";

            var builder = new LuisNLUTestClientBuilder();
            builder.LuisTestClientMock
                .Setup(luis => luis.QueryAsync(
                    It.Is<PredictionRequest>(query => query.Query == test),
                    It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult(new PredictionResponse
                {
                    Query = test,
                    Prediction = new Prediction
                    {
                        TopIntent = "intent",
                        Entities = ToEntityDictionary(new[]
                        {
                            new EntityModel
                            {
                                Entity = "past - due",
                                Type = "type",
                                StartIndex = 22,
                                EndIndex = 29,
                            },
                        }),
                    },
                }));

            using (var luis = builder.Build())
            {
                var result = await luis.TestAsync(test).ConfigureAwait(false);
                result.Entities.Count.Should().Be(1);
                result.Entities[0].MatchText.Should().Be("past-due");
                result.Entities[0].MatchIndex.Should().Be(0);
            }
        }

        [Test]
        public static async Task UtteranceWithNestedMLEntity()
        {
            var test = "i want to request sick leave for 6 days starting march 5";
            var predictionJson = File.ReadAllText("Resources/nested.json");
            var prediction = JObject.Parse(predictionJson).ToObject<PredictionResponse>();

            var builder = new LuisNLUTestClientBuilder();
            builder.LuisTestClientMock
                .Setup(luis => luis.QueryAsync(
                    It.Is<PredictionRequest>(query => query.Query == test),
                    It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult(prediction));

            using (var luis = builder.Build())
            {
                var result = await luis.TestAsync(test).ConfigureAwait(false);
                result.Text.Should().Be(test);
                result.Intent.Should().Be("RequestVacation");
                result.Entities.Count.Should().Be(7);
                result.Entities[0].EntityType.Should().Be("vacation-request::leave-type");
                result.Entities[0].EntityValue.Should().BeEquivalentTo(@"[ ""sick"" ]");
                result.Entities[0].MatchText.Should().Be("sick leave");
                result.Entities[0].MatchIndex.Should().Be(0);
                result.Entities[1].EntityType.Should().Be("vacation-request::days-duration::days-number");
                result.Entities[1].EntityValue.Should().BeEquivalentTo("6");
                result.Entities[1].MatchText.Should().Be("6");
                result.Entities[1].MatchIndex.Should().Be(0);
                result.Entities[2].EntityType.Should().Be("vacation-request::days-duration");
                result.Entities[2].EntityValue.Should().BeEquivalentTo(@"{ ""days-number"": [ 6 ] }");
                result.Entities[2].MatchText.Should().Be("6 days");
                result.Entities[2].MatchIndex.Should().Be(0);
                result.Entities[3].EntityType.Should().Be("vacation-request::start-date");
                result.Entities[3].MatchText.Should().Be("starting march 5");
                result.Entities[3].MatchIndex.Should().Be(0);
                result.Entities[4].EntityType.Should().Be("vacation-request");
                result.Entities[4].EntityValue.Should().ContainSubtree(@"{ ""leave-type"": [ [ ""sick"" ] ], ""days-duration"": [ { ""days-number"": [ 6 ] } ], ""start-date"": [ { ""type"": ""daterange"" } ] }");
                result.Entities[4].MatchText.Should().Be("sick leave for 6 days starting march 5");
                result.Entities[4].MatchIndex.Should().Be(0);
                result.Entities[5].EntityType.Should().Be("datetimeV2");
                result.Entities[5].EntityValue.Should().ContainSubtree(@"{ ""type"": ""duration"" }");
                result.Entities[5].MatchText.Should().Be("6 days");
                result.Entities[5].MatchIndex.Should().Be(0);
                result.Entities[6].EntityType.Should().Be("number");
                result.Entities[6].EntityValue.Should().BeEquivalentTo("5");
                result.Entities[6].MatchText.Should().Be("5");
                result.Entities[6].MatchIndex.Should().Be(0);
            }
        }

        [Test]
        public static void ThrowsInvalidOperationWhenMissingMetadata()
        {
            var test = "foo";
            var builder = new LuisNLUTestClientBuilder();
            builder.LuisTestClientMock
                .Setup(luis => luis.QueryAsync(
                    It.Is<PredictionRequest>(query => query.Query == test),
                    It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult(new PredictionResponse
                {
                    Query = test,
                    Prediction = new Prediction
                    {
                        TopIntent = "intent",
                        Entities = new Dictionary<string, object>
                        {
                            { "entityType", new JArray { 42 } },
                        },
                    },
                }));

            using (var luis = builder.Build())
            {
                Func<Task> testAsync = () => luis.TestAsync(test);
                testAsync.Should().Throw<InvalidOperationException>();
            }
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

                ((JArray)result[entity.Type]).Add(entity.Entity);
                ((JArray)((JObject)result["$instance"])[entity.Type]).Add(entityMetadata);
            }

            return result;
        }

        private class LuisNLUTestClientBuilder
        {
            public Mock<ILuisTestClient> LuisTestClientMock { get; } = new Mock<ILuisTestClient>();

            public LuisNLUTestClient Build()
            {
                return new LuisNLUTestClient(this.LuisTestClientMock.Object);
            }
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
