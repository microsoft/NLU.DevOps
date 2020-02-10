﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace NLU.DevOps.ModelPerformance
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Text;
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
        /// Prints to the console the intents and entities performance results in a table
        /// </summary>
        /// <param name="statistics"> The computed data for intents and entities</param>
        public static void PrintResults(this NLUStatistics statistics)
        {
            Console.WriteLine("== Intents results == ");
            var intentTable = new ConsoleTable("Intent", "Precision", "Recall", "F1");
            var intentAverageResults = statistics.Intent.CalcAccuracy();
            intentTable.AddRow("*", intentAverageResults[0], intentAverageResults[1], intentAverageResults[1]);

            statistics.ByIntent.ToList().ForEach(kvp =>
            {
                // Calculating accuracy numbers and rounding up the result values for each intent
                var results = kvp.Value.CalcAccuracy();
                intentTable.AddRow(kvp.Key, results[0], results[1], results[2]);
            });

            intentTable.Write();
            Console.WriteLine();

            Console.WriteLine("== Entity results == ");
            var entityTable = new ConsoleTable("Entity", "Precision", "Recall", "F1");
            var entityAverageResults = statistics.Entity.CalcAccuracy();
            entityTable.AddRow("*", entityAverageResults[0], entityAverageResults[1], entityAverageResults[2]);

            statistics.ByEntityType.ToList().ForEach(kvp =>
            {
                // Calculating accuracy numbers and rounding up the result values for each entity
                var entityResults = kvp.Value.CalcAccuracy().ToList();
                entityTable.AddRow(kvp.Key, entityResults[0], entityResults[1], entityResults[2]);
            });

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
            return denominator != 0 ? 2 * (precision * recall) / denominator : 0;
        }
    }
}
