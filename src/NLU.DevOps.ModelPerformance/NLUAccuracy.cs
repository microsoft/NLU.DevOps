// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace NLU.DevOps.ModelPerformance
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using ConsoleTables;

    /// <summary>
    /// This class contains utils function for performance calculations
    /// and printing
    /// </summary>
    public static class NLUAccuracy
    {
        /// <summary>
        /// Prints to the console the intents, entities performance results
        /// and a confusion table for intents
        /// </summary>
        /// <param name="compareResults"> The comparison results for intents and entities</param>
        /// <param name="baseline">The baseline results the results are benchmarked against.</param>
        public static void PrintResults(this NLUCompareResults compareResults, NLUStatistics baseline)
        {
            var allIntentsPrecision = Print(compareResults.Statistics.Intent.Precision(), baseline?.Intent.Precision());
            var allIntentsRecall = Print(compareResults.Statistics.Intent.Recall(), baseline?.Intent.Recall());
            var allIntentsF1 = Print(compareResults.Statistics.Intent.F1(), baseline?.Intent.F1());

            Console.WriteLine("# Intents results");

            var intentTable = new ConsoleTable("Intent", "Precision", "Recall", "F1");
            intentTable.AddRow("*", allIntentsPrecision, allIntentsRecall, allIntentsF1);

            compareResults.Statistics.ByIntent.ToList().ForEach(intentItem =>
            {
                var baselineResults = default(ConfusionMatrix);
                if (baseline != null && !baseline.ByIntent.TryGetValue(intentItem.Key, out baselineResults))
                {
                    baselineResults = ConfusionMatrix.Default;
                }

                var intentPrecision = Print(intentItem.Value.Precision(), baselineResults?.Precision());
                var intentRecall = Print(intentItem.Value.Recall(), baselineResults?.Recall());
                var intentF1 = Print(intentItem.Value.F1(), baselineResults?.F1());
                intentTable.AddRow(intentItem.Key, intentPrecision, intentRecall, intentF1);
            });

            intentTable.Write(Format.MarkDown);

            var allEntitiesPrecision = Print(compareResults.Statistics.Entity.Precision(), baseline?.Entity.Precision());
            var allEntitiesRecall = Print(compareResults.Statistics.Entity.Recall(), baseline?.Entity.Recall());
            var allEntitiesF1 = Print(compareResults.Statistics.Entity.F1(), baseline?.Entity.F1());

            Console.WriteLine("# Entity results");

            var entityTable = new ConsoleTable("Entity", "Precision", "Recall", "F1");
            entityTable.AddRow("*", allEntitiesPrecision, allEntitiesRecall, allEntitiesF1);

            compareResults.Statistics.ByEntityType.ToList().ForEach(entityItem =>
            {
                var baselineResults = default(ConfusionMatrix);
                if (baseline != null && !baseline.ByEntityType.TryGetValue(entityItem.Key, out baselineResults))
                {
                    baselineResults = ConfusionMatrix.Default;
                }

                var entityPrecision = Print(entityItem.Value.Precision(), baselineResults?.Precision());
                var entityRecall = Print(entityItem.Value.Recall(), baselineResults?.Recall());
                var entityF1 = Print(entityItem.Value.F1(), baselineResults?.F1());
                entityTable.AddRow(entityItem.Key, entityPrecision, entityRecall, entityF1);
            });

            entityTable.Write(Format.MarkDown);

            PrintIntentConfusionTable(compareResults.TestCases);
        }

        /// <summary>
        /// Calculates the precision from a confusion matrix
        /// </summary>
        /// <param name="matrix">confusion matrix metrics</param>
        /// <returns>The precision result</returns>
        internal static double Precision(this ConfusionMatrix matrix)
        {
            return Divide(matrix.TruePositive, matrix.TruePositive + matrix.FalsePositive);
        }

        /// <summary>
        ///  Calculates the recall from a confusion matrix
        /// </summary>
        /// <param name="matrix"> confusin matrix metrics</param>
        /// <returns> The recall result</returns>
        internal static double Recall(this ConfusionMatrix matrix)
        {
            return Divide(matrix.TruePositive, matrix.TruePositive + matrix.FalseNegative);
        }

        /// <summary>
        /// Calculates the f1 score from a confusion matrix
        /// </summary>
        /// <param name="matrix"> confusion matrix metrics</param>
        /// <returns> the f1 result</returns>
        internal static double F1(this ConfusionMatrix matrix)
        {
            var precision = matrix.Precision();
            var recall = matrix.Recall();
            var denominator = precision + recall;
            return Math.Abs(denominator) > double.Epsilon ? 2 * (precision * recall) / denominator : 0;
        }

        /// <summary>
        /// Divides the dividend input by the divisor
        /// </summary>
        /// <param name="dividend"> The dividend in the division</param>
        /// <param name="divisor"> The divisor in the division</param>
        /// <returns>The division result</returns>
        private static double Divide(double dividend, int divisor)
        {
            return divisor != 0 ? dividend / divisor : 0;
        }

        /// <summary>
        /// Prints the confusion table for intents
        /// </summary>
        /// <param name="testCases"> The calculated metadata results</param>
        private static void PrintIntentConfusionTable(IReadOnlyList<TestCase> testCases)
        {
            var falsePositiveIntents = testCases
                                       .Where(testCase => testCase.TargetKind == ComparisonTargetKind.Intent
                                                          && testCase.ResultKind == ConfusionMatrixResultKind.FalsePositive)
                                       .GroupBy(testCase => (testCase.ExpectedUtterance.Intent, testCase.ActualUtterance.Intent))
                                       .ToDictionary(
                                           group => group.Key,
                                           group => group.Count())
                                       .OrderByDescending(group => group.Value);

            Console.WriteLine("# Intent Confusion Matrix");

            var confusionMatrix = new ConsoleTable("Expected intent", "Actual Intent", "FP");
            falsePositiveIntents.ToList().ForEach(kvp =>
            {
                confusionMatrix.AddRow(kvp.Key.Item1, kvp.Key.Item2, kvp.Value);
            });

            confusionMatrix.Write(Format.MarkDown);
        }

        /// <summary>
        /// Prints the current value and difference from the baseline.
        /// </summary>
        /// <param name="current">Current value.</param>
        /// <param name="baseline">Baseline value.</param>
        /// <returns>Printed value and difference with baseline.</returns>
        private static string Print(double current, double? baseline)
        {
            return baseline.HasValue
                ? string.Format(CultureInfo.CurrentCulture, "{0:0.0000} ({1:0.0000})", current, current - baseline)
                : string.Format(CultureInfo.CurrentCulture, "{0:0.0000}", current);
        }
    }
}
