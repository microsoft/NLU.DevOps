// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace NLU.DevOps.ModelPerformance
{
    using ConsoleTables;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// This class contains utils function for performance calculations
    /// and printing
    /// </summary>
    public static class NLUAccuracy
    {
        /// <summary>
        /// Calculates the precision from a confusion matrix
        /// </summary>
        /// <param name="cm">confusion matrix metrics</param>
        /// <returns>The precision result</returns>
        public static double Precision(this ConfusionMatrix cm)
        {
            return Divide(cm.TruePositive, cm.TruePositive + cm.FalsePositive);
        }

        /// <summary>
        ///  Calculates the recall from a confusion matrix
        /// </summary>
        /// <param name="cm"> confusin matrix metrics</param>
        /// <returns> The recall result</returns>
        public static double Recall(this ConfusionMatrix cm)
        {
            return Divide(cm.TruePositive, cm.TruePositive + cm.FalseNegative);
        }

        /// <summary>
        /// Calculates the f1 score from a confusion matrix
        /// </summary>
        /// <param name="cm"> confusion matrix metrics</param>
        /// <returns> the f1 result</returns>
        public static double F1(ConfusionMatrix cm)
        {
            var precision = cm.Precision();
            var recall = cm.Recall();
            var denominator = precision + recall;
            return denominator != 0 ? 2 * (precision * recall) / denominator : 0;
        }

        /// <summary>
        /// Calculates Precision, Recall and F1 Score
        /// </summary>
        /// <param name="cm">confusion matrix metrics</param>
        /// <returns>List that contains the results calculated</returns>
        public static List<double> CalcMetrics(ConfusionMatrix cm)
        {
            List<double> metrics = new List<double>
            {
                Precision(cm),
                Recall(cm),
                F1(cm)
            };
            return metrics;
        }

        /// <summary>
        /// Prints to the console the intents and entities performance results in a table
        /// </summary>
        /// <param name="statistics"> The computed data for intents and entities</param>
        public static void PrintResults(this NLUStatistics statistics)
        {
            const int roundingplace = 4;

            Console.WriteLine("== Intents results == ");
            var intentTable = new ConsoleTable("Intent", "Precision", "Recall", "F1");
            var intentsTotalResults = NLUAccuracy.CalcMetrics(statistics.Intent).Select(intent => Math.Round(intent, roundingplace)).ToList();
            intentTable.AddRow("*", intentsTotalResults[0], intentsTotalResults[1], intentsTotalResults[1]);

            foreach (KeyValuePair<string, ConfusionMatrix> kvp in statistics.ByIntent)
            {
                // Calculating accuracy and rounding up the result values
                var results = NLUAccuracy.CalcMetrics(kvp.Value).Select(value => value = Math.Round(value, roundingplace)).ToList();
                intentTable.AddRow(kvp.Key, results[0], results[1], results[2]);
            }

            intentTable.Write();
            Console.WriteLine();

            Console.WriteLine("== Entity results == ");
            var entityTable = new ConsoleTable("Entity", "Precision", "Recall", "F1");
            var entityTotalResults = NLUAccuracy.CalcMetrics(statistics.Entity).Select(value => value = Math.Round(value, roundingplace)).ToList();
            entityTable.AddRow("*", entityTotalResults[0], entityTotalResults[1], entityTotalResults[2]);

            foreach (KeyValuePair<string, ConfusionMatrix> kvp in statistics.ByEntityType)
            {
                var entityResults = NLUAccuracy.CalcMetrics(kvp.Value).Select(value => value = Math.Round(value, roundingplace)).ToList();
                entityTable.AddRow(kvp.Key, entityResults[0], entityResults[1], entityResults[2]);
            }

            entityTable.Write();
        }

        /// <summary>
        /// Divides the dividend input by the divisor
        /// </summary>
        /// <param name="dividend"> The dividend in the division</param>
        /// <param name="divisor"> The divisor in the division</param>
        /// <returns>The division result</returns>
        private static double Divide(double dividend, double divisor)
        {
            return divisor != 0 ? dividend / divisor : 0;
        }
    }
}
