// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace LanguageUnderstanding.Lex.Tests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using System.Text;
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
    internal class LexLanguageUnderstandingServiceTests
    {
        private const string TemplatesDirectory = "Templates";
        private static readonly TimeSpan Epsilon = TimeSpan.FromMilliseconds(100);

        [Test]
        public void ThrowsArgumentNull()
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
                var nullSpeechFiles = new Func<Task>(() => service.TestSpeechAsync(default(IEnumerable<string>)));
                var nullSpeechFileItem = new Func<Task>(() => service.TestSpeechAsync(null));
                var nullTestUtterances = new Func<Task>(() => service.TestAsync(default(IEnumerable<string>)));
                var nullTestUtterancesItem = new Func<Task>(() => service.TestAsync(null));
                nullUtterances.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("utterances");
                nullUtteranceItem.Should().Throw<ArgumentException>().And.ParamName.Should().Be("utterances");
                nullEntityTypes.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("entityTypes");
                nullEntityTypeItem.Should().Throw<ArgumentException>().And.ParamName.Should().Be("entityTypes");
                nullSpeechFiles.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("speechFiles");
                nullSpeechFileItem.Should().Throw<ArgumentException>().And.ParamName.Should().Be("speechFiles");
                nullTestUtterances.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("utterances");
                nullTestUtterancesItem.Should().Throw<ArgumentException>().And.ParamName.Should().Be("utterances");
            }
        }

        [Test]
        public void MissingEntityMatchInTextThrowsInvalidOperation()
        {
            var text = "foo";
            var match = "bar";
            using (var service = new LexLanguageUnderstandingService(string.Empty, string.Empty, TemplatesDirectory, new MockLexClient()))
            {
                var entityType = new BuiltinEntityType(string.Empty, string.Empty);
                var entity = new Entity(string.Empty, string.Empty, match, 0);
                var utterance = new LabeledUtterance(text, string.Empty, new[] { entity });
                var invalidEntityMatch = new Func<Task>(() => service.TrainAsync(new[] { utterance }, new[] { entityType }));
                invalidEntityMatch.Should().Throw<InvalidOperationException>();
            }
        }

        [Test]
        public void MissingEntityTypeThrowsInvalidOperation()
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
        public async Task CreatesBot()
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
                var entityType = new BuiltinEntityType(entityTypeName, entityTypeName);

                await lex.TrainAsync(new[] { utterance }, new[] { entityType });

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
        [TestCase("food foo", "foo", "x", 0, "food {x}")]
        [TestCase("foo'd foo", "foo", "x", 0, "foo'd {x}")]
        [TestCase("foo foo foo", "foo", "x", 2, "foo foo {x}")]
        public async Task ReplacesCorrectTokensInSampleUtterances(
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
                var entityType = new BuiltinEntityType(entityTypeName, entityTypeName);

                await lex.TrainAsync(new[] { utterance }, new[] { entityType });

                var startImportRequest = mockClient.Requests.OfType<StartImportRequest>().FirstOrDefault();
                var payloadJson = GetPayloadJson(startImportRequest.Payload);
                var payload = JObject.Parse(payloadJson);

                // assert template utterance is set
                payload.SelectToken(".resource.intents[0].sampleUtterances").Count().Should().Be(1);
                payload.SelectToken(".resource.intents[0].sampleUtterances[0]").Value<string>().Should().Be(sampleUtterance);
            }
        }

        [Test]
        public async Task WaitsForImportCompletion()
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

                await lex.TrainAsync(new[] { utterance }, Array.Empty<EntityType>());

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
        public void ImportFailureThrowsInvalidOperation()
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
        public void DisposesLexClient()
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
        public async Task WaitsForBuildCompletion()
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
                await lex.TrainAsync(new[] { utterance }, Array.Empty<EntityType>());

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
        public void BuildFailureThrowsInvalidOperation()
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

                count = 0;
                mockClient.Get<GetBotResponse>().Status = Status.NOT_BUILT;
                buildFailed.Should().Throw<InvalidOperationException>();
            }
        }

        [Test]
        public async Task CleanupCallsLexActions()
        {
            var botName = Guid.NewGuid().ToString();
            var botAlias = Guid.NewGuid().ToString();
            var mockClient = new MockLexClient();
            using (var lex = new LexLanguageUnderstandingService(botName, botAlias, TemplatesDirectory, mockClient))
            {
                await lex.CleanupAsync();
                mockClient.Requests.OfType<DeleteBotAliasRequest>().Count().Should().Be(1);
                mockClient.Requests.OfType<DeleteBotAliasRequest>().First().Name.Should().Be(botAlias);
                mockClient.Requests.OfType<DeleteBotRequest>().Count().Should().Be(1);
                mockClient.Requests.OfType<DeleteBotRequest>().First().Name.Should().Be(botName);
            }
        }

        [Test]
        public async Task AddsListSlotTypesToImportJson()
        {
            var mockClient = new MockLexClient();
            using (var lex = new LexLanguageUnderstandingService(string.Empty, string.Empty, TemplatesDirectory, mockClient))
            {
                var originalValueListEntityTypeName = Guid.NewGuid().ToString();
                var topResolutionListEntityTypeName = Guid.NewGuid().ToString();
                var originalValueListEntityTypeCanonicalForm = Guid.NewGuid().ToString();
                var topResolutionListEntityTypeCanonicalForm = Guid.NewGuid().ToString();
                var topResolutionListEntityTypeSynonym = Guid.NewGuid().ToString();

                var originalValueListEntityType = new ListEntityType(
                    originalValueListEntityTypeName,
                    new[] { new SynonymSet(originalValueListEntityTypeCanonicalForm, null) });

                var topResolutionListEntityTypeValues = new[]
                {
                    new SynonymSet(
                        topResolutionListEntityTypeCanonicalForm,
                        new[] { topResolutionListEntityTypeSynonym }),
                };

                var topResolutionListEntityType = new ListEntityType(
                    topResolutionListEntityTypeName,
                    topResolutionListEntityTypeValues);

                var entityTypes = new[] { originalValueListEntityType, topResolutionListEntityType };
                var utterance = new LabeledUtterance(string.Empty, string.Empty, null);
                await lex.TrainAsync(new[] { utterance }, entityTypes);

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
        public async Task TestsWithSpeech()
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
                var results = await lex.TestSpeechAsync(Path.Combine("assets", "sample.txt"));

                // assert reads content from file (file contents are "hello world")
                var request = mockClient.Requests.OfType<PostContentRequest>().Single();
                using (var reader = new StreamReader(request.InputStream))
                {
                    reader.ReadToEnd().Should().Be("hello world");
                }

                // assert results
                results.Count().Should().Be(1);
                results.First().Intent.Should().Be(intent);
                results.First().Text.Should().Be(transcript);
                results.First().Entities.Should().BeNull();

                // test with valid slots response
                mockClient.Get<PostContentResponse>().Slots = $"{{\"{entityType}\":\"{entityValue}\"}}";
                results = await lex.TestSpeechAsync(Path.Combine("assets", "sample.txt"));
                results.Count().Should().Be(1);
                results.First().Entities.Count().Should().Be(1);
                results.First().Entities[0].EntityType.Should().Be(entityType);
                results.First().Entities[0].EntityValue.Should().Be(entityValue);
            }
        }

        [Test]
        public async Task CreatesLabeledUtterances()
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
                var responses = await lex.TestAsync(new[] { text });
                responses.Count().Should().Be(1);
                responses.First().Text.Should().Be(text);
                responses.First().Intent.Should().Be(intent);
                responses.First().Entities.Should().BeEmpty();

                mockClient.Get<PostTextResponse>().Slots = slots;
                responses = await lex.TestAsync(new[] { text });
                responses.First().Entities[0].EntityType.Should().Be(entityType);
                responses.First().Entities[0].EntityValue.Should().Be(entityValue);
            }
        }

        [Test]
        public async Task RunsTestsInParallel()
        {
            var mockClient = new MockLexClient();
            var degreeOfParallelism = 3;

            var requestCount = 0;
            var countdownEvent = new CountdownEvent(degreeOfParallelism);
            var taskCompletionSource = new TaskCompletionSource<bool>();
            Task onRequestAsync(object request)
            {
                if (request is PostTextRequest)
                {
                    countdownEvent.Signal();
                    requestCount++;
                    return taskCompletionSource.Task;
                }

                return Task.CompletedTask;
            }

            mockClient.OnRequestAsync = onRequestAsync;

            using (var lex = new LexLanguageUnderstandingService(string.Empty, string.Empty, string.Empty, mockClient))
            {
                // Send number of utterances greater than the max parallelism
                var utteranceCount = degreeOfParallelism + 1;
                var utterances = Enumerable.Repeat(string.Empty, utteranceCount);
                var task = lex.TestAsync(utterances);

                // Should receive max parallel requests within 5 seconds
                countdownEvent.Wait(5000).Should().BeTrue();

                // At most max parallel requests have been made
                requestCount.Should().Be(degreeOfParallelism);

                // Do not block future requests
                mockClient.OnRequestAsync = null;

                // Release blocked requests
                taskCompletionSource.SetResult(true);

                // Assert results count
                var results = await task;
                results.Count().Should().Be(utteranceCount);
            }
        }

        [Test]
        [Category("LongRunning")]
        public void DeleteBotAliasRetries()
        {
            var mockClient = new MockLexClient();

            // Wait for the second GetBot action to set status to failed
            var shouldThrow = true;
            var throwOnce = true;
            var conflictException = new Amazon.LexModelBuildingService.Model.ConflictException(string.Empty);
            void onRequest(object request)
            {
                if (request is DeleteBotAliasRequest && shouldThrow)
                {
                    shouldThrow = !throwOnce;
                    throw conflictException;
                }
            }

            mockClient.OnRequest = onRequest;
            using (var lex = new LexLanguageUnderstandingService(string.Empty, string.Empty, TemplatesDirectory, mockClient))
            {
                var utterance = new LabeledUtterance(string.Empty, string.Empty, null);
                var buildModel = new Func<Task>(() => lex.CleanupAsync());

                buildModel.Should().NotThrow<Amazon.LexModelBuildingService.Model.ConflictException>();

                // Assert two DeleteBotAliasRequest actions occur
                mockClient.Requests.OfType<DeleteBotAliasRequest>().Count().Should().Be(2);

                // Assert that the time difference is at least five seconds
                var requests = mockClient.TimestampedRequests
                    .Where(tuple => tuple.Item1 is DeleteBotAliasRequest)
                    .Select(tuple => new
                    {
                        Request = (DeleteBotAliasRequest)tuple.Item1,
                        Timestamp = tuple.Item2
                    })
                   .ToArray();
                var difference = requests[1].Timestamp - requests[0].Timestamp;
                difference.Should().BeGreaterThan(TimeSpan.FromSeconds(10) - Epsilon);

                shouldThrow = true;
                throwOnce = false;
                buildModel.Should().Throw<Amazon.LexModelBuildingService.Model.ConflictException>().And.Should().Be(conflictException);
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

            public async Task<GetBotResponse> GetBotAsync(GetBotRequest request, CancellationToken cancellationToken)
            {
                await this.ProcessRequestAsync(request);
                return this.Get<GetBotResponse>();
            }

            public async Task<GetImportResponse> GetImportAsync(GetImportRequest request, CancellationToken cancellationToken)
            {
                await this.ProcessRequestAsync(request);
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

                await this.ProcessRequestAsync(requestCopy);

                return this.Get<PostContentResponse>();
            }

            public async Task<PostTextResponse> PostTextAsync(PostTextRequest request, CancellationToken cancellationToken)
            {
                await this.ProcessRequestAsync(request);
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

                await this.ProcessRequestAsync(requestCopy);

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
