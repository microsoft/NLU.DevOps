// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace NLU.DevOps.CommandLine
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Text;
    using Amazon;
    using Amazon.Runtime;
    using Lex;
    using Luis;
    using Microsoft.Extensions.Configuration;
    using Models;
    using Newtonsoft.Json.Linq;

    internal class NLUServiceFactory
    {
        private const string LexServiceId = "lex";
        private const string LexPrefixConfigurationKey = "lexPrefix";
        private const string LexAccessKeyConfigurationKey = "awsAccessKey";
        private const string LexSecretKeyConfigurationKey = "awsSecretKey";
        private const string LexSecretKeyBase64ConfigurationKey = "awsSecretKeyBase64";
        private const string LexRegionConfigurationKey = "awsRegion";

        private const string LuisServiceId = "luis";
        private const string LuisPrefixConfigurationKey = "luisPrefix";
        private const string LuisAuthoringKeyConfigurationKey = "luisAuthoringKey";
        private const string LuisEndpointKeyConfigurationKey = "luisEndpointKey";
        private const string LuisAuthoringRegionConfigurationKey = "luisAuthoringRegion";
        private const string LuisEndpointRegionConfigurationKey = "luisEndpointRegion";
        private const string LuisIsStagingConfigurationKey = "luisIsStaging";

        private const string BuildVersionVariableKey = "nluBuildVersionKey";

        private static readonly string LexBotNameConfigurationKey = CamelCase(nameof(LexNLUService.LexBotName));
        private static readonly string LexBotAliasConfigurationKey = CamelCase(nameof(LexNLUService.LexBotAlias));

        private static readonly string LuisAppNameConfigurationKey = CamelCase(nameof(LuisNLUService.LuisAppName));
        private static readonly string LuisAppIdConfigurationKey = CamelCase(nameof(LuisNLUService.LuisAppId));
        private static readonly string LuisAppVersionConfigurationKey = CamelCase(nameof(LuisNLUService.LuisAppVersion));

        private static readonly string TemplatesPath =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates");

        public static INLUService Create(string service, IConfiguration configuration)
        {
            switch (service)
            {
                case LexServiceId:
                    return CreateLex(configuration);
                case LuisServiceId:
                    return CreateLuis(configuration);
                default:
                    throw new ArgumentException($"Invalid service type '{service}'.", nameof(service));
            }
        }

        private static INLUService CreateLex(IConfiguration configuration)
        {
            var userDefinedName = configuration[LexBotNameConfigurationKey];
            var botName = userDefinedName ?? GetRandomName(configuration[LexPrefixConfigurationKey]);
            var botAlias = configuration[LexBotAliasConfigurationKey] ?? botName;
            var credentials = new BasicAWSCredentials(configuration[LexAccessKeyConfigurationKey], GetSecretKey(configuration));
            var regionEndpoint = GetRegionEndpoint(configuration[LexRegionConfigurationKey]);
            return new LexNLUService(botName, botAlias, TemplatesPath, credentials, regionEndpoint);
        }

        private static INLUService CreateLuis(IConfiguration configuration)
        {
            var userDefinedName = configuration[LuisAppNameConfigurationKey];
            var appName = userDefinedName ?? GetRandomName(configuration[LuisPrefixConfigurationKey]);

            var isStagingString = configuration[LuisIsStagingConfigurationKey];
            var isStaging = isStagingString != null ? bool.Parse(isStagingString) : false;

            var builder = new LuisNLUServiceBuilder
            {
                AppName = appName,
                AppId = configuration[LuisAppIdConfigurationKey],
                AppVersion = GetAppVersion(configuration),
                AuthoringRegion = configuration[LuisAuthoringRegionConfigurationKey],
                EndpointRegion = configuration[LuisEndpointRegionConfigurationKey],
                IsStaging = isStaging,
                AuthoringKey = configuration[LuisAuthoringKeyConfigurationKey],
                EndpointKey = configuration[LuisEndpointKeyConfigurationKey]
            };

            return builder.Build();
        }

        private static RegionEndpoint GetRegionEndpoint(string region)
        {
            return RegionEndpoint.EnumerableAllRegions.FirstOrDefault(r => r.SystemName == region);
        }

        private static string GetSecretKey(IConfiguration configuration)
        {
            var secretKey = configuration[LexSecretKeyConfigurationKey];
            if (secretKey != null)
            {
                return secretKey;
            }

            var secretKeyBase64 = configuration[LexSecretKeyBase64ConfigurationKey];
            if (secretKeyBase64 == null)
            {
                return null;
            }

            return Encoding.UTF8.GetString(Convert.FromBase64String(secretKeyBase64));
        }

        private static string GetRandomName(string prefix)
        {
            prefix = prefix != null ? $"{prefix}_" : prefix;
            return $"{prefix}{GetRandomString()}";
        }

        private static string GetRandomString()
        {
            var random = new Random();
            return new string(Enumerable.Repeat(0, 8)
                .Select(_ => (char)random.Next((int)'A', (int)'Z'))
                .ToArray());
        }

        private static string GetAppVersion(IConfiguration configuration)
        {
            var appVersion = configuration[LuisAppVersionConfigurationKey];
            if (appVersion == null)
            {
                return null;
            }

            var buildIdKey = configuration[BuildVersionVariableKey];
            var buildId = buildIdKey != null ? configuration[buildIdKey] : null;
            var buildIdModifier = buildId != null ? $".{buildId}" : string.Empty;
            return $"{appVersion}{buildIdModifier}";
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
