// Copyright (c) Microsoft Corporation.
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
            optionsList.Add("-t");
            optionsList.Add("testSettings");
            optionsList.Add("-o");
            optionsList.Add("outputFolder");
            optionsList.Add("-b");
            optionsList.Add("baseline");
            optionsList.Add("-u");
            var args = optionsList.ToArray();
            var parser = Parser.Default.ParseArguments<CompareOptions>(args)
                .WithParsed(o =>
                {
                    o.ExpectedUtterancesPath.Should().Be("expectedUtterances");
                    o.ActualUtterancesPath.Should().Be("actualUtterances");
                    o.TestSettingsPath.Should().Be("testSettings");
                    o.OutputFolder.Should().Be("outputFolder");
                    o.UnitTestMode.Should().BeTrue();
                    o.BaselinePath.Should().Be("baseline");
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
                    o.OutputFolder.Should().BeNull();
                    o.TestSettingsPath.Should().BeNull();
                    o.UnitTestMode.Should().BeFalse();
                    o.BaselinePath.Should().BeNull();
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
