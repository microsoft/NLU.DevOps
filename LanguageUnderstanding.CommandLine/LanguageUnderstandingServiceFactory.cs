// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace LanguageUnderstanding.CommandLine
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Text;
    using Amazon;
    using Amazon.Runtime;
    using LanguageUnderstanding.Lex;
    using LanguageUnderstanding.Luis;
    using Microsoft.Extensions.Configuration;
    using Models;
    using Newtonsoft.Json.Linq;

    internal class LanguageUnderstandingServiceFactory
    {
        private const string LexServiceId = "lex";
        private const string LexPrefixConfigurationKey = "AWS_LEX_PREFIX";
        private const string LexBotNameConfigurationKey = "AWS_LEX_BOTNAME";
        private const string LexBotAliasConfigurationKey = "AWS_LEX_BOTALIAS";
        private const string LexAccessKeyConfigurationKey = "AWS_ACCESS_KEY";
        private const string LexSecretKeyConfigurationKey = "AWS_SECRET_KEY";
        private const string LexSecretKeyBase64ConfigurationKey = "AWS_SECRET_KEY_BASE64";
        private const string LexRegionConfigurationKey = "AWS_REGION";

        private const string LuisServiceId = "luis";
        private const string LuisPrefixConfigurationKey = "LUIS_PREFIX";
        private const string LuisAppNameConfigurationKey = "LUIS_APP_NAME";
        private const string LuisAppIdConfigurationKey = "LUIS_APP_ID";
        private const string LuisAppVersionConfigurationKey = "LUIS_APP_VERSION";
        private const string LuisAuthoringKeyConfigurationKey = "LUIS_AUTHORING_KEY";
        private const string LuisAuthoringRegionConfigurationKey = "LUIS_AUTHORING_REGION";

        private static readonly string TemplatesPath =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates");

        public static ILanguageUnderstandingService Create(string service, IConfiguration configuration)
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

        public static JToken GetServiceConfiguration(string service, ILanguageUnderstandingService instance)
        {
            switch (service)
            {
                case LexServiceId:
                    return GetLexConfig((LexLanguageUnderstandingService)instance);
                case LuisServiceId:
                    return GetLuisConfig((LuisLanguageUnderstandingService)instance);
                default:
                    throw new ArgumentException($"Invalid service type '{service}'.", nameof(service));
            }
        }

        private static ILanguageUnderstandingService CreateLex(IConfiguration configuration)
        {
            var userDefinedName = configuration[LexBotNameConfigurationKey];
            var botName = userDefinedName ?? GetRandomName(configuration[LexPrefixConfigurationKey]);
            var userDefinedAlias = configuration[LexBotAliasConfigurationKey];
            var botAlias = userDefinedAlias ?? GetRandomName(configuration[LexPrefixConfigurationKey]);
            var credentials = new BasicAWSCredentials(configuration[LexAccessKeyConfigurationKey], GetSecretKey(configuration));
            var regionEndpoint = GetRegionEndpoint(configuration[LexRegionConfigurationKey]);
            return new LexLanguageUnderstandingService(botName, botAlias, TemplatesPath, credentials, regionEndpoint);
        }

        private static ILanguageUnderstandingService CreateLuis(IConfiguration configuration)
        {
            var userDefinedName = configuration[LuisAppNameConfigurationKey];
            var appName = userDefinedName ?? GetRandomName(configuration[LuisPrefixConfigurationKey]);
            var appId = configuration[LuisAppIdConfigurationKey];
            var appVersion = configuration[LuisAppVersionConfigurationKey];
            var region = configuration[LuisAuthoringRegionConfigurationKey];
            var authoringKey = configuration[LuisAuthoringKeyConfigurationKey];
            return appId != null
                ? new LuisLanguageUnderstandingService(appName, appId, appVersion, region, authoringKey)
                : new LuisLanguageUnderstandingService(appName, region, authoringKey);
        }

        private static RegionEndpoint GetRegionEndpoint(string region)
        {
            return RegionEndpoint.EnumerableAllRegions.First(r => r.SystemName == region);
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

        private static JToken GetLexConfig(LexLanguageUnderstandingService instance)
        {
            return new JObject
            {
                { LexBotNameConfigurationKey, instance.BotName },
                { LexBotAliasConfigurationKey, instance.BotAlias },
            };
        }

        private static JToken GetLuisConfig(LuisLanguageUnderstandingService instance)
        {
            return new JObject
            {
                { LuisAppNameConfigurationKey, instance.AppName },
                { LuisAppIdConfigurationKey, instance.AppId },
                { LuisAppVersionConfigurationKey, instance.AppVersion },
            };
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
    }
}
