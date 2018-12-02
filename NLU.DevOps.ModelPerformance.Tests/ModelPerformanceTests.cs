// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace NLU.DevOps.ModelPerformance.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Text.RegularExpressions;
    using FluentAssertions;
    using Models;
    using NUnit.Framework;

    [TestFixture]
    internal static class ModelPerformanceTests
    {
        private static IEqualityComparer<string> UtteranceComparer { get; } = new UtteranceEqualityComparer();

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
            actual.Text.WithComparer(UtteranceComparer).Should().Be(expected.Text);
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
            return UtteranceComparer.Equals(expected.MatchText, actual.MatchText)
                || UtteranceComparer.Equals(expected.MatchText, actual.EntityValue)
                || UtteranceComparer.Equals(expected.EntityValue, actual.EntityValue)
                || UtteranceComparer.Equals(expected.EntityValue, actual.MatchText);
        }

        private class UtteranceEqualityComparer : IEqualityComparer<string>
        {
            public bool Equals(string x, string y)
            {
                return string.Equals(Normalize(x), Normalize(y), StringComparison.OrdinalIgnoreCase);
            }

            public int GetHashCode(string obj)
            {
                throw new NotImplementedException();
            }

            private static string Normalize(string s)
            {
                if (s == null)
                {
                    return null;
                }

                var normalizedSpace = Regex.Replace(s, @"\s+", " ");
                var withoutPunctuation = Regex.Replace(normalizedSpace, @"[^\w ]", string.Empty);
                return withoutPunctuation.Trim();
            }
        }
    }
}
