// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace LanguageUnderstanding.ModelPerformance.Tests
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
        [TestCaseSource(typeof(LabeledUtteranceTestCaseSource), "TestCases")]
        public static void CompareIntents(LabeledUtteranceTestCaseData testCaseData)
        {
            var actual = testCaseData.ActualUtterance;
            var expected = testCaseData.ExpectedUtterance;
            actual.Intent.Should().Be(expected.Intent);
        }

        [Test]
        [Category("Text")]
        [Category("Performance")]
        [TestCaseSource(typeof(LabeledUtteranceTestCaseSource), "TestCases")]
        public static void CompareText(LabeledUtteranceTestCaseData testCaseData)
        {
            var actual = testCaseData.ActualUtterance;
            var expected = testCaseData.ExpectedUtterance;
            actual.Text.Should().Be(expected.Text);
        }

        [Test]
        [Category("Entities")]
        [Category("Performance")]
        [TestCaseSource(typeof(LabeledUtteranceTestCaseSource), "TestCases")]
        public static void CompareEntities(LabeledUtteranceTestCaseData testCaseData)
        {
            var actual = testCaseData.ActualUtterance;
            var expected = testCaseData.ExpectedUtterance;

            if (expected.Entities == null)
            {
                actual.Entities.Should().BeNull();
                return;
            }

            actual.Entities.Count.Should().Be(expected.Entities.Count);
            expected.Entities
                .All(expectedEntity => actual.Entities.Any(actualEntity => IsEntityMatch(expectedEntity, actualEntity)))
                .Should().BeTrue();
        }

        [Test]
        [Category("Entities")]
        [Category("Performance")]
        [TestCaseSource(typeof(LabeledUtteranceTestCaseSource), "EntityTestCases")]
        public static void CompareEntitiesOfType(string entityType, LabeledUtteranceTestCaseData testCaseData)
        {
            var actual = testCaseData.ActualUtterance;
            var expected = testCaseData.ExpectedUtterance;

            expected.Entities
                .Where(entity => entity.EntityType == entityType)
                .All(expectedEntity => actual.Entities.Any(actualEntity => IsEntityMatch(expectedEntity, actualEntity)))
                .Should().BeTrue();
        }

        private static bool IsEntityMatch(Entity expected, Entity actual)
        {
            var expectedEntityValue = expected.EntityValue ?? expected.MatchText;
            var actualEntityValue = actual.EntityValue ?? actual.MatchText;
            return expected.EntityType == actual.EntityType && expectedEntityValue == actualEntityValue;
        }
    }
}
