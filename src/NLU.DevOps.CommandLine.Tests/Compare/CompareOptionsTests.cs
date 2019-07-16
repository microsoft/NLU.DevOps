// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace NLU.DevOps.CommandLine.Tests.Compare
{
    using System.Collections.Generic;
    using System.Linq;
    using CommandLine.Compare;
    using FluentAssertions;
    using global::CommandLine;
    using NUnit.Framework;

    [TestFixture]
    internal class CompareOptionsTests
    {
        [Test]
        public void SettingForCleanOptionsWhenSet()
        {
            var optionsList = new List<string>();
            optionsList.Add("-e");
            optionsList.Add("expectedUtterances");
            optionsList.Add("-a");
            optionsList.Add("actualUtterances");
            optionsList.Add("-l");
            optionsList.Add("testLabel");
            optionsList.Add("-o");
            optionsList.Add("outputFolder");
            var args = optionsList.ToArray();
            var parser = Parser.Default.ParseArguments<CompareOptions>(args).WithParsed<CompareOptions>(o =>
            {
                o.ExpectedUtterancesPath.Should().Be("expectedUtterances");
                o.ActualUtterancesPath.Should().Be("actualUtterances");
                o.TestLabel.Should().Be("testLabel");
                o.OutputFolder.Should().Be("outputFolder");
            }).WithNotParsed<CompareOptions>(o => Assert.Fail("Could not Parse Options"));
        }

        [Test]
        public void SettingForCleanOptionsWhenNotSet()
        {
            var optionsList = new List<string>();
            optionsList.Add("-e");
            optionsList.Add("expectedUtterances");
            optionsList.Add("-a");
            optionsList.Add("actualUtterances");
            var args = optionsList.ToArray();
            var parser = Parser.Default.ParseArguments<CompareOptions>(args).WithParsed<CompareOptions>(o =>
            {
                o.TestLabel.Should().Be(null);
                o.OutputFolder.Should().Be(null);
            }).WithNotParsed<CompareOptions>(o => Assert.Fail("Could not Parse Options"));
        }

        [Test]
        public void ExceptionWhenExpectedUtterancesNotSetForCleanOptions()
        {
            var optionsList = new List<string>();
            optionsList.Add("-a");
            optionsList.Add("actualUtterances");
            var args = optionsList.ToArray();
            var parser = Parser.Default.ParseArguments<CompareOptions>(args);
            parser.Tag.Should().Be(ParserResultType.NotParsed);
            var notParsed = (NotParsed<CompareOptions>)parser;
            var error = notParsed.Errors.First();
            error.Should().BeOfType<MissingRequiredOptionError>();
            var missingOptionError = (MissingRequiredOptionError)error;
            missingOptionError.NameInfo.LongName.Should().Be("expected");
        }

        [Test]
        public void ExceptionWhenActualUtterancesNotSetForCleanOptions()
        {
            var optionsList = new List<string>();
            optionsList.Add("-e");
            optionsList.Add("expectedUtterances");
            var args = optionsList.ToArray();
            var parser = Parser.Default.ParseArguments<CompareOptions>(args);
            parser.Tag.Should().Be(ParserResultType.NotParsed);
            var notParsed = (NotParsed<CompareOptions>)parser;
            var error = notParsed.Errors.First();
            error.Should().BeOfType<MissingRequiredOptionError>();
            var missingOptionError = (MissingRequiredOptionError)error;
            missingOptionError.NameInfo.LongName.Should().Be("actual");
        }
    }
}
