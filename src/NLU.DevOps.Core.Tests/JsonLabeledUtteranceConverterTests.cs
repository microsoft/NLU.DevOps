// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace NLU.DevOps.Core.Tests
{
    using System;
    using System.Linq;
    using FluentAssertions;
    using FluentAssertions.Json;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using Newtonsoft.Json.Serialization;
    using NUnit.Framework;

    [TestFixture]
    internal static class JsonLabeledUtteranceConverterTests
    {
        [Test]
        public static void ConvertsUtteranceWithNoEntities()
        {
            var text = Guid.NewGuid().ToString();
            var intent = Guid.NewGuid().ToString();
            var expected = new LabeledUtterance(text, intent, null);
            var serializer = CreateSerializer();
            var json = JObject.FromObject(expected, serializer);
            var actual = json.ToObject<JsonLabeledUtterance>(serializer);
            actual.Text.Should().Be(expected.Text);
            actual.Intent.Should().Be(actual.Intent);
            actual.Entities.Should().BeNull();
        }

        [Test]
        public static void ConvertsUtteranceWithQuery()
        {
            var text = Guid.NewGuid().ToString();
            var json = new JObject { { "query", text } };
            var serializer = CreateSerializer();
            var actual = json.ToObject<JsonLabeledUtterance>(serializer);
            actual.Text.Should().Be(text);
        }

        [Test]
        public static void ConvertsUtteranceWithGenericEntity()
        {
            var entityType = Guid.NewGuid().ToString();
            var matchText = Guid.NewGuid().ToString();
            var matchIndex = 42;
            var expected = new Entity(entityType, null, matchText, matchIndex);
            var expectedUtterance = new LabeledUtterance(null, null, new[] { expected });
            var serializer = CreateSerializer();
            var json = JObject.FromObject(expectedUtterance, serializer);
            var actualUtterance = json.ToObject<JsonLabeledUtterance>(serializer);
            actualUtterance.Text.Should().BeNull();
            actualUtterance.Intent.Should().BeNull();
            actualUtterance.Entities.Count.Should().Be(1);
            var actual = actualUtterance.Entities.Single();
            actual.EntityType.Should().Be(expected.EntityType);
            actual.MatchText.Should().Be(expected.MatchText);
            actual.MatchIndex.Should().Be(expected.MatchIndex);
            actual.EntityValue.Should().BeNull();
        }

        [Test]
        public static void ConvertsUtteranceWithStartPosAndEndPosEntity()
        {
            var entityType = Guid.NewGuid().ToString();
            var text = "foo foo foo";
            var entityJson = new JObject
            {
                { "entityType", entityType },
                { "startPos", 8 },
                { "endPos", 10 },
            };

            var json = new JObject
            {
                { "text", text },
                { "entities", new JArray { entityJson } },
            };

            var serializer = CreateSerializer();
            var actual = json.ToObject<JsonLabeledUtterance>(serializer);
            actual.Text.Should().Be(text);
            actual.Entities.Count.Should().Be(1);
            actual.Entities[0].EntityType.Should().Be(entityType);
            actual.Entities[0].MatchText.Should().Be("foo");
            actual.Entities[0].MatchIndex.Should().Be(2);
        }

        [Test]
        public static void ConvertsUtteranceWithNestedEntities()
        {
            var text = "foo bar baz";

            var leafEntity = new JObject
            {
                { "entity", "baz" },
                { "startPos", 8 },
                { "endPos", 10 },
                { "foo", new JArray(42) },
                { "bar", null },
                { "baz", 42 },
                { "qux", JValue.CreateUndefined() },
            };

            var midEntity = new JObject
            {
                { "entityType", "bar" },
                { "matchText", "bar baz" },
                { "children", new JArray { leafEntity } },
                { "entityValue", new JObject { { "bar", "qux" } } },
            };

            var entity = new JObject
            {
                { "entity", "foo" },
                { "startPos", 0 },
                { "endPos", 10 },
                { "children", new JArray { midEntity } },
            };

            var json = new JObject
            {
                { "text", text },
                { "entities", new JArray { entity } },
            };

            var serializer = CreateSerializer();
            var actual = json.ToObject<JsonLabeledUtterance>(serializer);
            actual.Text.Should().Be(text);
            actual.Entities.Count.Should().Be(3);
            actual.Entities[0].EntityType.Should().Be("foo");
            actual.Entities[0].MatchText.Should().Be(text);
            actual.Entities[1].EntityType.Should().Be("foo::bar");
            actual.Entities[1].MatchText.Should().Be("bar baz");
            actual.Entities[1].EntityValue.Should().BeEquivalentTo(new JObject { { "bar", "qux" } });
            actual.Entities[2].EntityType.Should().Be("foo::bar::baz");
            actual.Entities[2].MatchText.Should().Be("baz");

            var additionalProperties = actual.Entities[2].As<Entity>().AdditionalProperties;
            additionalProperties["foo"].As<JToken>().Should().BeEquivalentTo(new JArray(42));
            additionalProperties["bar"].Should().BeNull();
            additionalProperties["baz"].Should().Be(42);
            additionalProperties["qux"].Should().BeNull();
        }

        private static JsonSerializer CreateSerializer()
        {
            var serializer = JsonSerializer.CreateDefault();
            serializer.ContractResolver = new CamelCasePropertyNamesContractResolver();
            serializer.DefaultValueHandling = DefaultValueHandling.Ignore;
            serializer.Converters.Add(new JsonLabeledUtteranceConverter());
            return serializer;
        }
    }
}
