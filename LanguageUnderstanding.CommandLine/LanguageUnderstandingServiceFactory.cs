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

        private static readonly string TemplatesPath =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates");

        public static ILanguageUnderstandingService Create(
            string service,
            IConfiguration configuration,
            JToken serviceConfiguration)
        {
            if (service == LexServiceId)
            {
                return CreateLex(configuration, serviceConfiguration);
            }

            throw new ArgumentException($"Invalid service type '{service}'.", nameof(service));
        }

        public static JToken GetServiceConfiguration(string service, ILanguageUnderstandingService instance)
        {
            if (service == LexServiceId)
            {
                return GetLexConfig((LexLanguageUnderstandingService)instance);
            }

            throw new ArgumentException($"Invalid service type '{service}'.", nameof(service));
        }

        private static ILanguageUnderstandingService CreateLex(
            IConfiguration configuration,
            JToken serviceConfiguration)
        {
            var userDefinedName = serviceConfiguration?[LexBotNameConfigurationKey].ToString() ?? configuration[LexBotNameConfigurationKey];
            var botName = userDefinedName ?? GetRandomName(configuration[LexPrefixConfigurationKey]);
            var userDefinedAlias = serviceConfiguration?[LexBotAliasConfigurationKey].ToString() ?? configuration[LexBotAliasConfigurationKey];
            var botAlias = userDefinedAlias ?? GetRandomName(configuration[LexPrefixConfigurationKey]);
            var credentials = new BasicAWSCredentials(configuration[LexAccessKeyConfigurationKey], GetSecretKey(configuration));
            var regionEndpoint = GetRegionEndpoint(configuration[LexRegionConfigurationKey]);
            return new LexLanguageUnderstandingService(botName, botAlias, TemplatesPath, credentials, regionEndpoint);
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
