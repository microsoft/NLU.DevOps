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
            matrix.Total().Should().Be(68);
        }
    }
}
