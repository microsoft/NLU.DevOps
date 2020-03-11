// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace NLU.DevOps.ModelPerformance.Tests
{
    using System;
    using System.IO;
    using FluentAssertions;
    using Microsoft.Extensions.Configuration;
    using Newtonsoft.Json.Linq;
    using NUnit.Framework;

    [TestFixture]
    internal static class TestSettingsTests
    {
        [Test]
        public static void ThrowsArgumentNull()
        {
            Func<bool> nullConfiguration = () => new TestSettings(default(IConfiguration), false).UnitTestMode;
            nullConfiguration.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("configuration");
        }

        [Test]
        public static void CreateFromJson()
        {
            var settingsFile = Guid.NewGuid().ToString();

            var threshold = new JObject
            {
                { "type", "intent" },
                { "group", "*" },
                { "threshold", 0.05 },
            };

            var settings = new JObject
            {
                { "trueNegativeIntent", "default" },
                { "strictEntities", new JArray { "strict" } },
                { "ignoreEntities", new JArray { "ignore" } },
                { "thresholds", new JArray { threshold } },
            };

            File.WriteAllText(settingsFile, settings.ToString());

            try
            {
                var testSettings = new TestSettings(settingsFile, false);
                testSettings.TrueNegativeIntent.Should().Be("default");
                testSettings.StrictEntities.Should().BeEquivalentTo("strict");
                testSettings.IgnoreEntities.Should().BeEquivalentTo("ignore");
                testSettings.Thresholds.Count.Should().Be(1);
                testSettings.Thresholds[0].Type.Should().Be("intent");
                testSettings.Thresholds[0].Group.Should().Be("*");
                testSettings.Thresholds[0].Threshold.Should().BeApproximately(0.05, 0.001);
            }
            finally
            {
                File.Delete(settingsFile);
            }
        }

        [Test]
        public static void CreateFromYaml()
        {
            var settingsFile = $"{Guid.NewGuid()}.yml";
            var settings = new[]
            {
                "trueNegativeIntent: default",
                "strictEntities:",
                "- strict",
                "ignoreEntities:",
                "- ignore",
                "thresholds:",
                "- type: intent",
                "  group: '*'",
                "  threshold: 0.05",
            };

            File.WriteAllText(settingsFile, string.Join(Environment.NewLine, settings));

            try
            {
                var testSettings = new TestSettings(settingsFile, false);
                testSettings.TrueNegativeIntent.Should().Be("default");
                testSettings.StrictEntities.Should().BeEquivalentTo("strict");
                testSettings.IgnoreEntities.Should().BeEquivalentTo("ignore");
                testSettings.Thresholds.Count.Should().Be(1);
                testSettings.Thresholds[0].Type.Should().Be("intent");
                testSettings.Thresholds[0].Group.Should().Be("*");
                testSettings.Thresholds[0].Threshold.Should().BeApproximately(0.05, 0.001);
            }
            finally
            {
                File.Delete(settingsFile);
            }
        }
    }
}
