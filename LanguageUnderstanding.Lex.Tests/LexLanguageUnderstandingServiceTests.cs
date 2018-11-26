// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace LanguageUnderstanding.Lex.Tests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Amazon.Lex.Model;
    using Amazon.LexModelBuildingService;
    using Amazon.LexModelBuildingService.Model;
    using FluentAssertions;
    using Models;
    using Newtonsoft.Json.Linq;
    using NUnit.Framework;

    [TestFixture]
    internal static class LexLanguageUnderstandingServiceTests
    {
        private const string TemplatesDirectory = "Templates";
        private static readonly TimeSpan Epsilon = TimeSpan.FromMilliseconds(100);

        [Test]
        public static void ThrowsArgumentNull()
        {
            var nullBotName = new Action(() => new LexLanguageUnderstandingService(null, string.Empty, string.Empty, default(ILexClient)));
            var nullBotAlias = new Action(() => new LexLanguageUnderstandingService(string.Empty, null, string.Empty, default(ILexClient)));
            var nullTemplatesDirectory = new Action(() => new LexLanguageUnderstandingService(string.Empty, string.Empty, null, default(ILexClient)));
            var nullLexClient = new Action(() => new LexLanguageUnderstandingService(string.Empty, string.Empty, string.Empty, default(ILexClient)));
            nullBotName.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("botName");
            nullBotAlias.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("botAlias");
            nullTemplatesDirectory.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("templatesDirectory");
            nullLexClient.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("lexClient");

            using (var service = new LexLanguageUnderstandingService(string.Empty, string.Empty, string.Empty, new MockLexClient()))
            {
                var nullUtterances = new Func<Task>(() => service.TrainAsync(null, Array.Empty<EntityType>()));
                var nullUtteranceItem = new Func<Task>(() => service.TrainAsync(new LabeledUtterance[] { null }, Array.Empty<EntityType>()));
                var nullEntityTypes = new Func<Task>(() => service.TrainAsync(Array.Empty<LabeledUtterance>(), null));
                var nullEntityTypeItem = new Func<Task>(() => service.TrainAsync(Array.Empty<LabeledUtterance>(), new EntityType[] { null }));
                var nullSpeechFile = new Func<Task>(() => service.TestSpeechAsync(null, Array.Empty<EntityType>()));
                var nullTestSpeechEntityTypes = new Func<Task>(() => service.TestSpeechAsync(string.Empty, null));
                var nullTestUtterance = new Func<Task>(() => service.TestAsync(null, Array.Empty<EntityType>()));
                var nullTestEntityTypes = new Func<Task>(() => service.TestAsync(string.Empty, null));
                nullUtterances.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("utterances");
                nullUtteranceItem.Should().Throw<ArgumentException>().And.ParamName.Should().Be("utterances");
                nullEntityTypes.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("entityTypes");
                nullEntityTypeItem.Should().Throw<ArgumentException>().And.ParamName.Should().Be("entityTypes");
                nullSpeechFile.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("speechFile");
                nullTestSpeechEntityTypes.Should().Throw<ArgumentException>().And.ParamName.Should().Be("entityTypes");
                nullTestUtterance.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("utterance");
                nullTestEntityTypes.Should().Throw<ArgumentException>().And.ParamName.Should().Be("entityTypes");
            }
        }

        [Test]
        public static void MissingEntityMatchInTextThrowsInvalidOperation()
        {
            var text = "foo";
            var match = "bar";
            using (var service = new LexLanguageUnderstandingService(string.Empty, string.Empty, TemplatesDirectory, new MockLexClient()))
            {
                var entity = new Entity(string.Empty, string.Empty, match, 0);
                var utterance = new LabeledUtterance(text, string.Empty, new[] { entity });
                var invalidEntityMatch = new Func<Task>(() => service.TrainAsync(new[] { utterance }, Array.Empty<EntityType>()));
                invalidEntityMatch.Should().Throw<InvalidOperationException>();
            }
        }

        [Test]
        public static void MissingEntityTypeThrowsInvalidOperation()
        {
            var text = "hello world";
            var intent = Guid.NewGuid().ToString();
            var entityTypeName = "Planet";
            var mockClient = new MockLexClient();
            using (var lex = new LexLanguageUnderstandingService(string.Empty, string.Empty, TemplatesDirectory, mockClient))
            {
                var entity = new Entity(entityTypeName, "Earth", "world", 0);
                var utterance = new LabeledUtterance(text, intent, new[] { entity });

                var missingEntityType = new Func<Task>(() => lex.TrainAsync(new[] { utterance }, Array.Empty<EntityType>()));
                missingEntityType.Should().Throw<InvalidOperationException>();
            }
        }

        [Test]
        public static async Task CreatesBot()
        {
            var text = "hello world";
            var intent = Guid.NewGuid().ToString();
            var entityTypeName = "Planet";
            var botName = Guid.NewGuid().ToString();
            var mockClient = new MockLexClient();
            using (var lex = new LexLanguageUnderstandingService(botName, string.Empty, TemplatesDirectory, mockClient))
            {
                var entity = new Entity(entityTypeName, "Earth", "world", 0);
                var utterance = new LabeledUtterance(text, intent, new[] { entity });
                var entityType = new EntityType(entityTypeName, "builtin", new JObject { { "slotType", entityTypeName } });

                await lex.TrainAsync(new[] { utterance }, new[] { entityType }).ConfigureAwait(false);

                // get StartImport request
                var startImportRequest = mockClient.Requests.OfType<StartImportRequest>().FirstOrDefault();
                startImportRequest.Should().NotBeNull();

                // get payload
                var payloadJson = GetPayloadJson(startImportRequest.Payload);
                payloadJson.Should().NotBeNull().And.NotBeEmpty();
                var payload = JObject.Parse(payloadJson);

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
            var mockClient = new MockLexClient();
            using (var lex = new LexLanguageUnderstandingService(string.Empty, string.Empty, TemplatesDirectory, mockClient))
            {
                var entity = new Entity(entityTypeName, string.Empty, entityMatch, matchIndex);
                var utterance = new LabeledUtterance(text, intent, new[] { entity });
                var entityType = new EntityType(entityTypeName, "builtin", new JObject { { "slotType", entityTypeName } });

                await lex.TrainAsync(new[] { utterance }, new[] { entityType }).ConfigureAwait(false);

                var startImportRequest = mockClient.Requests.OfType<StartImportRequest>().FirstOrDefault();
                var payloadJson = GetPayloadJson(startImportRequest.Payload);
                var payload = JObject.Parse(payloadJson);

                // assert template utterance is set
                payload.SelectToken(".resource.intents[0].sampleUtterances").Count().Should().Be(1);
                payload.SelectToken(".resource.intents[0].sampleUtterances[0]").Value<string>().Should().Be(sampleUtterance);
            }
        }

        [Test]
        public static async Task WaitsForImportCompletion()
        {
            var importId = Guid.NewGuid().ToString();

            var mockClient = new MockLexClient();
            mockClient.Get<StartImportResponse>().ImportId = importId;
            mockClient.Get<StartImportResponse>().ImportStatus = ImportStatus.IN_PROGRESS;
            mockClient.Get<GetImportResponse>().ImportId = importId;
            mockClient.Get<GetImportResponse>().ImportStatus = ImportStatus.IN_PROGRESS;

            // Wait for the second GetImport action to set status to complete
            var count = 0;
            void onRequest(object request)
            {
                if (request is GetImportRequest && ++count == 2)
                {
                    mockClient.Get<GetImportResponse>().ImportStatus = ImportStatus.COMPLETE;
                }
            }

            mockClient.OnRequest = onRequest;

            using (var lex = new LexLanguageUnderstandingService(string.Empty, string.Empty, TemplatesDirectory, mockClient))
            {
                var utterance = new LabeledUtterance(string.Empty, string.Empty, null);

                await lex.TrainAsync(new[] { utterance }, Array.Empty<EntityType>()).ConfigureAwait(false);

                // Assert two GetImport actions occur
                mockClient.Requests.OfType<GetImportRequest>().Count().Should().Be(2);

                // Assert that the time difference is at least two seconds
                var requests = mockClient.TimestampedRequests
                    .Where(tuple => tuple.Item1 is GetImportRequest)
                    .Select(tuple => new
                    {
                        Request = (GetImportRequest)tuple.Item1,
                        Timestamp = tuple.Item2
                    })
                   .ToArray();

                var difference = requests[1].Timestamp - requests[0].Timestamp;
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

            var mockClient = new MockLexClient();
            mockClient.Get<StartImportResponse>().ImportId = importId;
            mockClient.Get<StartImportResponse>().ImportStatus = ImportStatus.FAILED;
            mockClient.Get<GetImportResponse>().ImportId = importId;
            mockClient.Get<GetImportResponse>().ImportStatus = ImportStatus.FAILED;
            using (var lex = new LexLanguageUnderstandingService(string.Empty, string.Empty, TemplatesDirectory, mockClient))
            {
                var utterance = new LabeledUtterance(string.Empty, string.Empty, null);

                // Null failure reason should be okay
                var importFails = new Func<Task>(() => lex.TrainAsync(new[] { utterance }, Array.Empty<EntityType>()));
                importFails.Should().Throw<InvalidOperationException>().And.Message.Should().BeEmpty();

                // Failure reason is concatenated in message
                mockClient.Get<GetImportResponse>().FailureReason = failureReason;
                var expectedMessage = string.Join(Environment.NewLine, failureReason);
                importFails.Should().Throw<InvalidOperationException>().And.Message.Should().Be(expectedMessage);
            }
        }

        [Test]
        public static void DisposesLexClient()
        {
            var handle = new ManualResetEvent(false);
            var mockClient = new MockLexClient
            {
                OnDispose = () => handle.Set(),
            };

            var service = new LexLanguageUnderstandingService(string.Empty, string.Empty, string.Empty, mockClient);
            service.Dispose();

            handle.WaitOne(5000).Should().BeTrue();
        }

        [Test]
        public static async Task WaitsForBuildCompletion()
        {
            var mockClient = new MockLexClient();
            mockClient.Get<GetBotResponse>().Status = Status.BUILDING;

            // Wait for the third GetBot action to set status to complete
            var count = 0;
            void onRequest(object request)
            {
                if (request is GetBotRequest && ++count == 3)
                {
                    mockClient.Get<GetBotResponse>().Status = Status.READY;
                }
            }

            mockClient.OnRequest = onRequest;

            using (var lex = new LexLanguageUnderstandingService(string.Empty, string.Empty, TemplatesDirectory, mockClient))
            {
                var utterance = new LabeledUtterance(string.Empty, string.Empty, null);
                await lex.TrainAsync(new[] { utterance }, Array.Empty<EntityType>()).ConfigureAwait(false);

                // Assert three GetBot actions occur
                mockClient.Requests.OfType<GetBotRequest>().Count().Should().Be(3);

                // Assert that the time difference is at least two seconds
                var requests = mockClient.TimestampedRequests
                    .Where(tuple => tuple.Item1 is GetBotRequest)
                    .Select(tuple => new
                    {
                        Request = (GetBotRequest)tuple.Item1,
                        Timestamp = tuple.Item2
                    })
                   .ToArray();

                var difference = requests[2].Timestamp - requests[1].Timestamp;
                difference.Should().BeGreaterThan(TimeSpan.FromSeconds(2) - Epsilon);
            }
        }

        [Test]
        public static void BuildFailureThrowsInvalidOperation()
        {
            var mockClient = new MockLexClient();
            mockClient.Get<GetBotResponse>().Status = Status.BUILDING;

            // Wait for the second GetBot action to set status to failed
            var count = 0;
            void onRequest(object request)
            {
                if (request is GetBotRequest && ++count == 2)
                {
                    mockClient.Get<GetBotResponse>().Status = Status.FAILED;
                }
            }

            mockClient.OnRequest = onRequest;

            using (var lex = new LexLanguageUnderstandingService(string.Empty, string.Empty, TemplatesDirectory, mockClient))
            {
                var utterance = new LabeledUtterance(string.Empty, string.Empty, null);
                var buildFailed = new Func<Task>(() => lex.TrainAsync(new[] { utterance }, Array.Empty<EntityType>()));
                buildFailed.Should().Throw<InvalidOperationException>();

                mockClient.Get<GetBotResponse>().Status = new Status("UNKNOWN");
                buildFailed.Should().Throw<InvalidOperationException>();

                mockClient.Get<GetBotResponse>().Status = Status.NOT_BUILT;
                buildFailed.Should().NotThrow<InvalidOperationException>();
            }
        }

        [Test]
        public static async Task CleanupCallsLexActions()
        {
            var botName = Guid.NewGuid().ToString();
            var botAlias = Guid.NewGuid().ToString();
            var mockClient = new MockLexClient();
            using (var lex = new LexLanguageUnderstandingService(botName, botAlias, TemplatesDirectory, mockClient))
            {
                await lex.CleanupAsync().ConfigureAwait(false);
                mockClient.Requests.OfType<DeleteBotAliasRequest>().Count().Should().Be(1);
                mockClient.Requests.OfType<DeleteBotAliasRequest>().First().Name.Should().Be(botAlias);
                mockClient.Requests.OfType<DeleteBotRequest>().Count().Should().Be(1);
                mockClient.Requests.OfType<DeleteBotRequest>().First().Name.Should().Be(botName);
            }
        }

        [Test]
        public static async Task CleanupSucceedsWithNotFound()
        {
            var botName = Guid.NewGuid().ToString();
            var botAlias = Guid.NewGuid().ToString();
            var mockClient = new MockLexClient();
            mockClient.OnRequest = request =>
            {
                if (request is DeleteBotAliasRequest || request is DeleteBotRequest)
                {
                    throw new Amazon.LexModelBuildingService.Model.NotFoundException(string.Empty);
                }
            };

            using (var lex = new LexLanguageUnderstandingService(botName, botAlias, TemplatesDirectory, mockClient))
            {
                await lex.CleanupAsync().ConfigureAwait(false);
                mockClient.Requests.OfType<DeleteBotAliasRequest>().Count().Should().Be(1);
                mockClient.Requests.OfType<DeleteBotAliasRequest>().First().Name.Should().Be(botAlias);
                mockClient.Requests.OfType<DeleteBotRequest>().Count().Should().Be(1);
                mockClient.Requests.OfType<DeleteBotRequest>().First().Name.Should().Be(botName);
            }
        }

        [Test]
        public static async Task AddsListSlotTypesToImportJson()
        {
            var mockClient = new MockLexClient();
            using (var lex = new LexLanguageUnderstandingService(string.Empty, string.Empty, TemplatesDirectory, mockClient))
            {
                var originalValueListEntityTypeName = Guid.NewGuid().ToString();
                var topResolutionListEntityTypeName = Guid.NewGuid().ToString();
                var originalValueListEntityTypeCanonicalForm = Guid.NewGuid().ToString();
                var topResolutionListEntityTypeCanonicalForm = Guid.NewGuid().ToString();
                var topResolutionListEntityTypeSynonym = Guid.NewGuid().ToString();
                var data = @"{""enumerationValues"":[{""value"":""" + originalValueListEntityTypeCanonicalForm + @""",""synonyms"":[]}]}";
                var originalValueListEntityType = new EntityType(
                    originalValueListEntityTypeName,
                    "list",
                    JObject.Parse(data));

                var dataTopResolution = @"{""enumerationValues"":[{""value"":""" + topResolutionListEntityTypeCanonicalForm + @""",""synonyms"":[""" + topResolutionListEntityTypeSynonym + @"""]}]}";
                var topResolutionListEntityType = new EntityType(
                    topResolutionListEntityTypeName,
                    "list",
                    JObject.Parse(dataTopResolution));

                var entityTypes = new[] { originalValueListEntityType, topResolutionListEntityType };
                var utterance = new LabeledUtterance(string.Empty, string.Empty, null);
                await lex.TrainAsync(new[] { utterance }, entityTypes).ConfigureAwait(false);

                var startImportRequest = mockClient.Requests.OfType<StartImportRequest>().FirstOrDefault();
                var payloadJson = GetPayloadJson(startImportRequest.Payload);
                var payload = JObject.Parse(payloadJson);

                // assert slot types are created
                payload.SelectToken(".resource.slotTypes").Count().Should().Be(2);
                payload.SelectToken(".resource.slotTypes[0].name").Value<string>().Should().Be(originalValueListEntityTypeName);
                payload.SelectToken(".resource.slotTypes[0].valueSelectionStrategy").Value<string>().Should().Be(SlotValueSelectionStrategy.ORIGINAL_VALUE);
                payload.SelectToken(".resource.slotTypes[0].enumerationValues").Count().Should().Be(1);
                payload.SelectToken(".resource.slotTypes[0].enumerationValues[0].value").Value<string>().Should().Be(originalValueListEntityTypeCanonicalForm);
                payload.SelectToken(".resource.slotTypes[0].enumerationValues[0].synonyms").Count().Should().Be(0);

                payload.SelectToken(".resource.slotTypes[1].name").Value<string>().Should().Be(topResolutionListEntityTypeName);
                payload.SelectToken(".resource.slotTypes[1].valueSelectionStrategy").Value<string>().Should().Be(SlotValueSelectionStrategy.TOP_RESOLUTION);
                payload.SelectToken(".resource.slotTypes[1].enumerationValues").Count().Should().Be(1);
                payload.SelectToken(".resource.slotTypes[1].enumerationValues[0].value").Value<string>().Should().Be(topResolutionListEntityTypeCanonicalForm);
                payload.SelectToken(".resource.slotTypes[1].enumerationValues[0].synonyms").Count().Should().Be(1);
                payload.SelectToken(".resource.slotTypes[1].enumerationValues[0].synonyms[0]").Value<string>().Should().Be(topResolutionListEntityTypeSynonym);
            }
        }

        [Test]
        public static async Task TestsWithSpeech()
        {
            var intent = Guid.NewGuid().ToString();
            var transcript = Guid.NewGuid().ToString();
            var entityType = Guid.NewGuid().ToString();
            var entityValue = Guid.NewGuid().ToString();
            var mockClient = new MockLexClient();
            mockClient.Get<PostContentResponse>().IntentName = intent;
            mockClient.Get<PostContentResponse>().InputTranscript = transcript;
            using (var lex = new LexLanguageUnderstandingService(string.Empty, string.Empty, TemplatesDirectory, mockClient))
            {
                // slots response will be null in this first request
                // using a text file because we don't need to work with real audio
                var result = await lex.TestSpeechAsync(Path.Combine("assets", "sample.txt"), Array.Empty<EntityType>()).ConfigureAwait(false);

                // assert reads content from file (file contents are "hello world")
                var request = mockClient.Requests.OfType<PostContentRequest>().Single();
                using (var reader = new StreamReader(request.InputStream))
                {
                    reader.ReadToEnd().Should().Be("hello world");
                }

                // assert results
                result.Intent.Should().Be(intent);
                result.Text.Should().Be(transcript);
                result.Entities.Should().BeNull();

                // test with valid slots response
                mockClient.Get<PostContentResponse>().Slots = $"{{\"{entityType}\":\"{entityValue}\"}}";
                result = await lex.TestSpeechAsync(Path.Combine("assets", "sample.txt"), Array.Empty<EntityType>()).ConfigureAwait(false);

                result.Entities.Count.Should().Be(1);
                result.Entities[0].EntityType.Should().Be(entityType);
                result.Entities[0].EntityValue.Should().Be(entityValue);
            }
        }

        [Test]
        public static async Task CreatesLabeledUtterances()
        {
            var text = Guid.NewGuid().ToString();
            var intent = Guid.NewGuid().ToString();
            var entityType = Guid.NewGuid().ToString();
            var entityValue = Guid.NewGuid().ToString();
            var slots = new Dictionary<string, string>
            {
                { entityType, entityValue },
            };

            var mockClient = new MockLexClient();
            mockClient.Get<PostTextResponse>().IntentName = intent;
            using (var lex = new LexLanguageUnderstandingService(string.Empty, string.Empty, TemplatesDirectory, mockClient))
            {
                var response = await lex.TestAsync(text, Array.Empty<EntityType>()).ConfigureAwait(false);
                response.Text.Should().Be(text);
                response.Intent.Should().Be(intent);
                response.Entities.Should().BeEmpty();

                mockClient.Get<PostTextResponse>().Slots = slots;
                response = await lex.TestAsync(text, Array.Empty<EntityType>()).ConfigureAwait(false);
                response.Entities[0].EntityType.Should().Be(entityType);
                response.Entities[0].EntityValue.Should().Be(entityValue);
            }
        }

        [Test]
        public static async Task DoesNotCreateIfBotExists()
        {
            var botName = Guid.NewGuid().ToString();
            var mockClient = new MockLexClient();
            mockClient.Set(new GetBotsResponse
            {
                Bots = new List<BotMetadata>
                {
                    new BotMetadata
                    {
                        Name = botName,
                    },
                },
            });

            using (var lex = new LexLanguageUnderstandingService(botName, string.Empty, TemplatesDirectory, mockClient))
            {
                await lex.TrainAsync(Array.Empty<LabeledUtterance>(), Array.Empty<EntityType>()).ConfigureAwait(false);

                // There are at most two put bot requests, the first to create the bot, the second to build
                // If the bot exists, only the second put bot to build should occur.
                mockClient.Requests.OfType<PutBotRequest>().Count().Should().Be(1);
                mockClient.Requests.OfType<PutBotRequest>().First().ProcessBehavior.Should().Be(ProcessBehavior.BUILD);
            }
        }

        [Test]
        public static async Task DoesNotPublishIfAliasExists()
        {
            var botAlias = Guid.NewGuid().ToString();
            var mockClient = new MockLexClient();
            mockClient.Set(new GetBotAliasesResponse
            {
                BotAliases = new List<BotAliasMetadata>
                {
                    new BotAliasMetadata
                    {
                        Name = botAlias,
                    },
                },
            });

            using (var lex = new LexLanguageUnderstandingService(string.Empty, botAlias, TemplatesDirectory, mockClient))
            {
                await lex.TrainAsync(Array.Empty<LabeledUtterance>(), Array.Empty<EntityType>()).ConfigureAwait(false);

                // If the bot alias exists, the 'PutBotAlias' request should not occur
                mockClient.Requests.OfType<PutBotAliasRequest>().Count().Should().Be(0);
            }
        }

        private static string GetPayloadJson(Stream payloadStream)
        {
            using (var zipArchive = new ZipArchive(payloadStream, ZipArchiveMode.Read, true))
            using (var streamReader = new StreamReader(zipArchive.Entries.Single().Open()))
            {
                return streamReader.ReadToEnd();
            }
        }

        private class MockLexClient : ILexClient
        {
            public MockLexClient()
            {
                this.Set(new GetBotResponse
                {
                    AbortStatement = new Statement { Messages = { new Message() } },
                    ClarificationPrompt = new Prompt { Messages = { new Message() } },
                    Status = Status.READY,
                });

                this.Set(new StartImportResponse
                {
                    ImportStatus = ImportStatus.COMPLETE,
                });
            }

            public Action OnDispose { get; set; }

            public Action<object> OnRequest { get; set; }

            public Func<object, Task> OnRequestAsync { get; set; }

            public IEnumerable<object> Requests => this.RequestsInternal.Select(tuple => tuple.Item1);

            public IEnumerable<Tuple<object, DateTimeOffset>> TimestampedRequests => this.RequestsInternal;

            private List<Tuple<object, DateTimeOffset>> RequestsInternal { get; } = new List<Tuple<object, DateTimeOffset>>();

            private IDictionary<Type, object> Responses { get; } = new Dictionary<Type, object>();

            public void Set<T>(T instance)
            {
                this.Responses[typeof(T)] = instance;
            }

            public T Get<T>()
                where T : new()
            {
                if (!this.Responses.TryGetValue(typeof(T), out var result))
                {
                    result = new T();
                    this.Responses.Add(typeof(T), result);
                }

                return (T)result;
            }

            public Task DeleteBotAliasAsync(DeleteBotAliasRequest request, CancellationToken cancellationToken)
            {
                return this.ProcessRequestAsync(request);
            }

            public Task DeleteBotAsync(DeleteBotRequest request, CancellationToken cancellationToken)
            {
                return this.ProcessRequestAsync(request);
            }

            public async Task<GetBotAliasesResponse> GetBotAliasesAsync(GetBotAliasesRequest request, CancellationToken cancellationToken)
            {
                await this.ProcessRequestAsync(request).ConfigureAwait(false);
                return this.Get<GetBotAliasesResponse>();
            }

            public async Task<GetBotResponse> GetBotAsync(GetBotRequest request, CancellationToken cancellationToken)
            {
                await this.ProcessRequestAsync(request).ConfigureAwait(false);
                return this.Get<GetBotResponse>();
            }

            public async Task<GetBotsResponse> GetBotsAsync(GetBotsRequest request, CancellationToken cancellationToken)
            {
                await this.ProcessRequestAsync(request).ConfigureAwait(false);
                return this.Get<GetBotsResponse>();
            }

            public async Task<GetImportResponse> GetImportAsync(GetImportRequest request, CancellationToken cancellationToken)
            {
                await this.ProcessRequestAsync(request).ConfigureAwait(false);
                return this.Get<GetImportResponse>();
            }

            public async Task<PostContentResponse> PostContentAsync(PostContentRequest request, CancellationToken cancellationToken)
            {
                var streamCopy = new MemoryStream();
                request.InputStream.CopyTo(streamCopy);
                streamCopy.Position = 0;
                var requestCopy = new PostContentRequest
                {
                    Accept = request.Accept,
                    BotAlias = request.BotAlias,
                    BotName = request.BotName,
                    ContentType = request.ContentType,
                    InputStream = streamCopy,
                    UserId = request.UserId,
                };

                await this.ProcessRequestAsync(requestCopy).ConfigureAwait(false);

                return this.Get<PostContentResponse>();
            }

            public async Task<PostTextResponse> PostTextAsync(PostTextRequest request, CancellationToken cancellationToken)
            {
                await this.ProcessRequestAsync(request).ConfigureAwait(false);
                return this.Get<PostTextResponse>();
            }

            public Task PutBotAliasAsync(PutBotAliasRequest request, CancellationToken cancellationToken)
            {
                return this.ProcessRequestAsync(request);
            }

            public Task PutBotAsync(PutBotRequest request, CancellationToken cancellationToken)
            {
                return this.ProcessRequestAsync(request);
            }

            public async Task<StartImportResponse> StartImportAsync(StartImportRequest request, CancellationToken cancellationToken)
            {
                var streamCopy = new MemoryStream();
                request.Payload.CopyTo(streamCopy);
                streamCopy.Position = 0;
                var requestCopy = new StartImportRequest
                {
                    MergeStrategy = request.MergeStrategy,
                    Payload = streamCopy,
                    ResourceType = request.ResourceType,
                };

                await this.ProcessRequestAsync(requestCopy).ConfigureAwait(false);

                return this.Get<StartImportResponse>();
            }

            public void Dispose()
            {
                // Dispose each copy of the StartImportRequest payload
                this.RequestsInternal
                    .Select(tuple => tuple.Item1)
                    .OfType<StartImportRequest>()
                    .ToList()
                    .ForEach(request => request.Payload.Dispose());

                // Dispose each copy of the PostContentRequest input stream
                this.RequestsInternal
                    .Select(tuple => tuple.Item1)
                    .OfType<PostContentRequest>()
                    .ToList()
                    .ForEach(request => request.InputStream.Dispose());

                this.OnDispose?.Invoke();
            }

            private Task ProcessRequestAsync(object request)
            {
                this.RequestsInternal.Add(Tuple.Create(request, DateTimeOffset.Now));
                this.OnRequest?.Invoke(request);
                return this.OnRequestAsync?.Invoke(request) ?? Task.CompletedTask;
            }
        }
    }
}
