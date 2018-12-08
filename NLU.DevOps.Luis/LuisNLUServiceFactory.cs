// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace NLU.DevOps.Luis
{
    using System;
    using System.Composition;
    using System.IO;
    using System.Linq;
    using Microsoft.Azure.CognitiveServices.Language.LUIS.Authoring.Models;
    using Microsoft.Extensions.Configuration;
    using Models;
    using Newtonsoft.Json;

    /// <summary>
    /// Factory for creating <see cref="LuisNLUService"/> instances.
    /// </summary>
    [Export("luis", typeof(INLUServiceFactory))]
    public class LuisNLUServiceFactory : INLUServiceFactory
    {
        private const string LuisPrefixConfigurationKey = "luisPrefix";
        private const string LuisAuthoringKeyConfigurationKey = "luisAuthoringKey";
        private const string LuisEndpointKeyConfigurationKey = "luisEndpointKey";
        private const string LuisAuthoringRegionConfigurationKey = "luisAuthoringRegion";
        private const string LuisEndpointRegionConfigurationKey = "luisEndpointRegion";
        private const string LuisSpeechKeyConfigurationKey = "luisSpeechKey";
        private const string LuisIsStagingConfigurationKey = "luisIsStaging";
        private const string LuisAppNameConfigurationKey = "luisAppName";

        private const string BuildIdConfigurationKey = "BUILD_BUILDID";

        private static readonly string LuisAppIdConfigurationKey = CamelCase(nameof(LuisNLUService.LuisAppId));
        private static readonly string LuisVersionIdConfigurationKey = CamelCase(nameof(LuisNLUService.LuisVersionId));

        /// <inheritdoc/>
        public INLUService CreateInstance(IConfiguration configuration, string settingsPath)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            var userDefinedName = configuration[LuisAppNameConfigurationKey];
            var appName = userDefinedName ?? GetRandomName(configuration[LuisPrefixConfigurationKey]);

            var isStagingString = configuration[LuisIsStagingConfigurationKey];
            var isStaging = isStagingString != null ? bool.Parse(isStagingString) : false;

            var luisSettings = settingsPath != null
                ? JsonConvert.DeserializeObject<LuisSettings>(File.ReadAllText(settingsPath))
                : new LuisSettings();

            var luisSpeechKey = configuration[LuisSpeechKeyConfigurationKey];

            var luisClient = luisSpeechKey != null
                ? new RestSpeechLuisClient(
                    configuration[LuisAuthoringKeyConfigurationKey],
                    configuration[LuisAuthoringRegionConfigurationKey],
                    configuration[LuisEndpointKeyConfigurationKey],
                    configuration[LuisEndpointRegionConfigurationKey],
                    luisSpeechKey,
                    isStaging)
                : new LuisClient(
                    configuration[LuisAuthoringKeyConfigurationKey],
                    configuration[LuisAuthoringRegionConfigurationKey],
                    configuration[LuisEndpointKeyConfigurationKey],
                    configuration[LuisEndpointRegionConfigurationKey],
                    isStaging);

            return new LuisNLUService(
                appName,
                configuration[LuisAppIdConfigurationKey],
                GetVersionId(configuration),
                luisSettings,
                luisClient);
        }

        private static string GetRandomName(string prefix)
        {
            var random = new Random();
            var randomString = new string(Enumerable.Repeat(0, 8)
                .Select(_ => (char)random.Next((int)'A', (int)'Z'))
                .ToArray());

            prefix = prefix != null ? $"{prefix}_" : prefix;
            return $"{prefix}{randomString}";
        }

        private static string GetVersionId(IConfiguration configuration)
        {
            var versionId = configuration[LuisVersionIdConfigurationKey];
            if (versionId == null)
            {
                return null;
            }

            var buildId = configuration[BuildIdConfigurationKey];
            var buildIdModifier = buildId != null ? $".{buildId}" : string.Empty;
            return $"{versionId}{buildIdModifier}";
        }

        private static string CamelCase(string s)
        {
            if (string.IsNullOrEmpty(s) || char.IsLower(s[0]))
            {
                return s;
            }

            return char.ToLowerInvariant(s[0]) + s.Substring(1);
        }
    }
}
