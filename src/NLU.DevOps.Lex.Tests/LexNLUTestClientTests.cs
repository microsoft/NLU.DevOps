// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace NLU.DevOps.Lex.Tests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Amazon.Lex.Model;
    using FluentAssertions;
    using Models;
    using NUnit.Framework;

    [TestFixture]
    internal static class LexNLUTestClientTests
    {
        [Test]
        public static void ThrowsArgumentNull()
        {
            var nullBotName = new Action(() => new LexNLUTestClient(null, string.Empty, null, default(ILexTestClient)));
            var nullBotAlias = new Action(() => new LexNLUTestClient(string.Empty, null, null, default(ILexTestClient)));
            var nullLexSettings = new Action(() => new LexNLUTestClient(string.Empty, string.Empty, null, default(ILexTestClient)));
            var nullLexClient = new Action(() => new LexNLUTestClient(string.Empty, string.Empty, new LexSettings(), default(ILexTestClient)));
            nullBotName.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("botName");
            nullBotAlias.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("botAlias");
            nullLexSettings.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("lexSettings");
            nullLexClient.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("lexClient");

            using (var lex = new LexNLUTestClient(string.Empty, string.Empty, new LexSettings(), new MockLexTestClient()))
            {
                var nullSpeechFile = new Func<Task>(() => lex.TestSpeechAsync(null));
                var nullTestUtterance = new Func<Task>(() => lex.TestAsync(default(INLUQuery)));
                nullSpeechFile.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("speechFile");
                nullTestUtterance.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("query");
            }
        }

        [Test]
        public static void DisposesLexClient()
        {
            var handle = new ManualResetEvent(false);
            var mockClient = new MockLexTestClient
            {
                OnDispose = () => handle.Set(),
            };

            var lex = new LexNLUTestClient(string.Empty, string.Empty, new LexSettings(), mockClient);
            lex.Dispose();

            handle.WaitOne(5000).Should().BeTrue();
        }

        [Test]
        public static async Task TestsWithSpeech()
        {
            var intent = Guid.NewGuid().ToString();
            var transcript = Guid.NewGuid().ToString();
            var entityType = Guid.NewGuid().ToString();
            var entityValue = Guid.NewGuid().ToString();
            var mockClient = new MockLexTestClient();
            mockClient.Get<PostContentResponse>().IntentName = intent;
            mockClient.Get<PostContentResponse>().InputTranscript = transcript;
            using (var lex = new LexNLUTestClient(string.Empty, string.Empty, new LexSettings(), mockClient))
            {
                // slots response will be null in this first request
                // using a text file because we don't need to work with real audio
                var result = await lex.TestSpeechAsync(Path.Combine("assets", "sample.txt")).ConfigureAwait(false);

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
                result = await lex.TestSpeechAsync(Path.Combine("assets", "sample.txt")).ConfigureAwait(false);

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

            var mockClient = new MockLexTestClient();
            mockClient.Get<PostTextResponse>().IntentName = intent;
            using (var lex = new LexNLUTestClient(string.Empty, string.Empty, new LexSettings(), mockClient))
            {
                var response = await lex.TestAsync(text).ConfigureAwait(false);
                response.Text.Should().Be(text);
                response.Intent.Should().Be(intent);
                response.Entities.Should().BeEmpty();

                mockClient.Get<PostTextResponse>().Slots = slots;
                response = await lex.TestAsync(text).ConfigureAwait(false);
                response.Entities[0].EntityType.Should().Be(entityType);
                response.Entities[0].EntityValue.Should().Be(entityValue);
            }
        }

        private class MockLexTestClient : ILexTestClient
        {
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

            public void Dispose()
            {
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
