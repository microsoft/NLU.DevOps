// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace NLU.DevOps.CommandLine.Tests.Clean
{
    using System.Collections.Generic;
    using CommandLine.Clean;
    using FluentAssertions;
    using global::CommandLine;
    using NUnit.Framework;

    [TestFixture]
    internal class CleanOptionsTests
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
        public void SettingForCleanOptionsWhenSet()
        {
            this.options.Add("-a");
            var args = this.options.ToArray();
            var parser = Parser.Default.ParseArguments<CleanOptions>(args)
                .WithParsed(o => o.DeleteAppSettings.Should().BeTrue())
                .WithNotParsed(o => Assert.Fail("Could not Parse Options"));
        }

        [Test]
        public void SettingForCleanOptionsWhenNotSet()
        {
            var args = this.options.ToArray();
            var parser = Parser.Default.ParseArguments<CleanOptions>(args)
                .WithParsed(o => o.DeleteAppSettings.Should().BeFalse())
                .WithNotParsed(o => Assert.Fail("Could not Parse Options"));
        }
    }
}
