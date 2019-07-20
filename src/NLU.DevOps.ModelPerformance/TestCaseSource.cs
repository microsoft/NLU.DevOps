// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace NLU.DevOps.ModelPerformance
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using Microsoft.Extensions.Configuration;
    using Models;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using NUnit.Framework;

    /// <summary>
    /// Test case generator for NLU confusion matrix tests.
    /// </summary>
    public static class TestCaseSource
    {
        private const string AppSettingsPath = "appsettings.json";
        private const string AppSettingsLocalPath = "appsettings.local.json";

        /// <summary>
        /// Gets the passing tests.
        /// </summary>
        public static IEnumerable<TestCaseData> PassingTests => TestCases
            .Where(IsTrue)
            .Select(ToTestCaseData);

        /// <summary>
        /// Gets the failing tests.
        /// </summary>
        public static IEnumerable<TestCaseData> FailingTests => TestCases
            .Where(IsFalse)
            .Select(ToTestCaseData);

        private static string TestLabel => TestContext.Parameters.Get(ConfigurationConstants.TestLabelKey) ?? Configuration[ConfigurationConstants.TestLabelKey];

        private static IConfiguration Configuration { get; } = new ConfigurationBuilder()
            .AddJsonFile(AppSettingsPath, true)
            .AddJsonFile(AppSettingsLocalPath, true)
            .AddEnvironmentVariables()
            .Build();

        private static IReadOnlyList<TestCase> TestCases { get; } = LoadTestCases();

        internal static TestCase ToTextTestCase(LabeledUtterance[] utterances)
        {
            var expectedUtterance = utterances[0];
            var actualUtterance = utterances[1];
            var expected = expectedUtterance.Text;
            var actual = actualUtterance.Text;

            if (expected == null && actual == null)
            {
                return TrueNegative(
                    ComparisonTargetKind.Text,
                    expectedUtterance,
                    actualUtterance,
                    Array.Empty<string>(),
                    "Both utterances are 'null'.",
                    "Text");
            }

            if (actual == null)
            {
                return FalseNegative(
                    ComparisonTargetKind.Text,
                    expectedUtterance,
                    actualUtterance,
                    new[] { expected },
                    $"Actual text is 'null', expected '{expected}'",
                    "Text");
            }

            if (EqualsNormalized(expected, actual))
            {
                return TruePositive(
                    ComparisonTargetKind.Text,
                    expectedUtterance,
                    actualUtterance,
                    new[] { expected },
                    "Utterances have matching text.",
                    "Text");
            }

            return FalsePositive(
                ComparisonTargetKind.Text,
                expectedUtterance,
                actualUtterance,
                new[] { expected, actual },
                $"Expected text '{expected}', actual text '{actual}'.",
                "Text");
        }

        internal static TestCase ToIntentTestCase(LabeledUtterance[] utterances)
        {
            var expectedUtterance = utterances[0];
            var actualUtterance = utterances[1];
            var text = expectedUtterance.Text;
            var expected = expectedUtterance.Intent;
            var actual = actualUtterance.Intent;

            if (actual == null || actual == "None")
            {
                return expected == null || expected == "None"
                    ? TrueNegative(
                        ComparisonTargetKind.Intent,
                        expectedUtterance,
                        actualUtterance,
                        new[] { text },
                        "Both intents are 'None'.",
                        "Intent")
                    : FalseNegative(
                        ComparisonTargetKind.Intent,
                        expectedUtterance,
                        actualUtterance,
                        new[] { expected, text },
                        $"Actual intent is 'None', expected '{expected}'",
                        "Intent");
            }

            if (expected == actual)
            {
                return TruePositive(
                    ComparisonTargetKind.Intent,
                    expectedUtterance,
                    actualUtterance,
                    new[] { expected, text },
                    "Utterances have matching intent.",
                    "Intent");
            }

            return FalsePositive(
                ComparisonTargetKind.Intent,
                expectedUtterance,
                actualUtterance,
                new[] { expected, actual, text },
                $"Expected intent '{expected}', actual intent '{actual}'.",
                "Intent");
        }

        internal static IEnumerable<TestCase> ToEntityTestCases(LabeledUtterance[] utterances)
        {
            var expectedUtterance = utterances[0];
            var actualUtterance = utterances[1];
            var text = expectedUtterance.Text;
            var expected = expectedUtterance.Entities;
            var actual = actualUtterance.Entities;

            if ((expected == null || expected.Count == 0) && (actual == null || actual.Count == 0))
            {
                yield return TrueNegative(
                    ComparisonTargetKind.Entity,
                    expectedUtterance,
                    actualUtterance,
                    new[] { text },
                    "Neither utterances have entities.",
                    "Entity");

                yield break;
            }

            bool isEntityMatch(Entity expectedEntity, Entity actualEntity)
            {
                return expectedEntity.EntityType == actualEntity.EntityType
                    && ((EqualsNormalized(expectedEntity.MatchText, actualEntity.MatchText)
                    && expectedEntity.MatchIndex == actualEntity.MatchIndex)
                    /* Required case to support NLU providers that do not specify matched text */
                    || EqualsNormalizedJson(expectedEntity.MatchText, actualEntity.EntityValue)
                    || EqualsNormalizedJson(expectedEntity.EntityValue, actualEntity.EntityValue)
                    /* Required case to support FalsePositiveEntity scenarios */
                    || EqualsNormalizedJson(expectedEntity.EntityValue, actualEntity.MatchText));
            }

            if (expected != null)
            {
                foreach (var entity in expected)
                {
                    // "Soft" entity match test cases
                    var entityValue = entity.MatchText ?? entity.EntityValue;
                    var formattedEntityValue = entityValue.ToString(Formatting.None);
                    if (actual == null || !actual.Any(actualEntity => isEntityMatch(entity, actualEntity)))
                    {
                        yield return FalseNegative(
                            ComparisonTargetKind.Entity,
                            expectedUtterance,
                            actualUtterance,
                            new[] { entity.EntityType, formattedEntityValue, text },
                            $"Actual utterance does not have entity matching '{entityValue}'.",
                            "Entity",
                            entity.EntityType);
                    }
                    else
                    {
                        yield return TruePositive(
                            ComparisonTargetKind.Entity,
                            expectedUtterance,
                            actualUtterance,
                            new[] { entity.EntityType, formattedEntityValue, text },
                            $"Both utterances have entity '{entityValue}'.",
                            "Entity",
                            entity.EntityType);
                    }

                    if (entity.EntityValue != null && entity.EntityValue.Type != JTokenType.Null)
                    {
                        // "Semantic" entity value match test cases
                        bool isEntityValueMatch(Entity actualEntity)
                        {
                            return entity.EntityType == actualEntity.EntityType
                                && ContainsSubtree(entity.EntityValue, actualEntity.EntityValue);
                        }

                        if (actual == null || !actual.Any(isEntityValueMatch))
                        {
                            yield return FalseNegative(
                                ComparisonTargetKind.EntityValue,
                                expectedUtterance,
                                actualUtterance,
                                new[] { entity.EntityType, formattedEntityValue, text },
                                $"Actual utterance does not have entity value matching '{formattedEntityValue}'.",
                                "Entity",
                                entity.EntityType);
                        }
                        else
                        {
                            yield return TruePositive(
                                ComparisonTargetKind.EntityValue,
                                expectedUtterance,
                                actualUtterance,
                                new[] { entity.EntityType, formattedEntityValue, text },
                                $"Both utterances have entity value '{formattedEntityValue}'.",
                                "Entity",
                                entity.EntityType);
                        }
                    }

                    if (entity.EntityResolution != null && entity.EntityResolution.Type != JTokenType.Null)
                    {
                        bool isEntityResolutionMatch(Entity actualEntity)
                        {
                            return ContainsSubtree(entity.EntityResolution, actualEntity.EntityResolution);
                        }

                        var formattedEntityResolution = entity.EntityResolution.ToString(Formatting.None);
                        if (actual == null || !actual.Any(isEntityResolutionMatch))
                        {
                            yield return FalseNegative(
                                ComparisonTargetKind.EntityResolution,
                                expectedUtterance,
                                actualUtterance,
                                new[] { entity.EntityType, formattedEntityResolution, text },
                                $"Actual utterance does not have entity resolution matching '{formattedEntityResolution}'.",
                                "Entity",
                                entity.EntityType);
                        }
                        else
                        {
                            yield return TruePositive(
                                ComparisonTargetKind.EntityResolution,
                                expectedUtterance,
                                actualUtterance,
                                new[] { entity.EntityType, formattedEntityResolution, text },
                                $"Both utterances contain expected resolution '{formattedEntityResolution}'.",
                                "Entity",
                                entity.EntityType);
                        }
                    }
                }
            }

            if (actual != null)
            {
                foreach (var entity in actual)
                {
                    var entityValue = entity.MatchText ?? entity.EntityValue;
                    if (expected == null || !expected.Any(expectedEntity => isEntityMatch(entity, expectedEntity)))
                    {
                        yield return FalsePositive(
                            ComparisonTargetKind.Entity,
                            expectedUtterance,
                            actualUtterance,
                            new[] { entity.EntityType, entityValue.ToString(Formatting.None), text },
                            $"Expected utterance does not have entity matching '{entityValue}'.",
                            "Entity",
                            entity.EntityType);
                    }
                }
            }
        }

        private static TestCaseData ToTestCaseData(this TestCase testCase)
        {
            var testCaseData = new TestCaseData(testCase.Because)
            {
                TestName = testCase.TestName,
            };

            testCase.Categories.ForEach(category => testCaseData.SetCategory(category));
            return testCaseData;
        }

        private static bool IsTrue(this TestCase testCase)
        {
            return testCase.ResultKind == ConfusionMatrixResultKind.TruePositive
                || testCase.ResultKind == ConfusionMatrixResultKind.TrueNegative;
        }

        private static bool IsFalse(this TestCase testCase)
        {
            return testCase.ResultKind == ConfusionMatrixResultKind.FalsePositive
                || testCase.ResultKind == ConfusionMatrixResultKind.FalseNegative;
        }

        private static IReadOnlyList<TestCase> LoadTestCases()
        {
            var expectedPath = TestContext.Parameters.Get(ConfigurationConstants.ExpectedUtterancesPathKey) ?? Configuration[ConfigurationConstants.ExpectedUtterancesPathKey];
            var actualPath = TestContext.Parameters.Get(ConfigurationConstants.ActualUtterancesPathKey) ?? Configuration[ConfigurationConstants.ActualUtterancesPathKey];

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

            var zippedUtterances = expectedUtterances
                .Zip(actualUtterances, (expected, actual) => new[] { expected, actual })
                .ToList();

            return zippedUtterances.Select(ToTextTestCase)
                .Concat(zippedUtterances.Select(ToIntentTestCase))
                .Concat(zippedUtterances.SelectMany(ToEntityTestCases))
                .ToList();
        }

        private static List<LabeledUtterance> Read(string path)
        {
            var serializer = JsonSerializer.CreateDefault();
            using (var jsonReader = new JsonTextReader(File.OpenText(path)))
            {
                return serializer.Deserialize<List<LabeledUtterance>>(jsonReader);
            }
        }

        private static bool EqualsNormalizedJson(JToken x, JToken y)
        {
            // Entity is not a match if both values are null
            if (x == null && y == null)
            {
                return false;
            }

            return x?.Type == JTokenType.String
                ? EqualsNormalizedJson(x.Value<string>(), y)
                : false;
        }

        private static bool EqualsNormalizedJson(string x, JToken y)
        {
            // Entity is not a match if both values are null
            if (x == null && y == null)
            {
                return false;
            }

            return y?.Type == JTokenType.String
                ? EqualsNormalized(x, y.Value<string>())
                : false;
        }

        private static bool EqualsNormalized(string x, string y)
        {
            string normalize(string s)
            {
                if (s == null)
                {
                    return null;
                }

                var normalizedSpace = Regex.Replace(s, @"\s+", " ");
                var withoutPunctuation = Regex.Replace(normalizedSpace, @"[^\w ]", string.Empty);
                return withoutPunctuation.Trim();
            }

            return string.Equals(normalize(x), normalize(y), StringComparison.OrdinalIgnoreCase);
        }

        private static bool ContainsSubtree(JToken expected, JToken actual)
        {
            if (expected == null)
            {
                return true;
            }

            if (actual == null)
            {
                return false;
            }

            switch (expected)
            {
                case JObject expectedObject:
                    var actualObject = actual as JObject;
                    if (actualObject == null)
                    {
                        return false;
                    }

                    foreach (var expectedProperty in expectedObject.Properties())
                    {
                        var actualProperty = actualObject.Property(expectedProperty.Name, StringComparison.Ordinal);
                        if (!ContainsSubtree(expectedProperty.Value, actualProperty?.Value))
                        {
                            return false;
                        }
                    }

                    return true;
                case JArray expectedArray:
                    var actualArray = actual as JArray;
                    if (actualArray == null)
                    {
                        return false;
                    }

                    foreach (var expectedItem in expectedArray)
                    {
                        // Order is not asserted
                        if (!actualArray.Any(actualItem => ContainsSubtree(expectedItem, actualItem)))
                        {
                            return false;
                        }
                    }

                    return true;
                default:
                    return JToken.DeepEquals(expected, actual);
            }
        }

        private static TestCase TruePositive(
            ComparisonTargetKind targetKind,
            LabeledUtterance expectedUtterance,
            LabeledUtterance actualUtterance,
            string[] args,
            string because,
            params string[] categories)
        {
            return CreateTestCase(
                ConfusionMatrixResultKind.TruePositive,
                targetKind,
                expectedUtterance,
                actualUtterance,
                args,
                because,
                categories.Append("TruePositive"));
        }

        private static TestCase TrueNegative(
            ComparisonTargetKind targetKind,
            LabeledUtterance expectedUtterance,
            LabeledUtterance actualUtterance,
            string[] args,
            string because,
            params string[] categories)
        {
            return CreateTestCase(
                ConfusionMatrixResultKind.TrueNegative,
                targetKind,
                expectedUtterance,
                actualUtterance,
                args,
                because,
                categories.Append("TrueNegative"));
        }

        private static TestCase FalsePositive(
            ComparisonTargetKind targetKind,
            LabeledUtterance expectedUtterance,
            LabeledUtterance actualUtterance,
            string[] args,
            string because,
            params string[] categories)
        {
            return CreateTestCase(
                ConfusionMatrixResultKind.FalsePositive,
                targetKind,
                expectedUtterance,
                actualUtterance,
                args,
                because,
                categories.Append("FalsePositive"));
        }

        private static TestCase FalseNegative(
            ComparisonTargetKind targetKind,
            LabeledUtterance expectedUtterance,
            LabeledUtterance actualUtterance,
            string[] args,
            string because,
            params string[] categories)
        {
            return CreateTestCase(
                ConfusionMatrixResultKind.FalseNegative,
                targetKind,
                expectedUtterance,
                actualUtterance,
                args,
                because,
                categories.Append("FalseNegative"));
        }

        private static TestCase CreateTestCase(
            ConfusionMatrixResultKind resultKind,
            ComparisonTargetKind targetKind,
            LabeledUtterance expectedUtterance,
            LabeledUtterance actualUtterance,
            string[] args,
            string because,
            IEnumerable<string> categories)
        {
            var testLabel = TestLabel != null ? $"[{TestLabel}] " : string.Empty;
            return new TestCase(
                resultKind,
                targetKind,
                expectedUtterance,
                actualUtterance,
                $"{testLabel}{resultKind}{targetKind}('{string.Join("', '", args)}')",
                because,
                categories);
        }
    }
}
