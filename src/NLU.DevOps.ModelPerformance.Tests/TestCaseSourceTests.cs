// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace NLU.DevOps.ModelPerformance.Tests
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using FluentAssertions;
    using Models;
    using Newtonsoft.Json.Linq;
    using NUnit.Framework;

    [TestFixture]
    internal static class TestCaseSourceTests
    {
        private const string TruePositiveEntityResolutionRegex = @"TruePositiveEntityResolution\(.*?\)";
        private const string FalseNegativeEntityResolutionRegex = @"FalseNegativeEntityResolution\(.*?\)";
        private const string FalseNegativeEntityValueRegex = @"FalseNegativeEntityValue\(.*?\)";
        private const string TruePositiveEntityValueRegex = @"TruePositiveEntityValue\(.*?\)";
        private const string FalseNegativeEntityRegex = @"FalseNegativeEntity\(.*?\)";
        private const string FalsePositiveEntityRegex = @"FalsePositiveEntity\(.*?\)";
        private const string TruePositiveEntityRegex = @"TruePositiveEntity\(.*?\)";
        private const string FalsePositiveIntentRegex = @"FalsePositiveIntent\(.*?\)";
        private const string FalseNegativeIntentRegex = @"FalseNegativeIntent\(.*?\)";

        [Test]
        public static void TestToIntentTestCaseExpectingNoneActualDayTime()
        {
            var utterances = new[]
            {
                new LabeledUtterance("FOO", "None", null),
                new LabeledUtterance("FOO", "DayTime", null)
            };

            var test = TestCaseSource.ToIntentTestCase(utterances);

            test.TestName.Should().MatchRegex(FalsePositiveIntentRegex);
            test.ResultKind.Should().Be(ConfusionMatrixResultKind.FalsePositive);
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

            test.TestName.Should().MatchRegex(FalseNegativeIntentRegex);
            test.ResultKind.Should().Be(ConfusionMatrixResultKind.FalseNegative);
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

            actualTestResult.First().ResultKind.Should().Be(ConfusionMatrixResultKind.FalseNegative);
            actualTestResult.First().TestName.Should().MatchRegex(FalseNegativeEntityRegex);
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
            var actualFalsePositive = testResult.FirstOrDefault(t => t.ResultKind == ConfusionMatrixResultKind.FalsePositive);
            var actualFalseNegative = testResult.FirstOrDefault(t => t.ResultKind == ConfusionMatrixResultKind.FalseNegative);
            var actualFalseNegativeEntityValue = testResult.FirstOrDefault(t => Matches(t.TestName, FalseNegativeEntityValueRegex));
            actualFalsePositive.Should().NotBeNull();
            actualFalsePositive.TestName.Should().MatchRegex(FalsePositiveEntityRegex);
            actualFalseNegative.Should().NotBeNull();
            actualFalseNegative.TestName.Should().MatchRegex(FalseNegativeEntityRegex);
            actualFalseNegativeEntityValue.Should().NotBeNull();
        }

        [Test]
        public static void TestToEntityTestCasesMissingEntityInExpectedFile()
        {
            var actualEntity = CreateEntityList("EntityType");
            actualEntity.Add(new Entity("RecognizedEntity", "value", null, "text", 2));
            var expectedEntity = CreateEntityList("EntityType");
            var utterances = new[]
            {
                new LabeledUtterance("FOO", "DayTime", expectedEntity),
                new LabeledUtterance("FOO", "DayTime", actualEntity)
            };

            var actualTestResult = TestCaseSource.ToEntityTestCases(utterances);

            var testResult = actualTestResult.ToList();
            testResult.Count().Should().Be(3);
            var actualFalsePositive = testResult.FirstOrDefault(t => t.ResultKind == ConfusionMatrixResultKind.FalsePositive);
            var actualTruePositive = testResult.FirstOrDefault(t => t.ResultKind == ConfusionMatrixResultKind.TruePositive);
            var actualTruePositiveEntityValue = testResult.FirstOrDefault(t => Matches(t.TestName, TruePositiveEntityValueRegex));
            actualFalsePositive.Should().NotBeNull();
            actualFalsePositive.TestName.Should().MatchRegex(FalsePositiveEntityRegex);
            actualTruePositive.Should().NotBeNull();
            actualTruePositive.TestName.Should().MatchRegex(TruePositiveEntityRegex);
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
            var actualFalseNegative = testResult.FirstOrDefault(t => t.ResultKind == ConfusionMatrixResultKind.FalseNegative);
            var actualFalseNegativeEntityValue = testResult.FirstOrDefault(t => Matches(t.TestName, FalseNegativeEntityValueRegex));
            actualFalseNegative.Should().NotBeNull();
            actualFalseNegative.TestName.Should().MatchRegex(FalseNegativeEntityRegex);
            actualFalseNegativeEntityValue.Should().NotBeNull();
        }

        [Test]
        public static void TestToEntityTestCasesWithDifferentEntityValueInActualVersusExpected()
        {
            var actualEntity = new List<Entity> { new Entity("EntityType", "differentEntityValue", null, "differentMatchedText", 1) };
            var expectedEntity = CreateEntityList("EntityType");
            var utterances = new[]
            {
                 new LabeledUtterance("FOO", "DayTime", expectedEntity),
                 new LabeledUtterance("FOO", "DayTime", actualEntity)
            };

            var actualTestResult = TestCaseSource.ToEntityTestCases(utterances);

            var testResult = actualTestResult.ToList();
            testResult.Count().Should().Be(3);
            var actualFalsePositive = testResult.FirstOrDefault(t => t.ResultKind == ConfusionMatrixResultKind.FalsePositive);
            var actualFalseNegative = testResult.FirstOrDefault(t => t.ResultKind == ConfusionMatrixResultKind.FalseNegative);
            var actualFalseNegativeEntityValue = testResult.FirstOrDefault(t => Matches(t.TestName, FalseNegativeEntityValueRegex));
            actualFalsePositive.Should().NotBeNull();
            actualFalsePositive.TestName.Should().MatchRegex(FalsePositiveEntityRegex);
            actualFalseNegative.Should().NotBeNull();
            actualFalseNegative.TestName.Should().MatchRegex(FalseNegativeEntityRegex);
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

            actualResult.ResultKind.Should().Be(ConfusionMatrixResultKind.TrueNegative);
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

             actualResult.ResultKind.Should().Be(ConfusionMatrixResultKind.FalseNegative);
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

            actualResult.ResultKind.Should().Be(ConfusionMatrixResultKind.TruePositive);
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

            actualResult.ResultKind.Should().Be(ConfusionMatrixResultKind.FalsePositive);
        }

        [Test]
        [TestCase("{}", null, FalseNegativeEntityResolutionRegex)]
        [TestCase("{}", "null", FalseNegativeEntityResolutionRegex)]
        [TestCase("[]", "null", FalseNegativeEntityResolutionRegex)]
        [TestCase("42", "null", FalseNegativeEntityResolutionRegex)]
        [TestCase("{\"foo\":42}", "{}", FalseNegativeEntityResolutionRegex)]
        [TestCase("[\"foo\"]", "[]", FalseNegativeEntityResolutionRegex)]
        [TestCase("{\"foo\":42, \"bar\":42}", "{\"bar\":42, \"foo\":42}", TruePositiveEntityResolutionRegex)]
        [TestCase("[1,2]", "[2,1]", TruePositiveEntityResolutionRegex)]
        public static void ToEntityTestCasesWithEntityResolution(string expectedJson, string actualJson, string testMessageRegex)
        {
            var actualEntity = new List<Entity> { new Entity("EntityType", "EntityValue", ParseResolutionJson(actualJson), "differentMatchedText", 1) };
            var expectedEntity = new List<Entity> { new Entity("EntityType", "EntityValue", ParseResolutionJson(expectedJson), "differentMatchedText", 1) };
            var utterances = new[]
            {
                 new LabeledUtterance("FOO", "DayTime", expectedEntity),
                 new LabeledUtterance("FOO", "DayTime", actualEntity)
            };

            var actualTestResult = TestCaseSource.ToEntityTestCases(utterances);

            var testResult = actualTestResult.ToList();
            testResult.Count().Should().Be(3);
            var actual = testResult.FirstOrDefault(t => Matches(t.TestName, testMessageRegex));
            actual.Should().NotBeNull();
        }

        [Test]
        public static void ToEntityTestCasesWithNullExpectedEntityResolution()
        {
            var actualEntity = new List<Entity> { new Entity("EntityType", "EntityValue", "foo", "differentMatchedText", 1) };
            var expectedEntity = new List<Entity> { new Entity("EntityType", "EntityValue", JValue.CreateNull(), "differentMatchedText", 1) };
            var utterances = new[]
            {
                 new LabeledUtterance("FOO", "DayTime", expectedEntity),
                 new LabeledUtterance("FOO", "DayTime", actualEntity)
            };

            var actualTestResult = TestCaseSource.ToEntityTestCases(utterances);

            var testResult = actualTestResult.ToList();
            testResult.Count().Should().Be(2);
            testResult.Should().NotContain(t => Matches(t.TestName, TruePositiveEntityResolutionRegex));
        }

        [Test]
        public static void ToEntityTestCasesWithNullExpectedEntityValue()
        {
            var actualEntity = new List<Entity> { new Entity("EntityType", "EntityValue", null, "differentMatchedText", 1) };
            var expectedEntity = new List<Entity> { new Entity("EntityType", JValue.CreateNull(), null, "differentMatchedText", 1) };
            var utterances = new[]
            {
                 new LabeledUtterance("FOO", "DayTime", expectedEntity),
                 new LabeledUtterance("FOO", "DayTime", actualEntity)
            };

            var actualTestResult = TestCaseSource.ToEntityTestCases(utterances);

            var testResult = actualTestResult.ToList();
            testResult.Count().Should().Be(1);
            testResult.Should().NotContain(t => Matches(t.TestName, TruePositiveEntityValueRegex));
        }

        [Test]
        public static void ToEntityTestCasesWithIncorrectMatchIndex()
        {
            var actualEntity = new List<Entity> { new Entity("EntityType", "EntityValue", null, "differentMatchedText", 1) };
            var expectedEntity = new List<Entity> { new Entity("EntityType", null, null, "differentMatchedText", 2) };
            var utterances = new[]
            {
                 new LabeledUtterance("FOO", "DayTime", expectedEntity),
                 new LabeledUtterance("FOO", "DayTime", actualEntity)
            };

            var actualTestResult = TestCaseSource.ToEntityTestCases(utterances);

            var testResult = actualTestResult.ToList();
            testResult.Count().Should().Be(2);
            testResult.Should().Contain(t => Matches(t.TestName, FalsePositiveEntityRegex));
            testResult.Should().Contain(t => Matches(t.TestName, FalseNegativeEntityRegex));
        }

        private static List<Entity> CreateEntityList(string type)
        {
            return new List<Entity> { new Entity(type, "EntityValue", null, "matchedText", 1) };
        }

        private static bool Matches(string input, string regEx)
        {
            return Regex.IsMatch(input, regEx);
        }

        private static JToken ParseResolutionJson(string json)
        {
            return json != null ? JToken.Parse(json) : null;
        }
    }
}