// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace NLU.DevOps.Lex.Tests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Amazon.LexModelBuildingService;
    using Amazon.LexModelBuildingService.Model;
    using FluentAssertions;
    using Models;
    using Moq;
    using Newtonsoft.Json.Linq;
    using NUnit.Framework;

    [TestFixture]
    internal static class LexNLUTrainClientTests
    {
        private static readonly TimeSpan Epsilon = TimeSpan.FromMilliseconds(100);

        [Test]
        public static void ThrowsArgumentNull()
        {
            var nullBotName = new Action(() => new LexNLUTrainClient(null, string.Empty, null, default(ILexTrainClient)));
            var nullBotAlias = new Action(() => new LexNLUTrainClient(string.Empty, null, null, default(ILexTrainClient)));
            var nullImportBotTemplate = new Action(() => new LexNLUTrainClient(string.Empty, string.Empty, null, default(ILexTrainClient)));
            var nullLexClient = new Action(() => new LexNLUTrainClient(string.Empty, string.Empty, new JObject(), default(ILexTrainClient)));
            nullBotName.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("botName");
            nullBotAlias.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("botAlias");
            nullImportBotTemplate.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("importBotTemplate");
            nullLexClient.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("lexClient");

            using (var lex = new LexNLUTrainClient(string.Empty, string.Empty, new JObject(), new Mock<ILexTrainClient>().Object))
            {
                var nullUtterances = new Func<Task>(() => lex.TrainAsync(null));
                var nullUtteranceItem = new Func<Task>(() => lex.TrainAsync(new LabeledUtterance[] { null }));
                nullUtterances.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("utterances");
                nullUtteranceItem.Should().Throw<ArgumentException>().And.ParamName.Should().Be("utterances");
            }
        }

        [Test]
        public static void MissingEntityMatchInTextThrowsInvalidOperation()
        {
            var text = "foo";
            var match = "bar";
            var mockClient = CreateLexTrainClientMock();
            using (var lex = new LexNLUTrainClient(string.Empty, string.Empty, new JObject(), mockClient.Object))
            {
                var entity = new Entity(null, null, match, 0);
                var utterance = new LabeledUtterance(text, null, new[] { entity });
                var invalidEntityMatch = new Func<Task>(() => lex.TrainAsync(new[] { utterance }));
                invalidEntityMatch.Should().Throw<InvalidOperationException>();
            }
        }

        [Test]
        public static async Task CreatesBot()
        {
            var text = "hello world";
            var intent = Guid.NewGuid().ToString();
            var entityTypeName = "Planet";
            var botName = Guid.NewGuid().ToString();

            var payload = default(JObject);
            var mockClient = CreateLexTrainClientMock();
            mockClient.Setup(lex => lex.StartImportAsync(
                    It.IsAny<StartImportRequest>(),
                    It.IsAny<CancellationToken>()))
                .Callback<StartImportRequest, CancellationToken>((request, cancellationToken) =>
                    payload = GetPayloadJson(request.Payload));

            var slot = CreateSlot(entityTypeName, entityTypeName);
            using (var lex = new LexNLUTrainClient(botName, string.Empty, new JObject(), mockClient.Object))
            {
                var entity = new Entity(entityTypeName, "Earth", "world", 0);
                var utterance = new LabeledUtterance(text, intent, new[] { entity });

                await lex.TrainAsync(new[] { utterance }).ConfigureAwait(false);

                // assert payload
                payload.Should().NotBeNull();

                // assert name is set
                payload.SelectToken(".resource.name").Value<string>().Should().Be(botName);

                // assert intent is created
                payload.SelectToken(".resource.intents").Count().Should().Be(1);
                payload.SelectToken(".resource.intents[0].name").Value<string>().Should().Be(intent);

                // assert template utterance is set
                payload.SelectToken(".resource.intents[0].sampleUtterances").Count().Should().Be(1);
                payload.SelectToken(".resource.intents[0].sampleUtterances[0]").Value<string>().Should().Be("hello {Planet}");

                // assert slot is created in intent
                payload.SelectToken(".resource.intents[0].slots").Count().Should().Be(1);
                payload.SelectToken(".resource.intents[0].slots[0].name").Value<string>().Should().Be(entityTypeName);
                payload.SelectToken(".resource.intents[0].slots[0].slotType").Value<string>().Should().Be(entityTypeName);
            }
        }

        [Test]
        [TestCase("food foo", "foo", "x", 0, "{x}d foo")]
        [TestCase("foo'd foo", "foo", "x", 0, "{x}'d foo")]
        [TestCase("foo'd foo", "foo", "x", 1, "foo'd {x}")]
        [TestCase("foo foo foo", "foo", "x", 2, "foo foo {x}")]
        public static async Task ReplacesCorrectTokensInSampleUtterances(
            string text,
            string entityMatch,
            string entityTypeName,
            int matchIndex,
            string sampleUtterance)
        {
            var intent = Guid.NewGuid().ToString();

            var payload = default(JObject);
            var mockClient = CreateLexTrainClientMock();
            mockClient.Setup(lex => lex.StartImportAsync(
                    It.IsAny<StartImportRequest>(),
                    It.IsAny<CancellationToken>()))
                .Callback<StartImportRequest, CancellationToken>((request, cancellationToken) =>
                    payload = GetPayloadJson(request.Payload));

            var slot = CreateSlot(entityTypeName, entityTypeName);
            using (var lex = new LexNLUTrainClient(string.Empty, string.Empty, new JObject(), mockClient.Object))
            {
                var entity = new Entity(entityTypeName, null, entityMatch, matchIndex);
                var utterance = new LabeledUtterance(text, intent, new[] { entity });

                await lex.TrainAsync(new[] { utterance }).ConfigureAwait(false);

                // assert template utterance is set
                payload.Should().NotBeNull();
                payload.SelectToken(".resource.intents[0].sampleUtterances").Count().Should().Be(1);
                payload.SelectToken(".resource.intents[0].sampleUtterances[0]").Value<string>().Should().Be(sampleUtterance);
            }
        }

        [Test]
        public static async Task WaitsForImportCompletion()
        {
            var importId = Guid.NewGuid().ToString();

            var mockClient = CreateLexTrainClientMock();
            mockClient.SetReturnsDefault(Task.FromResult(new StartImportResponse
                {
                    ImportId = importId,
                    ImportStatus = ImportStatus.IN_PROGRESS,
                }));

            var count = 0;
            var timestamps = new DateTimeOffset[2];
            mockClient.Setup(lex => lex.GetImportAsync(
                    It.Is<GetImportRequest>(request => request.ImportId == importId),
                    It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult(new GetImportResponse
                {
                    ImportId = importId,
                    ImportStatus = ++count < 2 ? ImportStatus.IN_PROGRESS : ImportStatus.COMPLETE,
                }))
                .Callback(() => timestamps[count - 1] = DateTimeOffset.Now);

            using (var lex = new LexNLUTrainClient(string.Empty, string.Empty, new JObject(), mockClient.Object))
            {
                var utterance = new LabeledUtterance(string.Empty, string.Empty, null);

                await lex.TrainAsync(new[] { utterance }).ConfigureAwait(false);

                // Assert that the time difference is at least two seconds
                var difference = timestamps[1] - timestamps[0];
                difference.Should().BeGreaterThan(TimeSpan.FromSeconds(2) - Epsilon);
            }
        }

        [Test]
        public static void ImportFailureThrowsInvalidOperation()
        {
            var importId = Guid.NewGuid().ToString();
            var failureReason = new List<string>
            {
                Guid.NewGuid().ToString(),
                Guid.NewGuid().ToString(),
            };

            var mockClient = CreateLexTrainClientMock();
            mockClient.SetReturnsDefault(Task.FromResult(new StartImportResponse
                {
                    ImportId = importId,
                    ImportStatus = ImportStatus.FAILED,
                }));

            mockClient.Setup(lex => lex.GetImportAsync(
                    It.Is<GetImportRequest>(request => request.ImportId == importId),
                    It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult(new GetImportResponse
                {
                    ImportId = importId,
                    ImportStatus = ImportStatus.FAILED,
                    FailureReason = failureReason,
                }));

            using (var lex = new LexNLUTrainClient(string.Empty, string.Empty, new JObject(), mockClient.Object))
            {
                var utterance = new LabeledUtterance(string.Empty, string.Empty, null);

                // Null failure reason should be okay
                var importFails = new Func<Task>(() => lex.TrainAsync(new[] { utterance }));
                var expectedMessage = string.Join(Environment.NewLine, failureReason);
                importFails.Should().Throw<InvalidOperationException>().And.Message.Should().Be(expectedMessage);
            }
        }

        [Test]
        public static void DisposesLexClient()
        {
            var handle = new ManualResetEvent(false);
            var mockClient = CreateLexTrainClientMock();
            mockClient.Setup(client => client.Dispose())
                .Callback(() => handle.Set());

            var lex = new LexNLUTrainClient(string.Empty, string.Empty, new JObject(), mockClient.Object);
            lex.Dispose();

            handle.WaitOne(5000).Should().BeTrue();
        }

        [Test]
        public static async Task WaitsForBuildCompletion()
        {
            var botName = Guid.NewGuid().ToString();

            var count = 0;
            var timestamps = new DateTimeOffset[3];
            var mockClient = CreateLexTrainClientMock();
            mockClient.Setup(lex => lex.GetBotAsync(
                    It.Is<GetBotRequest>(request => request.Name == botName),
                    It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult(new GetBotResponse
                {
                    AbortStatement = new Statement { Messages = { new Message() } },
                    ClarificationPrompt = new Prompt { Messages = { new Message() } },
                    Status = ++count < 3 ? Status.BUILDING : Status.READY,
                }))
                .Callback(() => timestamps[count - 1] = DateTimeOffset.Now);

            using (var lex = new LexNLUTrainClient(botName, string.Empty, new JObject(), mockClient.Object))
            {
                var utterance = new LabeledUtterance(string.Empty, string.Empty, null);
                await lex.TrainAsync(new[] { utterance }).ConfigureAwait(false);

                var difference = timestamps[2] - timestamps[1];
                difference.Should().BeGreaterThan(TimeSpan.FromSeconds(2) - Epsilon);
            }
        }

        [Test]
        [TestCase("FAILED", true)]
        [TestCase("UNKNOWN", true)]
        [TestCase("NOT_BUILT", false)]
        public static void BuildFailureThrowsInvalidOperation(string status, bool shouldThrow)
        {
            var botName = Guid.NewGuid().ToString();

            var count = 0;
            var mockClient = CreateLexTrainClientMock();
            mockClient.Setup(lex => lex.GetBotAsync(
                    It.Is<GetBotRequest>(request => request.Name == botName),
                    It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult(new GetBotResponse
                {
                    AbortStatement = new Statement { Messages = { new Message() } },
                    ClarificationPrompt = new Prompt { Messages = { new Message() } },
                    Status = ++count < 2 ? Status.NOT_BUILT : new Status(status),
                }));

            using (var lex = new LexNLUTrainClient(botName, string.Empty, new JObject(), mockClient.Object))
            {
                var utterance = new LabeledUtterance(string.Empty, string.Empty, null);
                var buildFailed = new Func<Task>(() => lex.TrainAsync(new[] { utterance }));

                if (shouldThrow)
                {
                    buildFailed.Should().Throw<InvalidOperationException>();
                }
                else
                {
                    buildFailed.Should().NotThrow<InvalidOperationException>();
                }
            }
        }

        [Test]
        public static async Task CleanupCallsLexActions()
        {
            var botName = Guid.NewGuid().ToString();
            var botAlias = Guid.NewGuid().ToString();
            var mockClient = CreateLexTrainClientMock();
            using (var lex = new LexNLUTrainClient(botName, botAlias, new JObject(), mockClient.Object))
            {
                await lex.CleanupAsync().ConfigureAwait(false);
                mockClient.Invocations.Select(i => i.Arguments[0]).OfType<DeleteBotAliasRequest>().Count().Should().Be(1);
                mockClient.Invocations.Select(i => i.Arguments[0]).OfType<DeleteBotAliasRequest>().First().Name.Should().Be(botAlias);
                mockClient.Invocations.Select(i => i.Arguments[0]).OfType<DeleteBotRequest>().Count().Should().Be(1);
                mockClient.Invocations.Select(i => i.Arguments[0]).OfType<DeleteBotRequest>().First().Name.Should().Be(botName);
            }
        }

        [Test]
        public static async Task CleanupSucceedsWithNotFound()
        {
            var botName = Guid.NewGuid().ToString();
            var botAlias = Guid.NewGuid().ToString();
            var mockClient = CreateLexTrainClientMock();

            mockClient.Setup(lex => lex.DeleteBotAsync(
                    It.Is<DeleteBotRequest>(request => request.Name == botName),
                    It.IsAny<CancellationToken>()))
                .Throws(new NotFoundException(string.Empty));

            mockClient.Setup(lex => lex.DeleteBotAliasAsync(
                    It.Is<DeleteBotAliasRequest>(request => request.Name == botAlias),
                    It.IsAny<CancellationToken>()))
                .Throws(new NotFoundException(string.Empty));

            using (var lex = new LexNLUTrainClient(botName, botAlias, new JObject(), mockClient.Object))
            {
                await lex.CleanupAsync().ConfigureAwait(false);
                mockClient.Invocations.Select(i => i.Arguments[0]).OfType<DeleteBotAliasRequest>().Count().Should().Be(1);
                mockClient.Invocations.Select(i => i.Arguments[0]).OfType<DeleteBotAliasRequest>().First().Name.Should().Be(botAlias);
                mockClient.Invocations.Select(i => i.Arguments[0]).OfType<DeleteBotRequest>().Count().Should().Be(1);
                mockClient.Invocations.Select(i => i.Arguments[0]).OfType<DeleteBotRequest>().First().Name.Should().Be(botName);
            }
        }

        [Test]
        public static async Task DoesNotCreateIfBotExists()
        {
            var botName = Guid.NewGuid().ToString();
            var mockClient = CreateLexTrainClientMock();
            mockClient.SetReturnsDefault(Task.FromResult(new GetBotsResponse
            {
                Bots = new List<BotMetadata>
                {
                    new BotMetadata
                    {
                        Name = botName,
                    },
                },
            }));

            using (var lex = new LexNLUTrainClient(botName, string.Empty, new JObject(), mockClient.Object))
            {
                await lex.TrainAsync(Array.Empty<LabeledUtterance>()).ConfigureAwait(false);

                // There are at most two put bot requests, the first to create the bot, the second to build
                // If the bot exists, only the second put bot to build should occur.
                mockClient.Invocations.Select(i => i.Arguments[0]).OfType<PutBotRequest>().Count().Should().Be(1);
                mockClient.Invocations.Select(i => i.Arguments[0]).OfType<PutBotRequest>().First().ProcessBehavior.Should().Be(ProcessBehavior.BUILD);
            }
        }

        [Test]
        public static async Task DoesNotPublishIfAliasExists()
        {
            var botAlias = Guid.NewGuid().ToString();
            var mockClient = CreateLexTrainClientMock();
            mockClient.SetReturnsDefault(Task.FromResult(new GetBotAliasesResponse
            {
                BotAliases = new List<BotAliasMetadata>
                {
                    new BotAliasMetadata
                    {
                        Name = botAlias,
                    },
                },
            }));

            using (var lex = new LexNLUTrainClient(string.Empty, botAlias, new JObject(), mockClient.Object))
            {
                await lex.TrainAsync(Array.Empty<LabeledUtterance>()).ConfigureAwait(false);

                // If the bot alias exists, the 'PutBotAlias' request should not occur
                mockClient.Invocations.Select(i => i.Arguments[0]).OfType<PutBotAliasRequest>().Count().Should().Be(0);
            }
        }

        [Test]
        public static async Task DoesNotOverwriteIntent()
        {
            var canary = Guid.NewGuid().ToString();
            var intentName = Guid.NewGuid().ToString();
            var entityTypeName = Guid.NewGuid().ToString();
            var existingUtterance = Guid.NewGuid().ToString();

            var existingSlot = new JObject
            {
                { "name", entityTypeName },
                { "slotType", canary },
            };

            var existingIntent = new JObject
            {
                { "name", intentName },
                { "canary", canary },
                { "sampleUtterances", new JArray { existingUtterance } },
                { "slots", new JArray { existingSlot } },
            };

            var importBotTemplate = new JObject
            {
                {
                    "resource",
                    new JObject
                    {
                        { "intents", new JArray { existingIntent } },
                    }
                }
            };

            var payload = default(JObject);
            var mockClient = CreateLexTrainClientMock();
            mockClient.Setup(lex => lex.StartImportAsync(
                    It.IsAny<StartImportRequest>(),
                    It.IsAny<CancellationToken>()))
                .Callback<StartImportRequest, CancellationToken>(
                    (request, cancellationToken) => payload = GetPayloadJson(request.Payload));

            var slot = CreateSlot(entityTypeName, Guid.NewGuid().ToString());
            using (var lex = new LexNLUTrainClient(string.Empty, string.Empty, importBotTemplate, mockClient.Object))
            {
                var text = Guid.NewGuid().ToString();
                var entity = new Entity(entityTypeName, null, text, 0);
                var utterance = new LabeledUtterance(text, intentName, new[] { entity });
                await lex.TrainAsync(new[] { utterance }).ConfigureAwait(false);

                // get StartImport request
                var startImportRequest = mockClient.Invocations.Select(i => i.Arguments[0]).OfType<StartImportRequest>().FirstOrDefault();
                startImportRequest.Should().NotBeNull();

                // assert payload
                payload.Should().NotBeNull();

                // get intent
                var intents = payload.SelectTokens($".resource.intents[?(@.name == '{intentName}')]");
                intents.Count().Should().Be(1);
                intents.First().Value<string>("canary").Should().Be(canary);
                intents.First().SelectToken(".sampleUtterances").Count().Should().Be(2);
                intents.First().SelectToken(".sampleUtterances").Should().Contain(u => u.Value<string>() == existingUtterance);
                intents.First().SelectToken(".sampleUtterances").Should().Contain(u => u.Value<string>() == $"{{{entityTypeName}}}");
                intents.First().SelectToken(".slots").Count().Should().Be(1);
                intents.First().SelectToken(".slots").First().Value<string>("slotType").Should().Be(canary);
                intents.First().SelectToken(".slots").First().Value<string>("slotConstraint").Should().Be("Optional");
            }
        }

        private static JToken CreateSlot(string name, string slotType)
        {
            return new JObject
            {
                { "name", name },
                { "slotType", slotType },
            };
        }

        private static JObject GetPayloadJson(Stream payloadStream)
        {
            using (var zipArchive = new ZipArchive(payloadStream, ZipArchiveMode.Read))
            using (var streamReader = new StreamReader(zipArchive.Entries.Single().Open()))
            {
                return JObject.Parse(streamReader.ReadToEnd());
            }
        }

        private static Mock<ILexTrainClient> CreateLexTrainClientMock()
        {
            var mockClient = new Mock<ILexTrainClient>();

            mockClient.SetReturnsDefault(Task.FromResult(new GetBotsResponse()));
            mockClient.SetReturnsDefault(Task.FromResult(new GetBotAliasesResponse()));
            mockClient.SetReturnsDefault(Task.FromResult(new GetImportResponse()));

            mockClient.SetReturnsDefault(Task.FromResult(new GetBotResponse
            {
                AbortStatement = new Statement { Messages = { new Message() } },
                ClarificationPrompt = new Prompt { Messages = { new Message() } },
                Status = Status.READY,
            }));

            mockClient.SetReturnsDefault(Task.FromResult(new StartImportResponse
            {
                ImportStatus = ImportStatus.COMPLETE,
            }));

            return mockClient;
        }
    }
}
