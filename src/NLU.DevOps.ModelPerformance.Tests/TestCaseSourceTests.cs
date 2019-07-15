// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace NLU.DevOps.ModelPerformance.Tests
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using FluentAssertions;
    using Models;
    using NUnit.Framework;

    [TestFixture]
    internal static class TestCaseSourceTests
    {
        private const string FalseNegativeEntityValueRegEx = @"FalseNegativeEntityValue\(.*?\)";
        private const string TruePositiveEntityValueRegEx = @"TruePositiveEntityValue\(.*?\)";
        private const string FalseNegativeEntityRegEx = @"FalseNegativeEntity\(.*?\)";
        private const string FalsePositiveEntityRegEx = @"FalsePositiveEntity\(.*?\)";
        private const string TruePositiveEntityRegEx = @"TruePositiveEntity\(.*?\)";
        private const string FalsePositiveIntentRegEx = @"FalsePositiveIntent\(.*?\)";
        private const string FalseNegativeIntentRegEx = @"FalseNegativeIntent\(.*?\)";

        [Test]
        public static void TestToIntentTestCaseExpectingNoneActualDayTime()
        {
            var utterances = new[]
            {
                new LabeledUtterance("FOO", "None", null),
                new LabeledUtterance("FOO", "DayTime", null)
            };

            var test = TestCaseSource.ToIntentTestCase(utterances);

            test.TestName.Should().MatchRegex(FalsePositiveIntentRegEx);
            test.Kind.Should().Be(TestResultKind.FalsePositive);
        }

        [Test]
        public static void TestToIntentTestCaseActualNoneExpectingDayTime()
        {
            var utterances = new[]
            {
                new LabeledUtterance("FOO", "DayTime", null),
                new LabeledUtterance("FOO", "None", null)
            };

            var test = TestCaseSource.ToIntentTestCase(utterances);

            test.TestName.Should().MatchRegex(FalseNegativeIntentRegEx);
            test.Kind.Should().Be(TestResultKind.FalseNegative);
        }

        [Test]
        public static void TestToEntityTestCasesMissingAnActualEntity()
        {
            var entity = CreateEntityList("EntityType");
            var utterances = new[]
            {
                new LabeledUtterance("FOO", "DayTime", entity),
                new LabeledUtterance("FOO", "DayTime", null)
            };

            var actualTestResult = TestCaseSource.ToEntityTestCases(utterances);

            actualTestResult.First().Kind.Should().Be(TestResultKind.FalseNegative);
            actualTestResult.First().TestName.Should().MatchRegex(FalseNegativeEntityRegEx);
        }

        [Test]
        public static void TestToEntityTestCasesWrongEntityType()
        {
            var actualEntity = CreateEntityList("EntityType");
            var expectedEntity = CreateEntityList("WrongType");
            var utterances = new[]
            {
                 new LabeledUtterance("FOO", "DayTime", expectedEntity),
                 new LabeledUtterance("FOO", "DayTime", actualEntity)
            };

            var actualTestResult = TestCaseSource.ToEntityTestCases(utterances);

            var testResult = actualTestResult.ToList();
            testResult.Count().Should().Be(3);
            var actualFalsePositive = testResult.FirstOrDefault(t => t.Kind == TestResultKind.FalsePositive);
            var actualFalseNegative = testResult.FirstOrDefault(t => t.Kind == TestResultKind.FalseNegative);
            var actualFalseNegativeEntityValue = testResult.FirstOrDefault(t => Matches(t.Message, FalseNegativeEntityValueRegEx));
            actualFalsePositive.Should().NotBeNull();
            actualFalsePositive.TestName.Should().MatchRegex(FalsePositiveEntityRegEx);
            actualFalseNegative.Should().NotBeNull();
            actualFalseNegative.TestName.Should().MatchRegex(FalseNegativeEntityRegEx);
            actualFalseNegativeEntityValue.Should().NotBeNull();
        }

        [Test]
        public static void TestToEntityTestCasesMissingEntityInExpectedFile()
        {
            var actualEntity = CreateEntityList("EntityType");
            actualEntity.Add(new Entity("RecognizedEntity", "value", "text", 2));
            var expectedEntity = CreateEntityList("EntityType");
            var utterances = new[]
            {
                new LabeledUtterance("FOO", "DayTime", expectedEntity),
                new LabeledUtterance("FOO", "DayTime", actualEntity)
            };

            var actualTestResult = TestCaseSource.ToEntityTestCases(utterances);

            var testResult = actualTestResult.ToList();
            testResult.Count().Should().Be(3);
            var actualFalsePositive = testResult.FirstOrDefault(t => t.Kind == TestResultKind.FalsePositive);
            var actualTruePositive = testResult.FirstOrDefault(t => t.Kind == TestResultKind.TruePositive);
            var actualTruePositiveEntityValue = testResult.FirstOrDefault(t => Matches(t.Message, TruePositiveEntityValueRegEx));
            actualFalsePositive.Should().NotBeNull();
            actualFalsePositive.TestName.Should().MatchRegex(FalsePositiveEntityRegEx);
            actualTruePositive.Should().NotBeNull();
            actualTruePositive.TestName.Should().MatchRegex(TruePositiveEntityRegEx);
            actualTruePositiveEntityValue.Should().NotBeNull();
        }

        [Test]
        public static void TestToEntityTestCasesMissingEntityValueInActualResult()
        {
            var expectedEntity = CreateEntityList("EntityType");
            var utterances = new[]
            {
                new LabeledUtterance("FOO", "DayTime", expectedEntity),
                new LabeledUtterance("FOO", "DayTime", null)
            };

            var actualTestResult = TestCaseSource.ToEntityTestCases(utterances);

            var testResult = actualTestResult.ToList();
            testResult.Count().Should().Be(2);
            var actualFalseNegative = testResult.FirstOrDefault(t => t.Kind == TestResultKind.FalseNegative);
            var actualFalseNegativeEntityValue = testResult.FirstOrDefault(t => Matches(t.Message, FalseNegativeEntityValueRegEx));
            actualFalseNegative.Should().NotBeNull();
            actualFalseNegative.TestName.Should().MatchRegex(FalseNegativeEntityRegEx);
            actualFalseNegativeEntityValue.Should().NotBeNull();
        }

        [Test]
        public static void TestToEntityTestCasesWithDifferentEntityValueInActualVersusExpected()
        {
            var actualEntity = new List<Entity> { new Entity("EntityType", "differentEntityValue", "differentMatchedText", 1) };
            var expectedEntity = CreateEntityList("EntityType");
            var utterances = new[]
            {
                 new LabeledUtterance("FOO", "DayTime", expectedEntity),
                 new LabeledUtterance("FOO", "DayTime", actualEntity)
            };

            var actualTestResult = TestCaseSource.ToEntityTestCases(utterances);

            var testResult = actualTestResult.ToList();
            testResult.Count().Should().Be(3);
            var actualFalsePositive = testResult.FirstOrDefault(t => t.Kind == TestResultKind.FalsePositive);
            var actualFalseNegative = testResult.FirstOrDefault(t => t.Kind == TestResultKind.FalseNegative);
            var actualFalseNegativeEntityValue = testResult.FirstOrDefault(t => Matches(t.Message, FalseNegativeEntityValueRegEx));
            actualFalsePositive.Should().NotBeNull();
            actualFalsePositive.TestName.Should().MatchRegex(FalsePositiveEntityRegEx);
            actualFalseNegative.Should().NotBeNull();
            actualFalseNegative.TestName.Should().MatchRegex(FalseNegativeEntityRegEx);
            actualFalseNegativeEntityValue.Should().NotBeNull();
        }

        [Test]
        public static void ToTextTestCaseTrueNegative()
        {
            var utterances = new[]
            {
                new LabeledUtterance(null, "DayTime", null),
                new LabeledUtterance(null, "DayTime", null)
            };

            var actualResult = TestCaseSource.ToTextTestCase(utterances);

            actualResult.Kind.Should().Be(TestResultKind.TrueNegative);
        }

        [Test]
        public static void ToTextTestCaseFalseNegative()
         {
             var utterances = new[]
             {
                 new LabeledUtterance("foo", "DayTime", null),
                 new LabeledUtterance(null, "DayTime", null)
             };

             var actualResult = TestCaseSource.ToTextTestCase(utterances);

             actualResult.Kind.Should().Be(TestResultKind.FalseNegative);
         }

        [Test]
        public static void ToTextTestCaseTruePositive()
        {
            var utterances = new[]
            {
                new LabeledUtterance("foo", "DayTime", null),
                new LabeledUtterance("FOO", "DayTime", null)
            };

            var actualResult = TestCaseSource.ToTextTestCase(utterances);

            actualResult.Kind.Should().Be(TestResultKind.TruePositive);
        }

        [Test]
        public static void ToTextTestCaseFalsePositive()
        {
            var utterances = new[]
            {
                new LabeledUtterance("foo", "DayTime", null),
                new LabeledUtterance("baz", "DayTime", null)
            };

            var actualResult = TestCaseSource.ToTextTestCase(utterances);

            actualResult.Kind.Should().Be(TestResultKind.FalsePositive);
        }

        private static List<Entity> CreateEntityList(string type)
        {
            return new List<Entity> { new Entity(type, "EntityValue", "matchedText", 1) };
        }

        private static bool Matches(string input, string regEx)
        {
            return Regex.IsMatch(input, regEx);
        }
    }
}