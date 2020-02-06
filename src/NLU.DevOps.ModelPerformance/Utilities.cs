// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace NLU.DevOps.ModelPerformance
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Text;
    using NLU.DevOps.ModelPerformance;

    /// <summary>
    /// This class contains utils function for performance calculations
    /// and printing
    /// </summary>
    public static class Utilities
    {
       /// <summary>
       /// Divides the dividend input by the diviso
       /// </summary>
       /// <param name="dividend"> The dividend in the division</param>
       /// <param name="divisor"> The divisor in the division</param>
       /// <returns>The division result</returns>
        public static decimal Calculate(decimal dividend, decimal divisor)
        {
            return divisor != 0 ? decimal.Divide(dividend, divisor) : 0;
        }

        /// <summary>
        /// Calculates the precision from a confusion matrix
        /// </summary>
        /// <param name="cm">confusion matrix metrics</param>
        /// <returns>The precision result</returns>
        public static decimal CalcPrecision(ConfusionMatrix cm)
        {
            return Calculate(cm.TruePositive, cm.TruePositive + cm.FalsePositive);
        }

        /// <summary>
        ///  Calculates the recall from a confusion matrix
        /// </summary>
        /// <param name="cm"> confusin matrix metrics</param>
        /// <returns> The recall result</returns>
        public static decimal CalcRecall(ConfusionMatrix cm)
        {
            return Calculate(cm.TruePositive, cm.TruePositive + cm.FalseNegative);
        }

        /// <summary>
        /// Calculates the f1 score from a confusion matrix
        /// </summary>
        /// <param name="cm"> confusion matrix metrics</param>
        /// <returns> the f1 result</returns>
        public static decimal CalcF1(ConfusionMatrix cm)
        {
            var precision = CalcPrecision(cm);
            var recall = CalcRecall(cm);
            var denominator = precision + recall;
            return denominator != 0 ? 2 * decimal.Divide(precision * recall, denominator) : 0;
        }

        /// <summary>
        /// Calculates Precision, Recall and F1 Score
        /// </summary>
        /// <param name="cm">confusion matrix metrics</param>
        /// <returns>List that contains the results calculated</returns>
        public static List<decimal> CalcMetrics(ConfusionMatrix cm)
        {
            List<decimal> metrics = new List<decimal>
            {
                CalcPrecision(cm),
                CalcRecall(cm),
                CalcF1(cm)
            };
            return metrics;
        }

        /// <summary>
        /// Prints to the console the intents and entities performance results in a table
        /// </summary>
        /// <param name="statistics"> The computed data for intents and entities</param>
        public static void PrintResults(NLUStatistics statistics)
        {
            Console.Out.WriteLine("== Intents results == ");
            Console.Out.WriteLine("Intent          | Precision | Recall    | F1        |");
            Console.Out.WriteLine("=====================================================");
            Console.Out.Write(string.Format(CultureInfo.InvariantCulture, "{0,-15} |", "*"));

            List<decimal> intentsTotalResults = Utilities.CalcMetrics(statistics.Intent);
            intentsTotalResults.ForEach(entry => Console.Out.Write(string.Format(CultureInfo.InvariantCulture, "{0,-10} |", decimal.Round(entry, 4))));
            Console.Out.WriteLine();

            foreach (KeyValuePair<string, ConfusionMatrix> kvp in statistics.ByIntent)
            {
                Console.Out.Write(string.Format(CultureInfo.InvariantCulture, "{0,-15} |", kvp.Key));
                Utilities.CalcMetrics(kvp.Value).ForEach(intent => Console.Out.Write(string.Format(CultureInfo.InvariantCulture, "{0,-10} |", decimal.Round(intent, 4))));
                Console.Out.WriteLine();
            }

            Console.Out.WriteLine();
            Console.Out.WriteLine("== Entity results == ");
            Console.Out.WriteLine("Entity            | Precision | Recall    | F1        |");
            Console.Out.WriteLine("=======================================================");
            Console.Out.Write(string.Format(CultureInfo.InvariantCulture, "{0,-17} |", "*"));

            List<decimal> entityTotalResults = Utilities.CalcMetrics(statistics.Entity);
            entityTotalResults.ForEach(entry => Console.Out.Write(string.Format(CultureInfo.InvariantCulture, "{0,-10} |", decimal.Round(entry, 4))));
            Console.Out.WriteLine();

            foreach (KeyValuePair<string, ConfusionMatrix> kvp in statistics.ByEntityType)
            {
                Console.Out.Write(string.Format(CultureInfo.InvariantCulture, "{0,-17} |", kvp.Key));
                var entityResults = Utilities.CalcMetrics(kvp.Value);
                entityResults.ForEach(entity => Console.Out.Write(string.Format(CultureInfo.InvariantCulture, "{0,-10} |", decimal.Round(entity, 4))));
                Console.Out.WriteLine();
            }
        }
    }
}
