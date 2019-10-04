// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace NLU.DevOps.ModelPerformance.Tests
{
    using FluentAssertions;
    using Newtonsoft.Json.Linq;
    using NUnit.Framework;

    [TestFixture]
    internal static class JTokenExtensionsTests
    {
        [Test]
        public static void ContainsSubtreeNull()
        {
            JTokenExtensions.ContainsSubtree(null, JValue.CreateNull()).Should().BeTrue();
            JTokenExtensions.ContainsSubtree(JValue.CreateNull(), null).Should().BeFalse();
        }

        [Test]
        [TestCase("null", "null")]
        [TestCase("42", "42")]
        [TestCase("\"foo\"", "\"foo\"")]
        [TestCase("[1]", "[1]")]
        [TestCase("[1,2]", "[1,2]")]
        [TestCase("[1,2]", "[2,1]")]
        [TestCase("[1]", "[1,2]")]
        [TestCase("[1,[2]]", "[1,[2]]")]
        [TestCase("[1,[2]]", "[1,[2,3]]")]
        [TestCase("{\"foo\": 42}", "{\"foo\": 42}")]
        [TestCase("{\"foo\": 42}", "{\"foo\": 42, \"bar\": 7}")]
        public static void ContainsSubtreeTrue(string expectedJson, string actualJson)
        {
            var expected = JToken.Parse(expectedJson);
            var actual = JToken.Parse(actualJson);
            expected.ContainsSubtree(actual).Should().BeTrue();
        }

        [Test]
        [TestCase("null", "42")]
        [TestCase("42", "\"foo\"")]
        [TestCase("[1]", "[null]")]
        [TestCase("[1,2]", "[1]")]
        [TestCase("[1,[2,3]]", "[1,[2]]")]
        [TestCase("{\"foo\": 42}", "{\"foo\": null}")]
        [TestCase("{\"foo\": 42, \"bar\": 7}", "{\"foo\": 42}")]
        public static void ContainsSubtreeFalse(string expectedJson, string actualJson)
        {
            var expected = JToken.Parse(expectedJson);
            var actual = JToken.Parse(actualJson);
            expected.ContainsSubtree(actual).Should().BeFalse();
        }

        [Test]
        public static void EvaluateNull()
        {
            JTokenExtensions.Evaluate(null, null).Should().BeNull();
        }

        [Test]
        [TestCase("csharp(1+1)", "2")]
        [TestCase("[csharp(1+1)]", "[2]")]
        [TestCase("{\"foo\":csharp(1+1)}", "{\"foo\":2}")]
        [TestCase("{\"foo\":[csharp(1+1)]}", "{\"foo\":[2]}")]
        public static void Evaluate(string actualJson, string expectedJson)
        {
            var expected = JToken.Parse(expectedJson);
            var actual = JToken.Parse(actualJson);
            JToken.DeepEquals(expected.Evaluate(null), actual).Should().BeTrue();
        }

        [Test]
        [TestCase("null")]
        [TestCase("42")]
        [TestCase("\"foo\"")]
        [TestCase("[]")]
        [TestCase("[42]")]
        [TestCase("{}")]
        [TestCase("{\"foo\":42}")]
        [TestCase("{\"foo\":[42, \"foo\"]}")]
        public static void EvaluateNoChange(string jsonString)
        {
            var json = JToken.Parse(jsonString);
            json.Evaluate(null).Should().BeSameAs(json);
        }

        [TestCase("csharp(x+1)", "3")]
        [TestCase("[csharp(x+1)]", "[3]")]
        [TestCase("{\"foo\":csharp(x+1)}", "{\"foo\":3}")]
        [TestCase("{\"foo\":[csharp(x+1)]}", "{\"foo\":[3]}")]
        public static void EvaluateWithGlobals(string actualJson, string expectedJson)
        {
            var globals = new { x = 2 };
            var expected = JToken.Parse(expectedJson);
            var actual = JToken.Parse(actualJson);
            JToken.DeepEquals(expected.Evaluate(globals), actual).Should().BeTrue();
        }
    }
}
