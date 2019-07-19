// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace NLU.DevOps.Luis.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Reflection;
    using FluentAssertions;
    using Microsoft.Extensions.Configuration;
    using NUnit.Framework;

    [TestFixture]
    internal static class LuisConfigurationTests
    {
        [Test]
        public static void ThrowsArgumentNull()
        {
            Action nullConfiguration = () => new LuisConfiguration(null);
            nullConfiguration.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("configuration");
        }

        [Test]
        [TestCase(nameof(ILuisConfiguration.AppName), "luisAppName")]
        [TestCase(nameof(ILuisConfiguration.AppId), "luisAppId")]
        [TestCase(nameof(ILuisConfiguration.VersionId), "luisVersionId")]
        [TestCase(nameof(ILuisConfiguration.AuthoringKey), "luisAuthoringKey")]
        [TestCase(nameof(ILuisConfiguration.AuthoringRegion), "luisAuthoringRegion")]
        [TestCase(nameof(ILuisConfiguration.EndpointKey), "luisEndpointKey")]
        [TestCase(nameof(ILuisConfiguration.EndpointRegion), "luisEndpointRegion")]
#if LUIS_V3
        [TestCase(nameof(ILuisConfiguration.SlotName), "luisSlotName")]
#endif
        [TestCase(nameof(ILuisConfiguration.SpeechKey), "speechKey")]
        [TestCase(nameof(ILuisConfiguration.AzureSubscriptionId), "azureSubscriptionId")]
        [TestCase(nameof(ILuisConfiguration.AzureResourceGroup), "azureResourceGroup")]
        [TestCase(nameof(ILuisConfiguration.AzureAppName), "azureLuisResourceName")]
        [TestCase(nameof(ILuisConfiguration.ArmToken), "ARM_TOKEN")]
        public static void ReadsStringConfigurationValues(string propertyName, string configurationKey)
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    { configurationKey, Guid.NewGuid().ToString() },
                })
                .Build();

            var luisConfiguration = new LuisConfiguration(configuration);
            var property = typeof(LuisConfiguration).GetProperty(propertyName);
            var propertyValue = property.GetValue(luisConfiguration);
            propertyValue.Should().Be(configuration[configurationKey]);
        }

        [Test]
        [TestCase(nameof(ILuisConfiguration.IsStaging), "luisIsStaging")]
#if LUIS_V2
        [TestCase(nameof(ILuisConfiguration.UseSpeechEndpoint), "luisUseSpeechEndpoint")]
#endif
#if LUIS_V3
        [TestCase(nameof(ILuisConfiguration.DirectVersionPublish), "luisDirectVersionPublish")]
