// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace NLU.DevOps.ModelPerformance
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using ConsoleTables;

    /// <summary>
    /// This class contains utils function for performance calculations
    /// and printing
    /// </summary>
    public static class NLUAccuracy
    {
        /// <summary>
        /// Calculates Precision, Recall and F1 Score
        /// </summary>
        /// <param name="cm">confusion matrix metrics</param>
        /// <param name="roundingplace"> Specifies the number of digits after the decimal point</param>
        /// <returns>List that contains the results calculated</returns>
        public static List<double> CalcAccuracy(this ConfusionMatrix cm, int roundingplace = 4)
        {
            var metrics = new List<double>
            {
                Math.Round(cm.Precision(), roundingplace),
                Math.Round(cm.Recall(), roundingplace),
                Math.Round(cm.F1(), roundingplace)
            };
            return metrics;
        }

        /// <summary>
        /// Prints to the console the intents, entities performance results
        /// and a confusion table for intents
        /// </summary>
        /// <param name="compareResults"> The comparison results for intents and entities</param>
        public static void PrintResults(this NLUCompareResults compareResults)
        {
            var intentAverageResults = compareResults.Statistics.Intent.CalcAccuracy();
            Console.WriteLine("== Intents results == ");
            var intentTable = new ConsoleTable("Intent", "Precision", "Recall", "F1");
            intentTable.AddRow("*", intentAverageResults[0], intentAverageResults[1], intentAverageResults[2]);

            compareResults.Statistics.ByIntent.ToList().ForEach(kvp =>
            {
                // Calculating accuracy numbers and rounding up the result values for each intent
                var results = kvp.Value.CalcAccuracy();
                intentTable.AddRow(kvp.Key, results[0], results[1], results[2]);
            });

            intentTable.Write();

            var entityAverageResults = compareResults.Statistics.Entity.CalcAccuracy();
            Console.WriteLine("== Entity results == ");
            var entityTable = new ConsoleTable("Entity", "Precision", "Recall", "F1");
            entityTable.AddRow("*", entityAverageResults[0], entityAverageResults[1], entityAverageResults[2]);

            compareResults.Statistics.ByEntityType.ToList().ForEach(kvp =>
            {
                // Calculating accuracy numbers and rounding up the result values for each entity
                var entityResults = kvp.Value.CalcAccuracy().ToList();
                entityTable.AddRow(kvp.Key, entityResults[0], entityResults[1], entityResults[2]);
            });

            entityTable.Write();

            PrintIntentConfusionTable(compareResults.TestCases);
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
        /// Calculates the precision from a confusion matrix
        /// </summary>
        /// <param name="cm">confusion matrix metrics</param>
        /// <returns>The precision result</returns>
        private static double Precision(this ConfusionMatrix cm)
        {
            return Divide(cm.TruePositive, cm.TruePositive + cm.FalsePositive);
        }

        /// <summary>
        ///  Calculates the recall from a confusion matrix
        /// </summary>
        /// <param name="cm"> confusin matrix metrics</param>
        /// <returns> The recall result</returns>
        private static double Recall(this ConfusionMatrix cm)
        {
            return Divide(cm.TruePositive, cm.TruePositive + cm.FalseNegative);
        }

        /// <summary>
        /// Calculates the f1 score from a confusion matrix
        /// </summary>
        /// <param name="cm"> confusion matrix metrics</param>
        /// <returns> the f1 result</returns>
        private static double F1(this ConfusionMatrix cm)
        {
            var precision = cm.Precision();
            var recall = cm.Recall();
            var denominator = precision + recall;
            return Math.Abs(denominator) > double.Epsilon ? 2 * (precision * recall) / denominator : 0;
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

            Console.WriteLine("== Intent Confusion Matrix == ");
            var confusionMatrix = new ConsoleTable("Expected intent", "Actual Intent", "FP");
            falsePositiveIntents.ToList().ForEach(kvp =>
            {
                confusionMatrix.AddRow(kvp.Key.Item1, kvp.Key.Item2, kvp.Value);
            });

            confusionMatrix.Write();
        }
    }
}
