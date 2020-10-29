// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace NLU.DevOps.ModelPerformance
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using Core;
    using Models;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using NUnit.Framework;

    /// <summary>
    /// Test case generator for NLU confusion matrix tests.
    /// </summary>
    public static class TestCaseSource
    {
        /// <summary>
        /// Gets the passing tests.
        /// </summary>
        public static IEnumerable<TestCaseData> PassingTests => LazyTestCases
            .Value
            .Where(IsTrue)
            .Select(ToTestCaseData);

        /// <summary>
        /// Gets the failing tests.
        /// </summary>
        public static IEnumerable<TestCaseData> FailingTests => LazyTestCases
            .Value
            .Where(IsFalse)
            .Select(ToTestCaseData);

        private static Lazy<IReadOnlyList<TestCase>> LazyTestCases { get; } =
            new Lazy<IReadOnlyList<TestCase>>(LoadTestCases);

        /// <summary>
        /// Generates the test cases.
        /// </summary>
        /// <returns>The test cases.</returns>
        /// <param name="expectedUtterances">Expected utterances.</param>
        /// <param name="actualUtterances">Actual utterances.</param>
        /// <param name="testSettings">Test settings.</param>
        public static NLUCompareResults GetNLUCompareResults(
            IReadOnlyList<ILabeledUtterance> expectedUtterances,
            IReadOnlyList<ILabeledUtterance> actualUtterances,
            TestSettings testSettings)
        {
            if (expectedUtterances == null)
            {
                throw new ArgumentNullException(nameof(expectedUtterances));
            }

            if (actualUtterances == null)
            {
                throw new ArgumentNullException(nameof(actualUtterances));
            }

            if (testSettings == null)
            {
                throw new ArgumentNullException(nameof(testSettings));
            }

            if (expectedUtterances.Count != actualUtterances.Count)
            {
                throw new InvalidOperationException("Expected the same number of utterances in the expected and actual sources.");
            }

            string getUtteranceId(ILabeledUtterance utterance, int index)
            {
                return utterance.GetUtteranceId() ?? index.ToString(CultureInfo.InvariantCulture);
            }

            testSettings = testSettings ?? new TestSettings(default(string), false);

            var zippedUtterances = expectedUtterances
                .Select((utterance, i) => new { Utterance = utterance, UtteranceId = getUtteranceId(utterance, i) })
                .Zip(actualUtterances, (expected, actual) => new LabeledUtteranceTestInput(expected.UtteranceId, expected.Utterance, actual, testSettings))
                .ToList();

            var testCases = zippedUtterances.SelectMany(ToIntentTestCases)
                .Concat(zippedUtterances.SelectMany(ToEntityTestCases))
                .Concat(zippedUtterances.SelectMany(ToTextTestCases));

            return new NLUCompareResults(testCases.ToList());
        }

        internal static IEnumerable<TestCase> ToTextTestCases(LabeledUtteranceTestInput testInput)
        {
            var expectedUtterance = testInput.Expected;

            // Skip if the test was not a speech test
            if (expectedUtterance.GetProperty<string>("speechFile") == null)
            {
                yield break;
            }

            var actualUtterance = testInput.Actual;
            var expected = expectedUtterance.Text;
            var actual = actualUtterance.Text;
            var score = actualUtterance.GetTextScore();

            if (expected == null && actual == null)
            {
                yield return TrueNegative(
                    testInput.UtteranceId,
                    ComparisonTargetKind.Text,
                    expectedUtterance,
                    actualUtterance,
                    score,
                    null,
                    Array.Empty<string>(),
                    "Both utterances are 'null'.",
                    "Text");
            }
            else if (actual == null)
            {
                yield return FalseNegative(
                    testInput.UtteranceId,
                    ComparisonTargetKind.Text,
                    expectedUtterance,
                    actualUtterance,
                    score,
                    null,
                    new[] { expected },
                    $"Actual text is 'null', expected '{expected}'",
                    "Text");
            }
            else if (EqualsNormalized(expected, actual))
            {
                yield return TruePositive(
                    testInput.UtteranceId,
                    ComparisonTargetKind.Text,
                    expectedUtterance,
                    actualUtterance,
                    score,
                    null,
                    new[] { expected },
                    "Utterances have matching text.",
                    "Text");
            }
            else if (!testInput.TestSettings.UnitTestMode
                || testInput.Expected.HasProperty("text")
                || testInput.Expected.HasProperty("query"))
            {
                yield return FalsePositive(
                    testInput.UtteranceId,
                    ComparisonTargetKind.Text,
                    expectedUtterance,
                    actualUtterance,
                    score,
                    null,
                    new[] { expected, actual },
                    $"Expected text '{expected}', actual text '{actual}'.",
                    "Text");
            }
        }

        internal static IEnumerable<TestCase> ToIntentTestCases(LabeledUtteranceTestInput testInput)
        {
            var expectedUtterance = testInput.Expected;
            var actualUtterance = testInput.Actual;
            var score = actualUtterance.GetScore();

            var text = expectedUtterance.Text;
            var expected = expectedUtterance.Intent;
            var actual = actualUtterance.Intent;

            bool isNoneIntent(string intent)
            {
                return intent == null || intent == testInput.TestSettings.TrueNegativeIntent;
            }

            if (isNoneIntent(expected) && isNoneIntent(actual))
            {
                yield return TrueNegative(
                    testInput.UtteranceId,
                    ComparisonTargetKind.Intent,
                    expectedUtterance,
                    actualUtterance,
                    score,
                    null,
                    new[] { text },
                    "Both intents are 'None'.",
                    "Intent");

                yield break;
            }

            if (expected == actual)
            {
                yield return TruePositive(
                    testInput.UtteranceId,
                    ComparisonTargetKind.Intent,
                    expectedUtterance,
                    actualUtterance,
                    score,
                    expected,
                    new[] { expected, text },
                    "Utterances have matching intent.",
                    "Intent");

                yield break;
            }

            if (!isNoneIntent(expected))
            {
                yield return FalseNegative(
                    testInput.UtteranceId,
                    ComparisonTargetKind.Intent,
                    expectedUtterance,
                    actualUtterance,
                    score,
                    expected,
                    new[] { expected, actual, text },
                    $"Expected intent '{expected}', actual intent '{actual}'.",
                    "Intent");
            }

            if (!isNoneIntent(actual)
                && (!testInput.TestSettings.UnitTestMode
                || testInput.Expected.HasProperty("intent")))
            {
                yield return FalsePositive(
                    testInput.UtteranceId,
                    ComparisonTargetKind.Intent,
                    expectedUtterance,
                    actualUtterance,
                    score,
                    actual,
                    new[] { expected, actual, text },
                    $"Expected intent '{expected}', actual intent '{actual}'.",
                    "Intent");
            }
        }

        internal static IEnumerable<TestCase> ToEntityTestCases(LabeledUtteranceTestInput testInput)
        {
            var expectedUtterance = testInput.Expected;
            var actualUtterance = testInput.Actual;
            var text = expectedUtterance.Text;
            var expected = expectedUtterance.Entities;
            var actual = actualUtterance.Entities;

            if ((expected == null || expected.Count == 0) && (actual == null || actual.Count == 0))
            {
                yield return TrueNegative(
                    testInput.UtteranceId,
                    ComparisonTargetKind.Entity,
                    expectedUtterance,
                    actualUtterance,
                    0,
                    null,
                    new[] { text },
                    "Neither utterances have entities.",
                    "Entity");

                yield break;
            }

            bool isEntityMatch(IEntity expectedEntity, IEntity actualEntity)
            {
                return expectedEntity.EntityType == actualEntity.EntityType
                    && (isEntityTextMatch(expectedEntity, actualEntity)
                    || isEntityValueMatch(expectedEntity, actualEntity));
            }

            bool isEntityTextMatch(IEntity expectedEntity, IEntity actualEntity)
            {
                return expectedEntity.MatchText != null
                    && EqualsNormalized(expectedEntity.MatchText, actualEntity.MatchText)
                    && expectedEntity.MatchIndex == actualEntity.MatchIndex;
            }

            bool isEntityValueMatch(IEntity expectedEntity, IEntity actualEntity)
            {
                /* Required case to support NLU providers that do not specify matched text */
                return actualEntity.MatchText == null
                    && (EqualsNormalizedJson(expectedEntity.EntityValue, actualEntity.EntityValue)
                    || EqualsNormalizedJson(expectedEntity.MatchText, actualEntity.EntityValue));
            }

            if (expected != null)
            {
                foreach (var entity in expected)
                {
                    // "Soft" entity match test cases
                    var entityValue = entity.MatchText ?? entity.EntityValue;
                    var formattedEntity = entityValue.ToString(Formatting.None);
                    var matchedEntity = actual != null
                        ? actual.FirstOrDefault(actualEntity => isEntityMatch(entity, actualEntity))
                        : null;

                    var score = matchedEntity.GetScore();

                    if (matchedEntity == null)
                    {
                        yield return FalseNegative(
                            testInput.UtteranceId,
                            ComparisonTargetKind.Entity,
                            expectedUtterance,
                            actualUtterance,
                            score,
                            entity.EntityType,
                            new[] { entity.EntityType, formattedEntity, text },
                            $"Actual utterance does not have entity matching '{entityValue}'.",
                            "Entity");
                    }
                    else
                    {
                        yield return TruePositive(
                            testInput.UtteranceId,
                            ComparisonTargetKind.Entity,
                            expectedUtterance,
                            actualUtterance,
                            score,
                            entity.EntityType,
                            new[] { entity.EntityType, formattedEntity, text },
                            $"Both utterances have entity '{entityValue}'.",
                            "Entity");
                    }

                    if (matchedEntity != null)
                    {
                        if (entity.EntityValue != null && entity.EntityValue.Type != JTokenType.Null)
                        {
                            var formattedEntityValue = entity.EntityValue.ToString(Formatting.None);
                            if (!ContainsSubtree(entity.EntityValue, matchedEntity.EntityValue))
                            {
                                yield return FalseNegative(
                                    testInput.UtteranceId,
                                    ComparisonTargetKind.EntityValue,
                                    expectedUtterance,
                                    actualUtterance,
                                    score,
                                    entity.EntityType,
                                    new[] { entity.EntityType, formattedEntityValue, text },
                                    $"Actual utterance does not have entity value matching '{formattedEntityValue}'.",
                                    "Entity");
                            }
                            else
                            {
                                yield return TruePositive(
                                    testInput.UtteranceId,
                                    ComparisonTargetKind.EntityValue,
                                    expectedUtterance,
                                    actualUtterance,
                                    score,
                                    entity.EntityType,
                                    new[] { entity.EntityType, formattedEntityValue, text },
                                    $"Both utterances have entity value '{formattedEntityValue}'.",
                                    "Entity");
                            }
                        }
                    }
                }
            }

            if (actual != null)
            {
                foreach (var entity in actual)
                {
                    var score = entity.GetScore();
                    var entityValue = entity.MatchText ?? entity.EntityValue;
                    var isStrictEntity = IsStrictEntity(entity.EntityType, expectedUtterance, testInput.TestSettings);
                    if (isStrictEntity && (expected == null || !expected.Any(expectedEntity => isEntityMatch(expectedEntity, entity))))
                    {
                        yield return FalsePositive(
                            testInput.UtteranceId,
                            ComparisonTargetKind.Entity,
                            expectedUtterance,
                            actualUtterance,
                            score,
                            entity.EntityType,
                            new[] { entity.EntityType, entityValue.ToString(Formatting.None), text },
                            $"Expected utterance does not have entity matching '{entityValue}'.",
                            "Entity");
                    }
                }
            }
        }

        private static bool IsStrictEntity(
            string entityType,
            ILabeledUtterance expectedUtterance,
            TestSettings testSettings)
        {
            var localIgnoreEntities = expectedUtterance.GetIgnoreEntities();
            var localStrictEntities = expectedUtterance.GetStrictEntities();

            if (!testSettings.UnitTestMode)
            {
                var globalIgnoreEntities = testSettings.IgnoreEntities;
                return !localIgnoreEntities
                    .Union(globalIgnoreEntities)
                    .Except(localStrictEntities)
                    .Contains(entityType);
            }

            var globalStrictEntities = testSettings.StrictEntities;
            return localStrictEntities
                .Union(globalStrictEntities)
                .Except(localIgnoreEntities)
                .Contains(entityType);
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
            var expectedPath = TestContext.Parameters.Get(ConfigurationConstants.ExpectedUtterancesPathKey) ?? "expected.json";
            var actualPath = TestContext.Parameters.Get(ConfigurationConstants.ActualUtterancesPathKey) ?? "actual.json";

            if (string.IsNullOrEmpty(expectedPath) || string.IsNullOrEmpty(actualPath))
            {
                throw new InvalidOperationException("Could not find configuration for expected or actual utterances.");
            }

            var testSettingsPath = TestContext.Parameters.Get(ConfigurationConstants.TestSettingsPathKey);
            var unitTestModeString = TestContext.Parameters.Get(ConfigurationConstants.UnitTestModeKey);
            if (unitTestModeString == null || !bool.TryParse(unitTestModeString, out var unitTestMode))
            {
                unitTestMode = false;
            }

            var testSettings = new TestSettings(testSettingsPath, unitTestMode);
            var expected = Read(expectedPath);
            var actual = Read(actualPath);
            return GetNLUCompareResults(expected, actual, testSettings).TestCases;
        }

        private static List<JsonLabeledUtterance> Read(string path)
        {
            var serializer = JsonSerializer.CreateDefault();
            serializer.Converters.Add(new LabeledUtteranceConverter());
            serializer.Converters.Add(new JsonLabeledUtteranceConverter());
            using (var jsonReader = new JsonTextReader(File.OpenText(path)))
            {
                return serializer.Deserialize<List<JsonLabeledUtterance>>(jsonReader);
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
            string utteranceId,
            ComparisonTargetKind targetKind,
            ILabeledUtterance expectedUtterance,
            ILabeledUtterance actualUtterance,
            double? score,
            string group,
            string[] args,
            string because,
            params string[] categories)
        {
            return CreateTestCase(
                utteranceId,
                ConfusionMatrixResultKind.TruePositive,
                targetKind,
                expectedUtterance,
                actualUtterance,
                score,
                group,
                args,
                because,
                categories.Append("TruePositive"));
        }

        private static TestCase TrueNegative(
            string utteranceId,
            ComparisonTargetKind targetKind,
            ILabeledUtterance expectedUtterance,
            ILabeledUtterance actualUtterance,
            double? score,
            string group,
            string[] args,
            string because,
            params string[] categories)
        {
            return CreateTestCase(
                utteranceId,
                ConfusionMatrixResultKind.TrueNegative,
                targetKind,
                expectedUtterance,
                actualUtterance,
                score,
                group,
                args,
                because,
                categories.Append("TrueNegative"));
        }

        private static TestCase FalsePositive(
            string utteranceId,
            ComparisonTargetKind targetKind,
            ILabeledUtterance expectedUtterance,
            ILabeledUtterance actualUtterance,
            double? score,
            string group,
            string[] args,
            string because,
            params string[] categories)
        {
            return CreateTestCase(
                utteranceId,
                ConfusionMatrixResultKind.FalsePositive,
                targetKind,
                expectedUtterance,
                actualUtterance,
                score,
                group,
                args,
                because,
                categories.Append("FalsePositive"));
        }

        private static TestCase FalseNegative(
            string utteranceId,
            ComparisonTargetKind targetKind,
            ILabeledUtterance expectedUtterance,
            ILabeledUtterance actualUtterance,
            double? score,
            string group,
            string[] args,
            string because,
            params string[] categories)
        {
            return CreateTestCase(
                utteranceId,
                ConfusionMatrixResultKind.FalseNegative,
                targetKind,
                expectedUtterance,
                actualUtterance,
                score,
                group,
                args,
                because,
                categories.Append("FalseNegative"));
        }

        private static TestCase CreateTestCase(
            string utteranceId,
            ConfusionMatrixResultKind resultKind,
            ComparisonTargetKind targetKind,
            ILabeledUtterance expectedUtterance,
            ILabeledUtterance actualUtterance,
            double? score,
            string group,
            string[] args,
            string because,
            IEnumerable<string> categories)
        {
            var categoriesWithGroup = categories;
            if (group != null)
            {
                categoriesWithGroup.Append(group);
            }

            return new TestCase(
                utteranceId,
                resultKind,
                targetKind,
                expectedUtterance,
                actualUtterance,
                score,
                group,
                $"{resultKind}{targetKind}('{string.Join("', '", args)}')",
                because,
                categoriesWithGroup);
        }

        internal class LabeledUtteranceTestInput
        {
            public LabeledUtteranceTestInput(
                string utteranceId,
                ILabeledUtterance expected,
                ILabeledUtterance actual,
                TestSettings testSettings)
            {
                this.UtteranceId = utteranceId;
                this.Expected = expected;
                this.Actual = actual;
                this.TestSettings = testSettings;
            }

            public string UtteranceId { get; }

            public ILabeledUtterance Expected { get; }

            public ILabeledUtterance Actual { get; }

            public TestSettings TestSettings { get; }
        }
    }
}
