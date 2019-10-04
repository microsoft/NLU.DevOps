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
        /// Gets or sets a flag signaling whether text comparison tests should be run.
        /// </summary>
        public static bool? ShouldCompareText { get; set; }

        /// <summary>
        /// Gets or sets a flag signaling whether inline scripts should be evaluated.
        /// </summary>
        public static bool? ShouldEvaluate { get; set; }

        /// <summary>
        /// Gets or sets the test label value.
        /// </summary>
        public static string TestLabel { get; set; }

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

        private static bool ShouldCompareTextSetting =>
            GetConfigurationBoolean(ConfigurationConstants.CompareTextKey, ShouldCompareText);

        private static bool ShouldEvaluateSetting =>
            GetConfigurationBoolean(ConfigurationConstants.EvaluateKey, ShouldEvaluate);

        private static string TestLabelSetting =>
            GetConfiguration(ConfigurationConstants.TestLabelKey, TestLabel);

        private static IConfiguration Configuration { get; } = new ConfigurationBuilder()
            .AddJsonFile(AppSettingsPath, true)
            .AddJsonFile(AppSettingsLocalPath, true)
            .AddEnvironmentVariables()
            .Build();

        private static Lazy<IReadOnlyList<TestCase>> LazyTestCases { get; } =
            new Lazy<IReadOnlyList<TestCase>>(LoadTestCases);

        /// <summary>
        /// Generates the test cases.
        /// </summary>
        /// <returns>The test cases.</returns>
        /// <param name="expectedUtterances">Expected utterances.</param>
        /// <param name="actualUtterances">Actual utterances.</param>
        public static NLUCompareResults GetNLUCompareResults(
            IReadOnlyList<LabeledUtterance> expectedUtterances,
            IReadOnlyList<LabeledUtterance> actualUtterances)
        {
            if (expectedUtterances.Count != actualUtterances.Count)
            {
                throw new InvalidOperationException("Expected the same number of utterances in the expected and actual sources.");
            }

            string getUtteranceId(LabeledUtterance utterance, int index)
            {
                var compareUtterance = utterance as CompareLabeledUtterance;
                return compareUtterance?.UtteranceId ?? index.ToString(CultureInfo.InvariantCulture);
            }

            var zippedUtterances = expectedUtterances
                .Select((utterance, i) => new { Utterance = utterance, UtteranceId = getUtteranceId(utterance, i) })
                .Zip(actualUtterances, (expected, actual) => new LabeledUtterancePair(expected.UtteranceId, expected.Utterance, actual))
                .ToList();

            var testCases = zippedUtterances.Select(ToIntentTestCase)
                .Concat(zippedUtterances.SelectMany(ToEntityTestCases));

            if (ShouldCompareTextSetting)
            {
                testCases = testCases.Concat(zippedUtterances.Select(ToTextTestCase));
            }

            return new NLUCompareResults(testCases.ToList());
        }

        internal static TestCase ToTextTestCase(LabeledUtterancePair pair)
        {
            var expectedUtterance = pair.Expected;
            var actualUtterance = pair.Actual;
            var expected = expectedUtterance.Text;
            var actual = actualUtterance.Text;
            var score = actualUtterance is PredictedLabeledUtterance scoredUtterance
                ? scoredUtterance.TextScore
                : 0;

            if (expected == null && actual == null)
            {
                return TrueNegative(
                    pair.UtteranceId,
                    ComparisonTargetKind.Text,
                    expectedUtterance,
                    actualUtterance,
                    score,
                    null,
                    Array.Empty<string>(),
                    "Both utterances are 'null'.",
                    "Text");
            }

            if (actual == null)
            {
                return FalseNegative(
                    pair.UtteranceId,
                    ComparisonTargetKind.Text,
                    expectedUtterance,
                    actualUtterance,
                    score,
                    null,
                    new[] { expected },
                    $"Actual text is 'null', expected '{expected}'",
                    "Text");
            }

            if (EqualsNormalized(expected, actual))
            {
                return TruePositive(
                    pair.UtteranceId,
                    ComparisonTargetKind.Text,
                    expectedUtterance,
                    actualUtterance,
                    score,
                    null,
                    new[] { expected },
                    "Utterances have matching text.",
                    "Text");
            }

            return FalsePositive(
                pair.UtteranceId,
                ComparisonTargetKind.Text,
                expectedUtterance,
                actualUtterance,
                score,
                null,
                new[] { expected, actual },
                $"Expected text '{expected}', actual text '{actual}'.",
                "Text");
        }

        internal static TestCase ToIntentTestCase(LabeledUtterancePair pair)
        {
            var expectedUtterance = pair.Expected;
            var actualUtterance = pair.Actual;
            var score = actualUtterance is PredictedLabeledUtterance scoredUtterance
                ? scoredUtterance.Score
                : 0;

            var text = expectedUtterance.Text;
            var expected = expectedUtterance.Intent;
            var actual = actualUtterance.Intent;

            bool isNoneIntent(string intent)
            {
                return intent == null || intent == "None";
            }

            if (isNoneIntent(actual))
            {
                return isNoneIntent(expected)
                    ? TrueNegative(
                        pair.UtteranceId,
                        ComparisonTargetKind.Intent,
                        expectedUtterance,
                        actualUtterance,
                        score,
                        null,
                        new[] { text },
                        "Both intents are 'None'.",
                        "Intent")
                    : FalseNegative(
                        pair.UtteranceId,
                        ComparisonTargetKind.Intent,
                        expectedUtterance,
                        actualUtterance,
                        score,
                        expected,
                        new[] { expected, text },
                        $"Actual intent is 'None', expected '{expected}'",
                        "Intent");
            }

            if (expected == actual)
            {
                return TruePositive(
                    pair.UtteranceId,
                    ComparisonTargetKind.Intent,
                    expectedUtterance,
                    actualUtterance,
                    score,
                    expected,
                    new[] { expected, text },
                    "Utterances have matching intent.",
                    "Intent");
            }

            return FalsePositive(
                pair.UtteranceId,
                ComparisonTargetKind.Intent,
                expectedUtterance,
                actualUtterance,
                score,
                isNoneIntent(expected) ? actual : expected,
                new[] { expected, actual, text },
                $"Expected intent '{expected}', actual intent '{actual}'.",
                "Intent");
        }

        internal static IEnumerable<TestCase> ToEntityTestCases(LabeledUtterancePair pair)
        {
            var expectedUtterance = pair.Expected;
            var actualUtterance = pair.Actual;
            var text = expectedUtterance.Text;
            var expected = expectedUtterance.Entities;
            var actual = actualUtterance.Entities;
            var globals = actualUtterance is PredictedLabeledUtterance predictedUtterance
                ? predictedUtterance.Context
                : null;

            if ((expected == null || expected.Count == 0) && (actual == null || actual.Count == 0))
            {
                yield return TrueNegative(
                    pair.UtteranceId,
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

            bool isEntityMatch(Entity expectedEntity, Entity actualEntity)
            {
                return expectedEntity.EntityType == actualEntity.EntityType
                    && (isEntityTextMatch(expectedEntity, actualEntity)
                    || isEntityValueMatch(expectedEntity, actualEntity));
            }

            bool isEntityTextMatch(Entity expectedEntity, Entity actualEntity)
            {
                return expectedEntity.MatchText != null
                    && EqualsNormalized(expectedEntity.MatchText, actualEntity.MatchText)
                    && expectedEntity.MatchIndex == actualEntity.MatchIndex;
            }

            bool isEntityValueMatch(Entity expectedEntity, Entity actualEntity)
            {
                /* Required case to support NLU providers that do not specify matched text */
                return actualEntity.MatchText == null
                    && (Evaluate(expectedEntity.EntityValue, globals).ContainsSubtree(actualEntity.EntityValue)
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

                    var score = matchedEntity is PredictedEntity scoredEntity
                        ? scoredEntity.Score
                        : 0;

                    if (matchedEntity == null)
                    {
                        yield return FalseNegative(
                            pair.UtteranceId,
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
                            pair.UtteranceId,
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
                            if (!Evaluate(entity.EntityValue, globals).ContainsSubtree(matchedEntity.EntityValue))
                            {
                                yield return FalseNegative(
                                    pair.UtteranceId,
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
                                    pair.UtteranceId,
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
                    var score = entity is PredictedEntity scoredEntity ? scoredEntity.Score : 0;
                    var entityValue = entity.MatchText ?? entity.EntityValue;
                    if (expected == null || !expected.Any(expectedEntity => isEntityMatch(expectedEntity, entity)))
                    {
                        yield return FalsePositive(
                            pair.UtteranceId,
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

            var expected = Read<CompareLabeledUtterance>(expectedPath);
            var actual = Read<PredictedLabeledUtterance>(actualPath);
            return GetNLUCompareResults(expected, actual).TestCases;
        }

        private static List<T> Read<T>(string path)
        {
            var serializer = JsonSerializer.CreateDefault();
            serializer.Converters.Add(new LabeledUtteranceConverter());
            serializer.DateParseHandling = DateParseHandling.None;
            using (var jsonReader = new JsonTextReader(File.OpenText(path)))
            {
                return serializer.Deserialize<List<T>>(jsonReader);
            }
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

        private static JToken Evaluate(JToken token, object globals)
        {
            return ShouldEvaluateSetting ? token.Evaluate(globals) : token;
        }

        private static bool GetConfigurationBoolean(string key, bool? overrideValue)
        {
            return overrideValue ?? (bool.TryParse(GetConfiguration(key, null), out var flag) ? flag : false);
        }

        private static string GetConfiguration(string key, string overrideValue)
        {
            return overrideValue ?? TestContext.Parameters.Get(key) ?? Configuration[key];
        }

        private static TestCase TruePositive(
            string utteranceId,
            ComparisonTargetKind targetKind,
            LabeledUtterance expectedUtterance,
            LabeledUtterance actualUtterance,
            double score,
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
            LabeledUtterance expectedUtterance,
            LabeledUtterance actualUtterance,
            double score,
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
            LabeledUtterance expectedUtterance,
            LabeledUtterance actualUtterance,
            double score,
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
            LabeledUtterance expectedUtterance,
            LabeledUtterance actualUtterance,
            double score,
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
            LabeledUtterance expectedUtterance,
            LabeledUtterance actualUtterance,
            double score,
            string group,
            string[] args,
            string because,
            IEnumerable<string> categories)
        {
            var testLabel = TestLabelSetting != null ? $"[{TestLabelSetting}] " : string.Empty;
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
                $"{testLabel}{resultKind}{targetKind}('{string.Join("', '", args)}')",
                because,
                categoriesWithGroup);
        }

        internal class LabeledUtterancePair
        {
            public LabeledUtterancePair(
                string utteranceId,
                LabeledUtterance expected,
                LabeledUtterance actual)
            {
                this.UtteranceId = utteranceId;
                this.Expected = expected;
                this.Actual = actual;
            }

            public string UtteranceId { get; }

            public LabeledUtterance Expected { get; }

            public LabeledUtterance Actual { get; }
        }
    }
}
