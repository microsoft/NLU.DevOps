// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace NLU.DevOps.CommandLine.Tests.Test
{
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Models;
    using Moq;
    using Newtonsoft.Json.Linq;
    using NLU.DevOps.CommandLine.Test;
    using NUnit.Framework;

    [TestFixture]
    internal static class TestCommandTests
    {
        [Test]
        public static void TestOutputTruncatesExistingFile()
        {
            var outputPath = Path.GetRandomFileName();
            File.WriteAllText(outputPath, string.Join(string.Empty, Enumerable.Repeat("!", 1000)));
            var testResult = new LabeledUtterance("foo", "foo", null);
            var options = new TestOptions
            {
                UtterancesPath = "testdata/utterances.json",
                OutputPath = outputPath,
            };

            var testCommand = new TestCommandWithMockResult(testResult, options);
            testCommand.Main();

            var content = File.ReadAllText(outputPath);
            var json = JToken.Parse(content);
            json.As<JArray>().Count.Should().Be(7);
        }

        private class TestCommandWithMockResult : TestCommand
        {
            public TestCommandWithMockResult(LabeledUtterance testResult, TestOptions options)
                : base(options)
            {
                this.TestResult = testResult;
            }

            private LabeledUtterance TestResult { get; }

            protected override INLUTestClient CreateNLUTestClient()
            {
                var mockTestClient = new Mock<INLUTestClient>();
                mockTestClient.Setup(client => client.TestAsync(
                        It.IsAny<JToken>(),
                        It.IsAny<CancellationToken>()))
                    .Returns(() => Task.FromResult(this.TestResult));
                return mockTestClient.Object;
            }
        }
    }
}
