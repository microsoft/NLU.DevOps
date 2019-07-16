// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace NLU.DevOps.CommandLine.Tests.Train
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using CommandLine.Train;
    using FluentAssertions;
    using global::CommandLine;
    using NUnit.Framework;

    [TestFixture]
    internal sealed class TrainCommandTests : IDisposable
    {
        private ICommand commandUnderTest;
        private List<string> options;

        [SetUp]
        public void SetUp()
        {
            this.options = new List<string>();
            this.options.Add("-s");
            this.options.Add("luis");
        }

        [TearDown]
        public void TearDown()
        {
            this.commandUnderTest?.Dispose();
        }

        [Test]
        public void WhenArgumentsDoNotIncludeUtterancesOrSettings()
        {
            this.WhenParserIsRun(true);
            Action a = () => this.commandUnderTest.Main();
            a.Should().Throw<InvalidOperationException>().WithMessage("Must specify either --utterances or --service-settings when using train.");
        }

        [Test]
        public void WhenArgumentsIncludeUtterancesValueButFileDoesNotExist()
        {
            this.options.Add("-u");
            this.options.Add("./bogusfolder/utterances.json");
            this.WhenParserIsRun(true);
            Action a = () => this.commandUnderTest.Main();
            a.Should().Throw<DirectoryNotFoundException>();
        }

        [Test]
        public void WhenArgumentsIncludeSettingsValue()
        {
            Environment.SetEnvironmentVariable("luisAuthoringKey", null);
            this.options.Add("-e");
            this.options.Add("./testdata/settings.luis.json");
            this.WhenParserIsRun(false);
            Action a = () => this.commandUnderTest.Main();
            a.Should().Throw<ArgumentException>().WithMessage("Must specify either 'authoringKey' or 'endpointKey'.");
        }

        [Test]
        public void WhenArgumentsIncludeUtteranceValue()
        {
            Environment.SetEnvironmentVariable("luisAuthoringKey", "abc");
            this.options.Add("-u");
            this.options.Add("./testdata/utterances.json");
            this.WhenParserIsRun(false);
            Action a = () => this.commandUnderTest.Main();
            a.Should().Throw<ArgumentException>().WithMessage("Must specify either 'authoringRegion' or 'endpointRegion'.");
        }

        [Test]
        public void ExceptionIsThrownWhenStagingValueIsNotABoolean()
        {
            Environment.SetEnvironmentVariable("luisIsStaging", "Truely");
            this.options.Add("-u");
            this.options.Add("./testdata/utterances.json");
            this.WhenParserIsRun(false);
            Action a = () => this.commandUnderTest.Main();
            a.Should().Throw<ArgumentException>().WithMessage("The configuration value 'luisIsStaging' must be a valid boolean.");
            Environment.SetEnvironmentVariable("luisIsStaging", null);
        }

        [Test]
        public void SaveAppsettingsShouldCreateAFile()
        {
            var useMock = true;
            this.options.Add("-u");
            this.options.Add("./testdata/utterances.json");
            this.options.Add("-a");
            this.WhenParserIsRun(useMock);
            this.commandUnderTest.Main();
            File.Exists("appsettings.luis.json").Should().BeTrue();
            File.Delete("appsettings.luis.json");
        }

        public void Dispose()
        {
            this.commandUnderTest?.Dispose();
        }

        private void WhenParserIsRun(bool useMock)
        {
            var args = this.options.ToArray();
            ParserResult<TrainOptions> results = Parser.Default.ParseArguments<TrainOptions>(args);
            var options = (Parsed<TrainOptions>)results;
            if (useMock)
            {
                this.commandUnderTest = new TrainCommandMock(options.Value);
            }
            else
            {
                this.commandUnderTest = new TrainCommand(options.Value);
            }
        }
    }
}
