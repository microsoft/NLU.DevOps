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
            Func<bool> nullConfiguration = () => new TestSettings(null).Strict;
            nullConfiguration.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("configuration");
        }

        [Test]
        public static void CreateFromJson()
        {
            var settingsFile = Guid.NewGuid().ToString();

            var settings = new JObject
            {
                { "trueNegativeIntent", "default" },
                { "strictEntities", new JArray { "strict" } },
                { "ignoreEntities", new JArray { "ignore" } },
            };

            File.WriteAllText(settingsFile, settings.ToString());

            try
            {
                var configuration = new ConfigurationBuilder()
                    .AddJsonFile(settingsFile)
                    .Build();

                var testSettings = new TestSettings(configuration);
                testSettings.TrueNegativeIntent.Should().Be("default");
                testSettings.StrictEntities.Should().BeEquivalentTo("strict");
                testSettings.IgnoreEntities.Should().BeEquivalentTo("ignore");
            }
            finally
            {
                File.Delete(settingsFile);
            }
        }
    }
}
