// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace NLU.DevOps.Luis
{
    using System;
    using System.Composition;
    using System.Linq;
    using Microsoft.Extensions.Configuration;
    using Models;

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
        private const string LuisIsStagingConfigurationKey = "luisIsStaging";

        private const string BuildIdConfigurationKey = "BUILD_BUILDID";

        private static readonly string LuisAppNameConfigurationKey = CamelCase(nameof(LuisNLUService.LuisAppName));
        private static readonly string LuisAppIdConfigurationKey = CamelCase(nameof(LuisNLUService.LuisAppId));
        private static readonly string LuisVersionIdConfigurationKey = CamelCase(nameof(LuisNLUService.LuisVersionId));

        /// <inheritdoc/>
        public INLUService CreateInstance(IConfiguration configuration)
        {
            var userDefinedName = configuration[LuisAppNameConfigurationKey];
            var appName = userDefinedName ?? GetRandomName(configuration[LuisPrefixConfigurationKey]);

            var isStagingString = configuration[LuisIsStagingConfigurationKey];
            var isStaging = isStagingString != null ? bool.Parse(isStagingString) : false;

            var builder = new LuisNLUServiceBuilder
            {
                AppName = appName,
                AppId = configuration[LuisAppIdConfigurationKey],
                AppVersion = GetVersionId(configuration),
                AuthoringRegion = configuration[LuisAuthoringRegionConfigurationKey],
                EndpointRegion = configuration[LuisEndpointRegionConfigurationKey],
                IsStaging = isStaging,
                AuthoringKey = configuration[LuisAuthoringKeyConfigurationKey],
                EndpointKey = configuration[LuisEndpointKeyConfigurationKey]
            };

            return builder.Build();
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
