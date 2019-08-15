// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace NLU.DevOps.ModelPerformance
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// NLU compare results.
    /// </summary>
    public class NLUCompareResults
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NLUCompareResults"/> class.
        /// </summary>
        /// <param name="testCases">Test cases.</param>
        public NLUCompareResults(IReadOnlyList<TestCase> testCases)
        {
            this.TestCases = testCases;
            this.LazyStatistics = new Lazy<NLUStatistics>(this.CalculateStatistics);
        }

        /// <summary>
        /// Gets the test cases.
        /// </summary>
        public IReadOnlyList<TestCase> TestCases { get; }

        /// <summary>
        /// Gets the test case statistics.
        /// </summary>
        public NLUStatistics Statistics => this.LazyStatistics.Value;

        private Lazy<NLUStatistics> LazyStatistics { get; }

        private static ConfusionMatrix CalculateConfusionMatrix(IEnumerable<TestCase> testCases)
        {
            var groups = testCases
                .GroupBy(testCase => testCase.ResultKind)
                .ToDictionary(
                    group => group.Key,
                    group => group.Count());

            return new ConfusionMatrix(
                GetValueOrDefault(groups, ConfusionMatrixResultKind.TruePositive),
                GetValueOrDefault(groups, ConfusionMatrixResultKind.TrueNegative),
                GetValueOrDefault(groups, ConfusionMatrixResultKind.FalsePositive),
                GetValueOrDefault(groups, ConfusionMatrixResultKind.FalseNegative));
        }

        private static TValue GetValueOrDefault<TKey, TValue>(IReadOnlyDictionary<TKey, TValue> dictionary, TKey key)
        {
            return dictionary.TryGetValue(key, out var value)
                ? value
                : default(TValue);
        }

        private NLUStatistics CalculateStatistics()
        {
            return new NLUStatistics(
                CalculateConfusionMatrix(this.TestCases.Where(testCase => testCase.TargetKind == ComparisonTargetKind.Text)),
                CalculateConfusionMatrix(this.TestCases.Where(testCase => testCase.TargetKind == ComparisonTargetKind.Intent)),
                CalculateConfusionMatrix(this.TestCases.Where(testCase => testCase.TargetKind == ComparisonTargetKind.Entity)),
                CalculateConfusionMatrix(this.TestCases.Where(testCase => testCase.TargetKind == ComparisonTargetKind.EntityValue)),
                this.TestCases
                    .Where(testCase => testCase.Group != null)
                    .Where(testCase => testCase.TargetKind == ComparisonTargetKind.Intent)
                    .GroupBy(testCase => testCase.Group)
                    .ToDictionary(group => group.Key, group => CalculateConfusionMatrix(group)),
                this.TestCases
                    .Where(testCase => testCase.Group != null)
                    .Where(testCase => testCase.TargetKind == ComparisonTargetKind.Entity)
                    .GroupBy(testCase => testCase.Group)
                    .ToDictionary(group => group.Key, group => CalculateConfusionMatrix(group)),
                this.TestCases
                    .Where(testCase => testCase.Group != null)
                    .Where(testCase => testCase.TargetKind == ComparisonTargetKind.EntityValue)
                    .GroupBy(testCase => testCase.Group)
                    .ToDictionary(group => group.Key, group => CalculateConfusionMatrix(group)));
        }
    }
}
