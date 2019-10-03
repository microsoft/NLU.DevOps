// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace NLU.DevOps.Dialogflow.Tests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;
    using FluentAssertions;
    using FluentAssertions.Json;
    using Google.Cloud.Dialogflow.V2;
    using Google.Protobuf.WellKnownTypes;
    using Grpc.Core;
    using Microsoft.Extensions.Configuration;
    using Models;
    using Moq;
    using Newtonsoft.Json.Linq;
    using NUnit.Framework;

    [TestFixture]
    internal static class DialogflowNLUTestClientTests
    {
        [Test]
        public static void ThrowsArgumentNull()
        {
            Action nullConfiguration = () => new DialogflowNLUTestClient(null);
            nullConfiguration.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("configuration");
        }

        [Test]
        public static void MissingConfigurationThrowsInvalidOperationException()
        {
            var missingKeyJsonConfiguration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    { "dialogflowProjectId", Guid.NewGuid().ToString() },
                })
                .Build();

            var missingProjectIdConfiguration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    { "dialogflowClientKeyJson", Guid.NewGuid().ToString() },
                })
                .Build();

            var missingKeyJsonClient = new DialogflowNLUTestClient(missingKeyJsonConfiguration);
            var missingProjectIdClient = new DialogflowNLUTestClient(missingProjectIdConfiguration);
            Func<Task> missingKeyJson = () => missingKeyJsonClient.TestAsync(new JObject { { "text", string.Empty } });
            Func<Task> missingProjectId = () => missingProjectIdClient.TestAsync(new JObject { { "text", string.Empty } });
            missingKeyJson.Should().Throw<InvalidOperationException>().And.Message.Should().Contain("dialogflowClientKeyJson");
            missingProjectId.Should().Throw<InvalidOperationException>().And.Message.Should().Contain("dialogflowProjectId");
        }

        [Test]
        public static async Task TestAsyncExtractsQueryTextAndIntentDisplayName()
        {
            var queryText = Guid.NewGuid().ToString();
            var intentName = Guid.NewGuid().ToString();

            var client = CreateTestClient(new DetectIntentResponse
            {
                QueryResult = new QueryResult
                {
                    QueryText = queryText,
                    Intent = new Intent { DisplayName = intentName },
                }
            });

            var result = await client.TestAsync(new JObject { { "text", string.Empty } }).ConfigureAwait(false);
            result.Text.Should().Be(queryText);
            result.Intent.Should().Be(intentName);
        }

        [Test]
        [TestCase("\"foo\"")]
        [TestCase("42.0")]
        [TestCase("true")]
        [TestCase("[ true ]")]
        [TestCase("{ \"foo\": true }")]
        public static async Task TestAsyncExtractsJsonEntities(string json)
        {
            var entityType = Guid.NewGuid().ToString();

            var client = CreateTestClient(new DetectIntentResponse
            {
                QueryResult = new QueryResult
                {
                    QueryText = string.Empty,
                    Intent = new Intent { DisplayName = string.Empty },
                    Parameters = Struct.Parser.ParseJson($"{{\"{entityType}\":{json}}}"),
                }
            });

            var result = await client.TestAsync(new JObject { { "text", string.Empty } }).ConfigureAwait(false);
            result.Entities.Count.Should().Be(1);
            result.Entities[0].EntityType.Should().Be(entityType);
            result.Entities[0].EntityValue.Should().BeEquivalentTo(JToken.Parse(json));
        }

        [Test]
        public static async Task TestAsyncNullEntities()
        {
            var entityType1 = Guid.NewGuid().ToString();
            var entityType2 = Guid.NewGuid().ToString();

            var parameters = new Struct();
            parameters.Fields.Add(entityType1, Value.ForNull());
            parameters.Fields.Add(entityType2, Value.ForString(string.Empty));
            var client = CreateTestClient(new DetectIntentResponse
            {
                QueryResult = new QueryResult
                {
                    QueryText = string.Empty,
                    Intent = new Intent { DisplayName = string.Empty },
                    Parameters = parameters,
                }
            });

            var result = await client.TestAsync(new JObject { { "text", string.Empty } }).ConfigureAwait(false);
            result.Entities.Count.Should().Be(0);
        }

        [Test]
        public static async Task TestSpeechAsyncExtractsQueryTextAndIntentDisplayName()
        {
            var queryText = Guid.NewGuid().ToString();
            var intentName = Guid.NewGuid().ToString();

            var client = CreateTestClient(new DetectIntentResponse
            {
                QueryResult = new QueryResult
                {
                    QueryText = queryText,
                    Intent = new Intent { DisplayName = intentName },
                }
            });

            var speechFile = Path.Combine("Assets", "test.txt");
            var result = await client.TestSpeechAsync(speechFile).ConfigureAwait(false);
            result.Text.Should().Be(queryText);
            result.Intent.Should().Be(intentName);
        }

        [Test]
        public static async Task TestSpeechAsyncExtractsEntities()
        {
            var entityType = "foo";
            var entityValue = 42.0;

            var client = CreateTestClient(new DetectIntentResponse
            {
                QueryResult = new QueryResult
                {
                    QueryText = string.Empty,
                    Intent = new Intent { DisplayName = string.Empty },
                    Parameters = Struct.Parser.ParseJson($"{{\"{entityType}\":{entityValue}}}"),
                }
            });

            var speechFile = Path.Combine("Assets", "test.txt");
            var result = await client.TestSpeechAsync(speechFile).ConfigureAwait(false);
            result.Entities.Count.Should().Be(1);
            result.Entities[0].EntityType.Should().Be(entityType);
            result.Entities[0].EntityValue.Should().BeEquivalentTo(new JValue(entityValue));
        }

        [Test]
        public static async Task TestSpeechAsyncReadsSpeechFile()
        {
            var request = default(DetectIntentRequest);
            var client = CreateTestClient(
                new DetectIntentResponse
                {
                    QueryResult = new QueryResult
                    {
                        QueryText = string.Empty,
                        Intent = new Intent { DisplayName = string.Empty },
                    }
                },
                detectIntentRequest => request = detectIntentRequest);

            var speechFile = Path.Combine("Assets", "test.txt");
            await client.TestSpeechAsync(speechFile).ConfigureAwait(false);
            request.Should().NotBeNull();
            request.InputAudio.ToStringUtf8().Should().EndWith("hello");
        }

        [Test]
        public static async Task TestAsyncRetries()
        {
            var intentName = Guid.NewGuid().ToString();
            var throwCount = 1;
            var client = CreateTestClient(
                new DetectIntentResponse
                {
                    QueryResult = new QueryResult
                    {
                        QueryText = string.Empty,
                        Intent = new Intent { DisplayName = intentName },
                    }
                },
                _ =>
                {
                    if (throwCount-- > 0)
                    {
                        throw new RpcException(new Status(StatusCode.ResourceExhausted, string.Empty));
                    }
                });

            var result = await client.TestAsync(new JObject { { "text", string.Empty } }).ConfigureAwait(false);
            result.Intent.Should().Be(intentName);
        }

        [Test]
        public static async Task TestSpeechAsyncRetries()
        {
            var intentName = Guid.NewGuid().ToString();
            var throwCount = 1;
            var client = CreateTestClient(
                new DetectIntentResponse
                {
                    QueryResult = new QueryResult
                    {
                        QueryText = string.Empty,
                        Intent = new Intent { DisplayName = intentName },
                    }
                },
                _ =>
                {
                    if (throwCount-- > 0)
                    {
                        throw new RpcException(new Status(StatusCode.ResourceExhausted, string.Empty));
                    }
                });

            var speechFile = Path.Combine("Assets", "test.txt");
            var result = await client.TestSpeechAsync(speechFile).ConfigureAwait(false);
            result.Intent.Should().Be(intentName);
        }

        private static DialogflowNLUTestClient CreateTestClient(DetectIntentResponse response, Action<DetectIntentRequest> callback = null)
        {
            var mockCallInvoker = new Mock<CallInvoker>();
            mockCallInvoker
                .Setup(invoker => invoker.AsyncUnaryCall(
                    It.Is<Method<DetectIntentRequest, DetectIntentResponse>>(method => method.Name == "DetectIntent"),
                    It.IsAny<string>(),
                    It.IsAny<CallOptions>(),
                    It.IsAny<DetectIntentRequest>()))
                .Returns(new AsyncUnaryCall<DetectIntentResponse>(
                        Task.FromResult(response),
                        Task.FromResult(default(Metadata)),
                        () => Status.DefaultSuccess,
                        () => default(Metadata),
                        () => { }))
                .Callback((
                    Method<DetectIntentRequest, DetectIntentResponse> method,
                    string host,
                    CallOptions callOptions,
                    DetectIntentRequest request) =>
                        callback?.Invoke(request));

            var sessionsClient = new SessionsClientBuilder
                {
                    CallInvoker = mockCallInvoker.Object
                }
                .Build();

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    { "dialogflowProjectId", Guid.NewGuid().ToString() },
                })
                .Build();

            return new DialogflowNLUTestClient(sessionsClient, configuration);
        }
    }
}