#endif
        public static void ReadsBooleanConfigurationValues(string propertyName, string configurationKey)
        {
            var trueConfiguration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    { configurationKey, true.ToString(CultureInfo.InvariantCulture) },
                })
                .Build();

            var trueLuisConfiguration = new LuisConfiguration(trueConfiguration);
            var property = typeof(LuisConfiguration).GetProperty(propertyName);
            var truePropertyValue = property.GetValue(trueLuisConfiguration);
            truePropertyValue.As<bool>().Should().BeTrue();

            var emptyConfiguration = new ConfigurationBuilder()
                .Build();

            var emptyLuisConfiguration = new LuisConfiguration(emptyConfiguration);
            var emptyPropertyValue = property.GetValue(emptyLuisConfiguration);
            emptyPropertyValue.As<bool>().Should().BeFalse();

            var falseConfiguration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    { configurationKey, false.ToString(CultureInfo.InvariantCulture) },
                })
                .Build();

            var falseLuisConfiguration = new LuisConfiguration(falseConfiguration);
            var falsePropertyValue = property.GetValue(falseLuisConfiguration);
            falsePropertyValue.As<bool>().Should().BeFalse();
        }

        [Test]
        [TestCase(nameof(ILuisConfiguration.AuthoringKey), "luisAuthoringKey")]
        [TestCase(nameof(ILuisConfiguration.AuthoringRegion), "luisAuthoringRegion")]
        [TestCase(nameof(ILuisConfiguration.EndpointKey), "luisEndpointKey", "luisAuthoringKey")]
        [TestCase(nameof(ILuisConfiguration.EndpointRegion), "luisEndpointRegion", "luisAuthoringRegion")]
        [TestCase(nameof(ILuisConfiguration.SpeechRegion), "speechRegion", "luisEndpointRegion")]
        [TestCase(nameof(ILuisConfiguration.SpeechKey), "speechKey")]
        public static void ThrowsInvalidOperationForMissingConfiguration(string propertyName, params string[] configurationKeys)
        {
            var luisConfiguration = new LuisConfiguration(new ConfigurationBuilder().Build());
            var property = typeof(LuisConfiguration).GetProperty(propertyName);
            Action missingConfiguration = () => property.GetValue(luisConfiguration);
            var shouldThrow = missingConfiguration.Should().Throw<TargetInvocationException>();
            shouldThrow.And.InnerException.Should().BeOfType<InvalidOperationException>();

            foreach (var key in configurationKeys)
            {
                shouldThrow.And.InnerException.Message.Should().Contain(key);
            }
        }

        [Test]
        [TestCase(nameof(ILuisConfiguration.AppId))]
        [TestCase(nameof(ILuisConfiguration.AzureSubscriptionId))]
        [TestCase(nameof(ILuisConfiguration.AzureResourceGroup))]
        [TestCase(nameof(ILuisConfiguration.AzureAppName))]
        [TestCase(nameof(ILuisConfiguration.ArmToken))]
        public static void EmptyConfigurationReturnsNullValue(string propertyName)
        {
            var luisConfiguration = new LuisConfiguration(new ConfigurationBuilder().Build());
            var property = typeof(LuisConfiguration).GetProperty(propertyName);
            property.GetValue(luisConfiguration).Should().BeNull();
        }

        [Test]
        public static void GeneratesRandomAppNameWithEmptyConfiguration()
        {
            var configuration = new ConfigurationBuilder()
                .Build();

            var luisConfiguration = new LuisConfiguration(configuration);
            luisConfiguration.AppName.Should().MatchRegex(@"^[A-Z]{8}$");
        }

        [Test]
        public static void GeneratesAppNameWithPrefix()
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    { "luisAppNamePrefix", "foo" },
                })
                .Build();

            var luisConfiguration = new LuisConfiguration(configuration);
            luisConfiguration.AppName.Should().MatchRegex(@"^foo_[A-Z]{8}$");
        }

        [Test]
        public static void GeneratesDefaultVersionId()
        {
            var luisConfiguration = new LuisConfiguration(new ConfigurationBuilder().Build());
            luisConfiguration.VersionId.Should().Be("1.0.1");

            var luisConfigurationWithVersionPrefix = new LuisConfiguration(new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    { "luisVersionPrefix", "2.0" },
                })
                .Build());

            luisConfigurationWithVersionPrefix.VersionId.Should().Be("1.0.1");

            var luisConfigurationWithBuildId = new LuisConfiguration(new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    { "BUILD_ID", 999.ToString(CultureInfo.InvariantCulture) },
                })
                .Build());
            luisConfigurationWithBuildId.VersionId.Should().Be("1.0.1");
        }

        [Test]
        public static void GeneratesVersionWithPrefixAndBuildId()
        {
            var luisConfiguration = new LuisConfiguration(new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    { "luisVersionPrefix", "2.0" },
                    { "BUILD_BUILDID", 999.ToString(CultureInfo.InvariantCulture) },
                })
                .Build());

            luisConfiguration.VersionId.Should().Be("2.0.999");
        }

        [Test]
        [TestCase("speechRegion", "foo")]
        [TestCase("luisEndpointRegion", "bar")]
        public static void CreatesDefaultSpeechEndpoint(string regionKey, string regionValue)
        {
            var luisConfiguration = new LuisConfiguration(new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    { regionKey, regionValue },
                })
                .Build());

            luisConfiguration.SpeechEndpoint.Should().Be(
                new Uri(string.Format(
                    CultureInfo.InvariantCulture,
                    @"https://{0}.stt.speech.microsoft.com/speech/recognition/conversation/cognitiveservices/v1?language=en-US",
                    regionValue)));
        }

        [Test]
        [TestCase("speechRegion", "foo")]
        [TestCase("luisEndpointRegion", "bar")]
        public static void CreatesCustomSpeechEndpoint(string regionKey, string regionValue)
        {
            var appId = Guid.NewGuid().ToString();
            var luisConfiguration = new LuisConfiguration(new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    { regionKey, regionValue },
                    { "customSpeechAppId", appId },
                })
                .Build());

            luisConfiguration.SpeechEndpoint.Should().Be(
                new Uri(string.Format(
                    CultureInfo.InvariantCulture,
                    @"https://{0}.stt.speech.microsoft.com/speech/recognition/interactive/cognitiveservices/v1?language=en-US&?cid={1}",
                    regionValue,
                    appId)));
        }

        [Test]
        public static void CreatesDefaultSpeechEndpointFromSpeechRegion()
        {
            var speechRegion = Guid.NewGuid().ToString();
            var luisConfiguration = new LuisConfiguration(new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    { "speechRegion", speechRegion },
                })
                .Build());

            luisConfiguration.SpeechEndpoint.Should().Be(
                string.Format(
                    CultureInfo.InvariantCulture,
                    @"https://{0}.stt.speech.microsoft.com/speech/recognition/conversation/cognitiveservices/v1?language=en-US",
                    speechRegion));
        }
#if LUIS_V2

        [Test]
        public static void UseSpeechEndpointWithCustomSpeechAppId()
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    { "customSpeechAppId", Guid.NewGuid().ToString() },
                })
                .Build();

            var luisConfiguration = new LuisConfiguration(configuration);
            luisConfiguration.UseSpeechEndpoint.Should().BeTrue();
        }
#endif
#if LUIS_V3

        [Test]
        public static void UsesSlotNameBasedOnIsStaging()
        {
            var emptyLuisConfiguration = new LuisConfiguration(new ConfigurationBuilder().Build());
            emptyLuisConfiguration.SlotName.Should().Be("Production");

            var isStagingLuisConfiguration = new LuisConfiguration(new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    { "luisIsStaging", true.ToString(CultureInfo.InvariantCulture) },
                })
                .Build());
            isStagingLuisConfiguration.SlotName.Should().Be("Staging");

            var notIsStagingLuisConfiguration = new LuisConfiguration(new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    { "luisIsStaging", false.ToString(CultureInfo.InvariantCulture) },
                })
                .Build());
            notIsStagingLuisConfiguration.SlotName.Should().Be("Production");
        }
#endif
    }
}
