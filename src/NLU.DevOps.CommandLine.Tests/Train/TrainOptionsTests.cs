// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace NLU.DevOps.CommandLine.Tests.Train
{
    using System.Collections.Generic;
    using FluentAssertions;
    using global::CommandLine;
    using NLU.DevOps.CommandLine.Train;
    using NUnit.Framework;

    [TestFixture]
    internal class TrainOptionsTests
    {
        private List<string> options;

        [SetUp]
        public void SetUp()
        {
            this.options = new List<string>();
            this.options.Add("-s");
            this.options.Add("luis");
        }

        [Test]
        public void SettingForTrainOptionsWhenSet()
        {
            this.options.Add("-u");
            this.options.Add("utterances");
            this.options.Add("-e");
            this.options.Add("settings");
            this.options.Add("-a");
            var args = this.options.ToArray();
            var parser = Parser.Default.ParseArguments<TrainOptions>(args).WithParsed<TrainOptions>(o =>
            {
                o.UtterancesPath.Should().Be("utterances");
                o.SettingsPath.Should().Be("settings");
                o.SaveAppSettings.Should().BeTrue();
            }).WithNotParsed<TrainOptions>(o => Assert.Fail("Could not Parse Options"));
        }

        [Test]
        public void SettingForTrainOptionsWhenNotSet()
        {
            var args = this.options.ToArray();
            var parser = Parser.Default.ParseArguments<TrainOptions>(args).WithParsed<TrainOptions>(o =>
            {
                o.UtterancesPath.Should().Be(null);
                o.SettingsPath.Should().Be(null);
                o.SaveAppSettings.Should().BeFalse();
            }).WithNotParsed<TrainOptions>(o => Assert.Fail("Could not Parse Options"));
        }
    }
}
