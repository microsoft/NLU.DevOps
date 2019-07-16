// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace NLU.DevOps.CommandLine.Tests.Clean
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using CommandLine.Clean;
    using FluentAssertions;
    using global::CommandLine;
    using NUnit.Framework;

    [TestFixture]
    internal sealed class CleanCommandTests : IDisposable
    {
        private ICommand commandUnderTest;
        private List<string> options;

        [SetUp]
        public void SetUp()
        {
            this.options = new List<string>
            {
                "-s",
                "luis"
            };
        }

        [TearDown]
        public void TearDown()
        {
            this.commandUnderTest?.Dispose();
        }

        [Test]
        public void WhenArgumentsDoNotIncludeAuthoringKeyOrEndpointKey()
        {
            this.WhenParserIsRun(false);
            Action a = () => this.commandUnderTest.Main();
            a.Should().Throw<ArgumentException>().WithMessage("Must specify either 'authoringKey' or 'endpointKey'.");
        }

        [Test]
        public void WhenOptionToDeleteAppsettingsFile()
        {
            using (TextWriter tw = new StreamWriter("appsettings.luis.json"))
            {
                tw.WriteLine("{ \"dummy\": \"value\" }");
            }

            File.Exists("appsettings.luis.json").Should().BeTrue();
            this.options.Add("-a");
            this.WhenParserIsRun(true);
            this.commandUnderTest.Main();
            File.Exists("appsettings.luis.json").Should().BeFalse();
        }

        public void Dispose()
        {
            this.commandUnderTest?.Dispose();
        }

        private void WhenParserIsRun(bool useMock)
        {
            var args = this.options.ToArray();
            ParserResult<CleanOptions> results = Parser.Default.ParseArguments<CleanOptions>(args);
            var options = (Parsed<CleanOptions>)results;
            if (useMock)
            {
                this.commandUnderTest = new CleanCommandMock(options.Value);
            }
            else
            {
                this.commandUnderTest = new CleanCommand(options.Value);
            }
        }
    }
}
