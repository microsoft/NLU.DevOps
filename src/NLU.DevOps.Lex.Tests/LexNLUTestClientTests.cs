// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace NLU.DevOps.Lex.Tests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Amazon.Lex.Model;
    using Core;
    using FluentAssertions;
    using FluentAssertions.Json;
    using Models;
    using Moq;
    using Newtonsoft.Json.Linq;
    using NUnit.Framework;

    [TestFixture]
    internal static class LexNLUTestClientTests
    {
        [Test]
        public static void ThrowsArgumentNull()
        {
            var nullBotName = new Action(() => new LexNLUTestClient(null, string.Empty, default(ILexTestClient)));
            var nullBotAlias = new Action(() => new LexNLUTestClient(string.Empty, null, default(ILexTestClient)));
            var nullLexClient = new Action(() => new LexNLUTestClient(string.Empty, string.Empty, default(ILexTestClient)));
            nullBotName.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("botName");
            nullBotAlias.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("botAlias");
            nullLexClient.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("lexClient");

            using (var lex = new LexNLUTestClient(string.Empty, string.Empty, new Mock<ILexTestClient>().Object))
            {
                var nullSpeechFile = new Func<Task>(() => lex.TestSpeechAsync(null));
                var nullTestUtterance = new Func<Task>(() => lex.TestAsync(default(JToken)));
                nullSpeechFile.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("speechFile");
                nullTestUtterance.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("query");
            }
        }

        [Test]
        public static void DisposesLexClient()
        {
            var handle = new ManualResetEvent(false);
            var mockClient = new Mock<ILexTestClient>();
            mockClient.Setup(client => client.Dispose())
                .Callback(() => handle.Set());

            var lex = new LexNLUTestClient(string.Empty, string.Empty, mockClient.Object);
            lex.Dispose();

            handle.WaitOne(5000).Should().BeTrue();
        }

        [Test]
        [TestCase(null, null, null)]
        [TestCase("{\"foo\":\"bar\"}", "foo", "bar")]
        public static async Task TestsWithSpeech(string slots, string entityType, string entityValue)
        {
            var fileName = "sample.txt";
            var intent = Guid.NewGuid().ToString();
            var transcript = Guid.NewGuid().ToString();

            var content = default(string);
            var mockClient = new Mock<ILexTestClient>();
            mockClient.Setup(lex => lex.PostContentAsync(
                    It.Is<PostContentRequest>(request => ((FileStream)request.InputStream).Name.EndsWith(fileName, StringComparison.InvariantCulture)),
                    It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult(new PostContentResponse
                {
                    IntentName = intent,
                    InputTranscript = transcript,
                    Slots = slots,
                }))
                .Callback<PostContentRequest, CancellationToken>(
                    (request, cancellationToken) => content = GetContent(request.InputStream));

            using (var lex = new LexNLUTestClient(string.Empty, string.Empty, mockClient.Object))
            {
                // slots response will be null in this first request
                // using a text file because we don't need to work with real audio
                var result = await lex.TestSpeechAsync(Path.Combine("assets", "sample.txt")).ConfigureAwait(false);

                // assert reads content from file (file contents are "hello world")
                content.Should().Be("hello world");

                // assert intent and text
                result.Intent.Should().Be(intent);
                result.Text.Should().Be(transcript);

                // assert entities
                if (slots == null)
                {
                    result.Entities.Should().BeNull();
                }
                else
                {
                    result.Entities.Count.Should().Be(1);
                    result.Entities[0].EntityType.Should().Be(entityType);
                    result.Entities[0].EntityValue.Value<string>().Should().BeEquivalentTo(entityValue);
                }
            }
        }

        [Test]
        public static async Task CreatesLabeledUtterances()
        {
            var text = Guid.NewGuid().ToString();
            var intent = Guid.NewGuid().ToString();
            var mockClient = new Mock<ILexTestClient>();
            mockClient.Setup(lex => lex.PostTextAsync(
                    It.Is<PostTextRequest>(request => request.InputText == text),
                    It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult(new PostTextResponse { IntentName = intent }));

            using (var lex = new LexNLUTestClient(string.Empty, string.Empty, mockClient.Object))
            {
                var response = await lex.TestAsync(text).ConfigureAwait(false);
                response.Text.Should().Be(text);
                response.Intent.Should().Be(intent);
                response.Entities.Should().BeEmpty();
            }
        }

        [Test]
        public static async Task CreatesLabeledUtterancesWithEntities()
        {
            var text = Guid.NewGuid().ToString();
            var intent = Guid.NewGuid().ToString();
            var entityType = Guid.NewGuid().ToString();
            var entityValue = Guid.NewGuid().ToString();
            var slots = new Dictionary<string, string>
            {
                { entityType, entityValue },
            };

            var mockClient = new Mock<ILexTestClient>();
            mockClient.Setup(lex => lex.PostTextAsync(
                    It.Is<PostTextRequest>(request => request.InputText == text),
                    It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult(new PostTextResponse { Slots = slots }));

            using (var lex = new LexNLUTestClient(string.Empty, string.Empty, mockClient.Object))
            {
                var response = await lex.TestAsync(text).ConfigureAwait(false);
                response.Entities[0].EntityType.Should().Be(entityType);
                response.Entities[0].EntityValue.Value<string>().Should().BeEquivalentTo(entityValue);
            }
        }

        private static string GetContent(Stream stream)
        {
            using (var streamReader = new StreamReader(stream, Encoding.UTF8, true, 4096, true))
            {
                return streamReader.ReadToEnd();
            }
        }
    }
}
