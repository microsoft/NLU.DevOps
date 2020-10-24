// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace NLU.DevOps.ModelPerformance.Tests
{
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
    }
}
