// Copyright (c) Microsoft Corporation.
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
        public void WhenOptionToDeleteAppsettingsFile()
        {
            using (TextWriter tw = new StreamWriter("appsettings.luis.json"))
            {
                tw.WriteLine("{ \"dummy\": \"value\" }");
            }

            File.Exists("appsettings.luis.json").Should().BeTrue();
            this.options.Add("-a");
            this.WhenParserIsRun();
            this.commandUnderTest.Main();
            File.Exists("appsettings.luis.json").Should().BeFalse();
        }

        public void Dispose()
        {
            this.commandUnderTest?.Dispose();
        }

        private void WhenParserIsRun()
        {
            var args = this.options.ToArray();
            var results = Parser.Default.ParseArguments<CleanOptions>(args);
            var cleanOptions = (Parsed<CleanOptions>)results;
            this.commandUnderTest = new CleanCommandMock(cleanOptions.Value);
        }
    }
}
