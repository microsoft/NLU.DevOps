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
            ConfusionMatrix cm = new ConfusionMatrix(0, 0, 0, 0);

            var result = cm.CalcAccuracy();

            result.Count.Should().Be(3);
            result[0].Should().Be(0);
            result[1].Should().Be(0);
            result[2].Should().Be(0);
        }

        [Test]
        public static void AccuracyWithValues()
        {
            ConfusionMatrix cm = new ConfusionMatrix(13, 0, 10, 45);

            var result = cm.CalcAccuracy();

            result.Count.Should().Be(3);
            result[0].Should().Be(0.5652);
            result[1].Should().Be(0.2241);
            result[2].Should().Be(0.3210);
        }

        [Test]
        public static void AccuracyWithValuesWithdRoundingDecimal()
        {
            ConfusionMatrix cm = new ConfusionMatrix(13, 0, 10, 45);

            var result = cm.CalcAccuracy(5);

            result.Count.Should().Be(3);
            result[0].Should().Be(0.56522);
            result[1].Should().Be(0.22414);
            result[2].Should().Be(0.32099);
        }
    }
}
