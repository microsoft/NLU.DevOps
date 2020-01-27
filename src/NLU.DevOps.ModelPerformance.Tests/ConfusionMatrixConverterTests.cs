// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace NLU.DevOps.ModelPerformance.Tests
{
    using FluentAssertions;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using NUnit.Framework;

    [TestFixture]
    internal static class ConfusionMatrixConverterTests
    {
        [Test]
        public static void WritesNullValue()
        {
            var converter = new ConfusionMatrixConverter();
            var serializer = JsonSerializer.CreateDefault();
            serializer.Converters.Add(converter);
            var json = JToken.FromObject(new ConfusionMatrixContainer(), serializer);
            json.Type.Should().Be(JTokenType.Object);
            json.As<JObject>().ContainsKey("Data").Should().BeTrue();
            json["Data"].Type.Should().Be(JTokenType.Null);
        }

        [Test]
        public static void WritesValueAsArray()
        {
            var value = new ConfusionMatrix(42, 7, 1, 0);
            var converter = new ConfusionMatrixConverter();
            var serializer = JsonSerializer.CreateDefault();
            serializer.Converters.Add(converter);
            var json = JToken.FromObject(value, serializer);
            json.Type.Should().Be(JTokenType.Array);
            json.As<JArray>().Count.Should().Be(4);
            json.Value<int>(0).Should().Be(42);
            json.Value<int>(1).Should().Be(7);
            json.Value<int>(2).Should().Be(1);
            json.Value<int>(3).Should().Be(0);
        }

        [Test]
        public static void UsesConverterByDefault()
        {
            var value = new ConfusionMatrixContainer
            {
                Data = new ConfusionMatrix(42, 7, 1, 0),
            };

            var json = JToken.FromObject(value, JsonSerializer.CreateDefault());
            json.Type.Should().Be(JTokenType.Object);
            json.As<JObject>().ContainsKey("Data").Should().BeTrue();
            json["Data"].Type.Should().Be(JTokenType.Array);
            json["Data"].As<JArray>().Count.Should().Be(4);
            json["Data"].Value<int>(0).Should().Be(42);
            json["Data"].Value<int>(1).Should().Be(7);
            json["Data"].Value<int>(2).Should().Be(1);
            json["Data"].Value<int>(3).Should().Be(0);
        }

        private class ConfusionMatrixContainer
        {
            public ConfusionMatrix Data { get; set; }
        }
    }
}
