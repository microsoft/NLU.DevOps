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
        public void SettingForCompareOptionsWhenSet()
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
            optionsList.Add("-m");
            var args = optionsList.ToArray();
            var parser = Parser.Default.ParseArguments<CompareOptions>(args)
                .WithParsed(o =>
                {
                    o.ExpectedUtterancesPath.Should().Be("expectedUtterances");
                    o.ActualUtterancesPath.Should().Be("actualUtterances");
                    o.TestLabel.Should().Be("testLabel");
                    o.OutputFolder.Should().Be("outputFolder");
                    o.Metadata.Should().BeTrue();
                })
                .WithNotParsed(o => Assert.Fail("Could not Parse Options"));
        }

        [Test]
        public void SettingForCompareOptionsWhenNotSet()
        {
            var optionsList = new List<string>();
            optionsList.Add("-e");
            optionsList.Add("expectedUtterances");
            optionsList.Add("-a");
            optionsList.Add("actualUtterances");
            var args = optionsList.ToArray();
            var parser = Parser.Default.ParseArguments<CompareOptions>(args)
                .WithParsed(o =>
                {
                    o.TestLabel.Should().Be(null);
                    o.OutputFolder.Should().Be(null);
                    o.Metadata.Should().BeFalse();
                })
                .WithNotParsed(o => Assert.Fail("Could not Parse Options"));
        }

        [Test]
        public void ExceptionWhenExpectedUtterancesNotSetForCompareOptions()
        {
            var optionsList = new List<string>();
            optionsList.Add("-a");
            optionsList.Add("actualUtterances");
            var args = optionsList.ToArray();
            var parser = Parser.Default.ParseArguments<CompareOptions>(args);
            var error = parser.As<NotParsed<CompareOptions>>().Errors.First();
            error.As<MissingRequiredOptionError>().NameInfo.LongName.Should().Be("expected");
        }

        [Test]
        public void ExceptionWhenActualUtterancesNotSetForCompareOptions()
        {
            var optionsList = new List<string>();
            optionsList.Add("-e");
            optionsList.Add("expectedUtterances");
            var args = optionsList.ToArray();
            var parser = Parser.Default.ParseArguments<CompareOptions>(args);
            parser.Tag.Should().Be(ParserResultType.NotParsed);
            var error = parser.As<NotParsed<CompareOptions>>().Errors.First();
            error.As<MissingRequiredOptionError>().NameInfo.LongName.Should().Be("actual");
        }
    }
}
