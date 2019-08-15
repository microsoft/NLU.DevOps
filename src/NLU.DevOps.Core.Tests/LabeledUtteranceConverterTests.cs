// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace NLU.DevOps.Core.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using FluentAssertions;
    using Models;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using Newtonsoft.Json.Serialization;
    using NUnit.Framework;

    [TestFixture]
    internal static class LabeledUtteranceConverterTests
    {
        [Test]
        public static void ConvertsUtteranceWithNoEntities()
        {
            var text = Guid.NewGuid().ToString();
            var intent = Guid.NewGuid().ToString();
            var expected = new LabeledUtterance(text, intent, null);
            var serializer = CreateSerializer();
            var json = JObject.FromObject(expected);
            var actual = json.ToObject<LabeledUtterance>(serializer);
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
            var actual = json.ToObject<LabeledUtterance>(serializer);
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
            var actualUtterance = json.ToObject<LabeledUtterance>(serializer);
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
            var actual = json.ToObject<LabeledUtterance>(serializer);
            actual.Text.Should().Be(text);
            actual.Entities.Count.Should().Be(1);
            actual.Entities[0].EntityType.Should().Be(entityType);
            actual.Entities[0].MatchText.Should().Be("foo");
            actual.Entities[0].MatchIndex.Should().Be(2);
        }

        [Test]
        public static void DeserializesDerivedType()
        {
            var entityType = Guid.NewGuid().ToString();
            var text = "foo foo foo";
            var utteranceScore = 0.07;
            var entityScore = 0.42;
            var entityJson = new JObject
            {
                { "entityType", entityType },
                { "startPos", 8 },
                { "endPos", 10 },
                { "score", entityScore },
            };

            var json = new JObject
            {
                { "text", text },
                { "entities", new JArray { entityJson } },
                { "score", utteranceScore },
            };

            var serializer = CreateSerializer();
            var actual = json.ToObject<DerivedLabeledUtterance>(serializer);
            actual.Text.Should().Be(text);
            actual.Score.Should().Be(utteranceScore);
            actual.Entities.Count.Should().Be(1);
            actual.Entities[0].EntityType.Should().Be(entityType);
            actual.Entities[0].MatchText.Should().Be("foo");
            actual.Entities[0].MatchIndex.Should().Be(2);
            actual.Entities[0].Score.Should().Be(entityScore);
        }

        [Test]
        public static void ThrowsWithInvalidStartPos()
        {
            var entityJson = new JObject
            {
                { "startPos", 3 },
                { "endPos", 5 },
            };

            var json = new JObject
            {
                { "text", "foo" },
                { "entities", new JArray { entityJson } },
            };

            var serializer = CreateSerializer();
            Action toObject = () => json.ToObject<LabeledUtterance>(serializer);
            toObject.Should().Throw<InvalidOperationException>();

            entityJson["startPos"] = 5;
            toObject.Should().Throw<InvalidOperationException>();

            entityJson["startPos"] = 0;
            entityJson["endPos"] = 2;
            toObject.Should().NotThrow<InvalidOperationException>();
        }

        [Test]
        public static void ThrowsWithInvalidEndPos()
        {
            var entityJson = new JObject
            {
                { "startPos", 4 },
                { "endPos", 7 },
            };

            var json = new JObject
            {
                { "text", "foo bar" },
                { "entities", new JArray { entityJson } },
            };

            var serializer = CreateSerializer();
            Action toObject = () => json.ToObject<LabeledUtterance>(serializer);
            toObject.Should().Throw<InvalidOperationException>();

            entityJson["endPos"] = 4;
            toObject.Should().Throw<InvalidOperationException>();

            entityJson["endPos"] = 6;
            toObject.Should().NotThrow<InvalidOperationException>();
        }

        [Test]
        public static void ThrowsWithMatchIndexSet()
        {
            var entityJson = new JObject
            {
                { "startPos", 0 },
                { "endPos", 2 },
                { "matchIndex", 1 },
            };

            var json = new JObject
            {
                { "text", "foo" },
                { "entities", new JArray { entityJson } },
            };

            var serializer = CreateSerializer();
            Action toObject = () => json.ToObject<LabeledUtterance>(serializer);
            toObject.Should().Throw<ArgumentException>();
        }

        private static JsonSerializer CreateSerializer()
        {
            var serializer = JsonSerializer.CreateDefault();
            serializer.ContractResolver = new CamelCasePropertyNamesContractResolver();
            serializer.DefaultValueHandling = DefaultValueHandling.Ignore;
            serializer.Converters.Add(new LabeledUtteranceConverter());
            return serializer;
        }

        private class DerivedEntity : Entity
        {
            public DerivedEntity(string entityType, string matchText, int matchIndex, double score)
                : base(entityType, null, matchText, matchIndex)
            {
                this.Score = score;
            }

            public double Score { get; }
        }

        private class DerivedLabeledUtterance : LabeledUtterance
        {
            public DerivedLabeledUtterance(string text, string intent, double score, IReadOnlyList<DerivedEntity> entities)
                : base(text, intent, entities)
            {
                this.Score = score;
            }

            public double Score { get; }

            public new IReadOnlyList<DerivedEntity> Entities => base.Entities?.OfType<DerivedEntity>().ToList();
        }
    }
}
