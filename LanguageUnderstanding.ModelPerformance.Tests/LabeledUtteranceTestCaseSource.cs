// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace LanguageUnderstanding.ModelPerformance.Tests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Json;
    using Microsoft.Extensions.Configuration;
    using Models;

    internal class LabeledUtteranceTestCaseSource
    {
        private const string AppSettingsPath = "appsettings.json";
        private const string AppSettingsLocalPath = "appsettings.local.json";

        public static IEnumerable<LabeledUtteranceTestCaseData> TestCases
        {
            get
            {
                var configurationBuilder = new ConfigurationBuilder()
                    .AddJsonFile(AppSettingsPath);

                if (File.Exists(AppSettingsLocalPath))
                {
                    configurationBuilder.AddJsonFile(AppSettingsLocalPath);
                }

                var configuration = configurationBuilder.Build();
                var expectedPath = configuration["expectedUtterances"];
                var actualPath = configuration["actualUtterances"];
                var testLabel = configuration["testLabel"];
                return ZipUtterances(expectedPath, actualPath, testLabel);
            }
        }

        public static IEnumerable<object[]> EntityTestCases
        {
            get
            {
                return from testCase in TestCases
                       from entityType in DistinctEntityTypes(testCase.ExpectedUtterance)
                       select new object[] { entityType, testCase };
            }
        }

        private static IEnumerable<LabeledUtteranceTestCaseData> ZipUtterances(string expectedPath, string actualPath, string testLabel)
        {
            if (string.IsNullOrEmpty(expectedPath) || string.IsNullOrEmpty(actualPath))
            {
                throw new InvalidOperationException("Could not find configuration for expected or actual utterances.");
            }

            var expectedUtterances = Serializer.Read<List<LabeledUtterance>>(expectedPath);
            var actualUtterances = Serializer.Read<List<LabeledUtterance>>(actualPath);

            if (expectedUtterances.Count != actualUtterances.Count)
            {
                throw new InvalidOperationException("Expected the same number of utterances in the expected and actual sources.");
            }

            return expectedUtterances.Zip(
                actualUtterances,
                (expected, actual) => new LabeledUtteranceTestCaseData(expected, actual, testLabel));
        }

        private static IEnumerable<string> DistinctEntityTypes(LabeledUtterance utterance)
        {
            return utterance.Entities?.Select(entity => entity.EntityType).Distinct() ?? Array.Empty<string>();
        }
    }
}
