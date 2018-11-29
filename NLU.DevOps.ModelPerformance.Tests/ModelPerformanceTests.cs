// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace NLU.DevOps.ModelPerformance.Tests
{
    using System.Linq;
    using FluentAssertions;
    using Models;
    using NUnit.Framework;

    [TestFixture]
    internal static class ModelPerformanceTests
    {
        [Test]
        [Category("Intent")]
        [Category("Performance")]
        [TestCaseSource(typeof(ModelPerformanceTestCaseSource), "TestCases")]
        public static void CompareIntent(LabeledUtteranceTestCaseData testCaseData)
        {
            var actual = testCaseData.ActualUtterance;
            var expected = testCaseData.ExpectedUtterance;
            actual.Intent.Should().Be(expected.Intent);
        }

        [Test]
        [Category("Text")]
        [Category("Performance")]
        [TestCaseSource(typeof(ModelPerformanceTestCaseSource), "TestCases")]
        public static void CompareText(LabeledUtteranceTestCaseData testCaseData)
        {
            var actual = testCaseData.ActualUtterance;
            var expected = testCaseData.ExpectedUtterance;
            actual.Text.Should().Be(expected.Text);
        }

        [Test]
        [Category("Entities")]
        [Category("Performance")]
        [TestCaseSource(typeof(ModelPerformanceTestCaseSource), "ExpectedEntityTestCases")]
        public static void CompareExpectedEntity(EntityTestCaseData testCaseData)
        {
            var actual = testCaseData.ActualUtterance;
            var expectedEntity = testCaseData.ExpectedEntity;
            actual.Entities.Any(actualEntity => IsEntityMatch(expectedEntity, actualEntity)).Should().BeTrue();
        }

        [Test]
        [Category("Entities")]
        [Category("Performance")]
        [TestCaseSource(typeof(ModelPerformanceTestCaseSource), "ExpectedEntityValueTestCases")]
        public static void CompareExpectedEntityValue(EntityTestCaseData testCaseData)
        {
            var actual = testCaseData.ActualUtterance;
            var expectedEntity = testCaseData.ExpectedEntity;
            actual.Entities.Any(actualEntity => expectedEntity.EntityValue == actualEntity.EntityValue).Should().BeTrue();
        }

        [Test]
        [Category("Entities")]
        [Category("Performance")]
        [TestCaseSource(typeof(ModelPerformanceTestCaseSource), "ActualEntityTestCases")]
        public static void CompareExtractedEntity(EntityTestCaseData testCaseData)
        {
            var actual = testCaseData.ActualUtterance;
            var expectedEntity = testCaseData.ExpectedEntity;
            actual.Entities.Any(actualEntity => IsEntityMatch(expectedEntity, actualEntity)).Should().BeTrue();
        }

        private static bool IsEntityMatch(Entity expected, Entity actual)
        {
            return string.Compare(expected.MatchText, actual.MatchText, System.StringComparison.OrdinalIgnoreCase) == 0
                || string.Compare(expected.MatchText, actual.EntityValue, System.StringComparison.OrdinalIgnoreCase) == 0
                || string.Compare(expected.EntityValue, actual.EntityValue, System.StringComparison.OrdinalIgnoreCase) == 0
                || string.Compare(expected.EntityValue, actual.MatchText, System.StringComparison.OrdinalIgnoreCase) == 0;
        }
    }
}
