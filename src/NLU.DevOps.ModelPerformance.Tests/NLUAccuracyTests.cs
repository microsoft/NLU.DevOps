// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace NLU.DevOps.ModelPerformance.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using FluentAssertions;
    using NUnit.Framework;

    [TestFixture]
    internal class NLUAccuracyTests
    {
        [Test]
        public void CalculateWithZeroDivisor()
        {
            var result = NLUAccuracy.Calculate(100, 0);

            result.Should().Be(0);
        }

        [Test]
        public void CalculateNonZeroDivisor()
        {
            var result = NLUAccuracy.Calculate(20, 40);

            result.Should().Be(0.5);
        }

        [Test]
        public void PrecisionWithAllZeroes()
        {
            ConfusionMatrix cm = new ConfusionMatrix(0, 0, 0, 0);

            var result = NLUAccuracy.CalcPrecision(cm);

            result.Should().Be(0);
        }

        [Test]
        public void PrecisionWithValues()
        {
            ConfusionMatrix cm = new ConfusionMatrix(10, 0, 10, 50);

            var result = NLUAccuracy.CalcPrecision(cm);

            result.Should().Be(0.5);
        }

        [Test]
        public void RecallWithAllZeroes()
        {
            ConfusionMatrix cm = new ConfusionMatrix(0, 0, 0, 0);

            var result = NLUAccuracy.CalcRecall(cm);

            result.Should().Be(0);
        }

        [Test]
        public void RecallWithValues()
        {
            ConfusionMatrix cm = new ConfusionMatrix(10, 0, 10, 40);

            var result = NLUAccuracy.CalcRecall(cm);

            result.Should().Be(0.2);
        }

        [Test]
        public void F1WithAllZeroes()
        {
            ConfusionMatrix cm = new ConfusionMatrix(0, 0, 0, 0);

            var result = NLUAccuracy.CalcF1(cm);

            result.Should().Be(0);
        }

        [Test]
        public void F1WithValues()
        {
            ConfusionMatrix cm = new ConfusionMatrix(10, 0, 10, 40);

            var result = NLUAccuracy.CalcF1(cm);
            var roundedResult = Math.Round(result, 5);

            roundedResult.Should().Be(0.28571);
        }
    }
}
