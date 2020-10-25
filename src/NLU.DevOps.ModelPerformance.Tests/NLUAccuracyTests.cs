// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace NLU.DevOps.ModelPerformance.Tests
{
    using System.Collections.Generic;
    using FluentAssertions;
    using NUnit.Framework;

    [TestFixture]
    internal static class NLUAccuracyTests
    {
        [Test]
        public static void AccuracyWithAllZeroes()
        {
            var matrix = new ConfusionMatrix(0, 0, 0, 0);
            matrix.Precision().Should().Be(0);
            matrix.Recall().Should().Be(0);
            matrix.F1().Should().Be(0);
            matrix.Total().Should().Be(0);
        }

        [Test]
        public static void AccuracyWithValues()
        {
            var matrix = new ConfusionMatrix(13, 0, 10, 45);
            matrix.Precision().Should().BeApproximately(0.5652, 0.0001);
            matrix.Recall().Should().BeApproximately(0.2241, 0.0001);
            matrix.F1().Should().BeApproximately(0.3210, 0.0001);
            matrix.FScore(2).Should().BeApproximately(0.2549, 0.0001);
            matrix.Total().Should().Be(68);
        }

        [Test]
        public static void GetPrecisionMetric()
        {
            var matrix = new ConfusionMatrix(1, 0, 1, 0);
            matrix.GetMetric("precision").Should().Be(matrix.Precision());
        }

        [Test]
        public static void GetRecallMetric()
        {
            var matrix = new ConfusionMatrix(1, 0, 1, 0);
            matrix.GetMetric("recall").Should().Be(matrix.Recall());
        }

        [Test]
        public static void GetFMeasureMetric()
        {
            var matrix = new ConfusionMatrix(1, 0, 1, 0);
            matrix.GetMetric("f2").Should().Be(matrix.FScore(2));
        }

        [Test]
        public static void GetFMeasureMetricDecimal()
        {
            var matrix = new ConfusionMatrix(1, 0, 1, 0);
            matrix.GetMetric("f0.5").Should().Be(matrix.FScore(0.5));
        }

        [Test]
        public static void GetDefaultMetricAsF1()
        {
            var matrix = new ConfusionMatrix(1, 0, 1, 0);
            matrix.GetMetric(null).Should().Be(matrix.F1());
        }

        [Test]
        public static void CheckThresholdPass()
        {
            var threshold = new NLUThreshold
            {
                Type = "intent",
            };

            var current = GetStatistics(new ConfusionMatrix(1, 0, 1, 0));
            var baseline = GetStatistics(new ConfusionMatrix(0, 0, 2, 0));
            NLUAccuracy.CheckThreshold(current, baseline, threshold).Should().BeTrue();
        }

        [Test]
        public static void CheckThresholdFail()
        {
            var threshold = new NLUThreshold
            {
                Type = "intent",
            };

            var current = GetStatistics(new ConfusionMatrix(0, 0, 2, 0));
            var baseline = GetStatistics(new ConfusionMatrix(1, 0, 1, 0));
            NLUAccuracy.CheckThreshold(current, baseline, threshold).Should().BeFalse();
        }

        [Test]
        public static void CheckThresholdAbsolutePass()
        {
            var threshold = new NLUThreshold
            {
                Type = "intent",
                Threshold = 0.5,
                Comparison = NLUThresholdKind.Absolute,
            };

            var current = GetStatistics(new ConfusionMatrix(1, 0, 0, 0));
            NLUAccuracy.CheckThreshold(current, null, threshold).Should().BeTrue();
        }

        [Test]
        public static void CheckThresholdAbsoluteFail()
        {
            var threshold = new NLUThreshold
            {
                Type = "intent",
                Threshold = 0.5,
                Comparison = NLUThresholdKind.Absolute,
            };

            var current = GetStatistics(new ConfusionMatrix(0, 0, 1, 0));
            NLUAccuracy.CheckThreshold(current, null, threshold).Should().BeFalse();
        }

        private static NLUStatistics GetStatistics(ConfusionMatrix intent)
        {
            return new NLUStatistics(
                ConfusionMatrix.Default,
                intent,
                ConfusionMatrix.Default,
                ConfusionMatrix.Default,
                new Dictionary<string, ConfusionMatrix>(),
                new Dictionary<string, ConfusionMatrix>(),
                new Dictionary<string, ConfusionMatrix>());
        }
    }
}
