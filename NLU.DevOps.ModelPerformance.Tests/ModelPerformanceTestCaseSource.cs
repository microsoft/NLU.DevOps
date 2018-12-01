// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace NLU.DevOps.ModelPerformance.Tests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Microsoft.Extensions.Configuration;
    using Models;
    using Newtonsoft.Json;
    using NUnit.Framework;

    internal class ModelPerformanceTestCaseSource
    {
        private const string AppSettingsPath = "appsettings.json";
        private const string AppSettingsLocalPath = "appsettings.local.json";

        public static IEnumerable<LabeledUtteranceTestCaseData> TestCases
        {
            get
            {
                var configuration = new ConfigurationBuilder()
                    .AddJsonFile(AppSettingsPath)
                    .AddJsonFile(AppSettingsLocalPath, true)
                    .AddEnvironmentVariables()
                    .Build();

                var expectedPath = TestContext.Parameters.Get("expected") ?? configuration["expected"];
                var actualPath = TestContext.Parameters.Get("actual") ?? configuration["actual"];
                var testLabel = TestContext.Parameters.Get("testLabel") ?? configuration["testLabel"];
                return ZipUtterances(expectedPath, actualPath, testLabel);
            }
        }

        public static IEnumerable<EntityTestCaseData> ExpectedEntityTestCases
        {
            get
            {
                return from testCase in TestCases
                       from entityTestCase in GetEntityTestCaseData(testCase, testCase.ExpectedUtterance)
                       select entityTestCase;
            }
        }

        public static IEnumerable<EntityTestCaseData> ActualEntityTestCases
        {
            get
            {
                return from testCase in TestCases
                       from entityTestCase in GetEntityTestCaseData(testCase, testCase.ActualUtterance)
                       select entityTestCase;
            }
        }

        public static IEnumerable<EntityTestCaseData> ExpectedEntityValueTestCases
        {
            get
            {
                return from entityTestCase in ExpectedEntityTestCases
                       where entityTestCase.ExpectedEntity.EntityValue != null
                       select entityTestCase;
            }
        }

        private static IEnumerable<LabeledUtteranceTestCaseData> ZipUtterances(string expectedPath, string actualPath, string testLabel)
        {
            if (string.IsNullOrEmpty(expectedPath) || string.IsNullOrEmpty(actualPath))
            {
                throw new InvalidOperationException("Could not find configuration for expected or actual utterances.");
            }

            var expectedUtterances = Read(expectedPath);
            var actualUtterances = Read(actualPath);

            if (expectedUtterances.Count != actualUtterances.Count)
            {
                throw new InvalidOperationException("Expected the same number of utterances in the expected and actual sources.");
            }

            return expectedUtterances.Zip(
                actualUtterances,
                (expected, actual) => new LabeledUtteranceTestCaseData(expected, actual, testLabel));
        }

        private static IEnumerable<EntityTestCaseData> GetEntityTestCaseData(
            LabeledUtteranceTestCaseData testCase,
            LabeledUtterance utterance)
        {
            var otherUtterance = testCase.ExpectedUtterance == utterance
                ? testCase.ActualUtterance
                : testCase.ExpectedUtterance;

            var entityTestCases = utterance.Entities?
                .Select(entity => new EntityTestCaseData(
                    entity,
                    otherUtterance,
                    testCase.ExpectedUtterance.Text,
                    testCase.TestLabel));

            return entityTestCases ?? Array.Empty<EntityTestCaseData>();
        }

        private static List<LabeledUtterance> Read(string path)
        {
            var serializer = JsonSerializer.CreateDefault();
            using (var jsonReader = new JsonTextReader(File.OpenText(path)))
            {
                return serializer.Deserialize<List<LabeledUtterance>>(jsonReader);
            }
        }
    }
}
