// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace LanguageUnderstanding.Json.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;
    using FluentAssertions;
    using Models;
    using Newtonsoft.Json.Linq;
    using NUnit.Framework;

    [TestFixture]
    internal static class SerializerTests
    {
        [Test]
        public static void ReadsEntities()
        {
            var entities = Serializer.Read<List<EntityType>>(Path.Combine(".", "models", "entities.json"));
            entities[0].Kind.Should().Be(EntityTypeKind.Builtin);
            entities[0].Name.Should().Be("BookFlight");
            entities[1].Kind.Should().Be(EntityTypeKind.Simple);
            entities[1].Name.Should().Be("FlightType");
        }

        [Test]
        public static void ReadsUtterances()
        {
            var utterances = Serializer.Read<List<LabeledUtterance>>(Path.Combine(".", "models", "utterances.json"));
            utterances.Count.Should().Be(3);
            utterances[0].Text.Should().Be("Book me a flight to Cairo");
            utterances[0].Intent.Should().Be("BookFlight");
            utterances[0].Entities[0].EntityType.Should().Be("City");
            utterances[0].Entities[0].EntityValue.Should().Be("Cairo");
            utterances[0].Entities[0].MatchText.Should().Be("Egypt");
            utterances[0].Entities[0].MatchIndex.Should().Be(0);

            // Order doesn't matter
            utterances[2].Text.Should().Be("Who is the fourth president");
            utterances[2].Intent.Should().Be("Search");
            utterances[2].Entities[0].EntityType.Should().Be("Ordinal");
            utterances[2].Entities[0].EntityValue.Should().Be("Fourth");
            utterances[2].Entities[0].MatchText.Should().Be("fourth");
            utterances[2].Entities[0].MatchIndex.Should().Be(0);
        }

        [Test]
        public static void ReadLeavesStreamOpen()
        {
            var expected = 42;
            var bytes = Encoding.UTF8.GetBytes(expected.ToString(CultureInfo.InvariantCulture));
            using (var stream = new MemoryStream(bytes))
            {
                var actual = Serializer.Read<int>(stream);
                actual.Should().Be(expected);

                var seek = new Action(() => stream.Position = 0);
                seek.Should().NotThrow<ObjectDisposedException>();
            }
        }

        [Test]
        public static void WritesUtterances()
        {
            var text = Guid.NewGuid().ToString();
            var intent = Guid.NewGuid().ToString();
            var entityType = Guid.NewGuid().ToString();
            var entityValue = Guid.NewGuid().ToString();
            var matchText = Guid.NewGuid().ToString();
            var matchIndex = 42;
            var entity = new Entity(entityType, entityValue, matchText, matchIndex);
            var utterance = new LabeledUtterance(text, intent, new[] { entity });

            var path = Path.GetTempFileName();
            try
            {
                Serializer.Write(path, new[] { utterance });
                var jsonText = File.ReadAllText(path);
                var jsonArray = JArray.Parse(jsonText);

                jsonArray.Count.Should().Be(1);
                var jsonUtterance = jsonArray[0].As<JObject>();
                jsonUtterance.ContainsKey("text").Should().BeTrue();
                jsonUtterance.Value<string>("text").Should().Be(text);
                jsonUtterance.ContainsKey("intent").Should().BeTrue();
                jsonUtterance.Value<string>("intent").Should().Be(intent);
                jsonUtterance.ContainsKey("entities").Should().BeTrue();
                jsonUtterance["entities"].As<JArray>().Count.Should().Be(1);

                var jsonEntity = jsonUtterance["entities"].As<JArray>()[0].As<JObject>();
                jsonEntity.ContainsKey("entityType").Should().BeTrue();
                jsonEntity.Value<string>("entityType").Should().Be(entityType);
                jsonEntity.ContainsKey("entityValue").Should().BeTrue();
                jsonEntity.Value<string>("entityValue").Should().Be(entityValue);
                jsonEntity.ContainsKey("matchText").Should().BeTrue();
                jsonEntity.Value<string>("matchText").Should().Be(matchText);
                jsonEntity.ContainsKey("matchIndex").Should().BeTrue();
                jsonEntity.Value<int>("matchIndex").Should().Be(matchIndex);
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Test]
        public static void WriteLeavesStreamOpen()
        {
            using (var stream = new MemoryStream())
            {
                Serializer.Write(stream, "foo");
                var seek = new Action(() => stream.Position = 0);
                seek.Should().NotThrow<ObjectDisposedException>();
            }
        }
    }
}