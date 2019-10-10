// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace NLU.DevOps.Core.Tests
{
    using System;
    using FluentAssertions;
    using Models;
    using Newtonsoft.Json.Linq;
    using NUnit.Framework;

    [TestFixture]
    internal static class LabeledUtterancePropertyExtensionsTests
    {
        private const double Epsilon = 1e-6;

        [Test]
        public static void ThrowsArgumentNull()
        {
            Action nullUtterance = () => LabeledUtterancePropertyExtensions.WithTextScore(null, null);
            nullUtterance.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("instance");
        }

        [Test]
        public static void DoesNotCreateJsonInstance()
        {
            var expected = new Entity(string.Empty, null, string.Empty, 0);
            var actual = expected.WithScore(null);
            actual.Should().BeSameAs(expected);
            expected.GetScore().Should().BeNull();
            actual.GetScore().Should().BeNull();
        }

        [Test]
        public static void DoesNotCreateNewInstance()
        {
            var expected = new JsonEntity(string.Empty, null, string.Empty, 0);
            var actual = expected.WithScore(0.1);
            actual.Should().BeSameAs(expected);
            expected.AdditionalProperties.ContainsKey("score").Should().BeTrue();
            expected.AdditionalProperties["score"].As<double>().Should().BeApproximately(0.1, Epsilon);
        }

        [Test]
        public static void UtteranceRoundtrip()
        {
            var utterance = new LabeledUtterance(null, null, null)
                .WithScore(0.42)
                .WithTextScore(0.5)
                .WithTimestamp(DateTimeOffset.Now.Date);
            var roundtrip = JToken.FromObject(utterance).ToObject<JsonLabeledUtterance>();
            roundtrip.GetScore().Should().BeApproximately(utterance.GetScore(), Epsilon);
            roundtrip.GetTextScore().Should().BeApproximately(utterance.GetTextScore(), Epsilon);
            roundtrip.GetTimestamp().Should().Be(utterance.GetTimestamp());
        }

        [Test]
        public static void ParsesJsonValues()
        {
            var utterance = new JsonLabeledUtterance(null, null, null);
            utterance.AdditionalProperties.Add("score", new JValue(0.42));
            utterance.GetScore().Should().BeApproximately(0.42, Epsilon);
        }

        [Test]
        public static void ReturnsNullPropertyValues()
        {
            var utterance = new LabeledUtterance(null, null, null);
            utterance.GetScore().Should().BeNull();
            utterance.GetTextScore().Should().BeNull();
            utterance.GetTimestamp().Should().BeNull();
            utterance.GetUtteranceId().Should().BeNull();
        }

        [Test]
        public static void GetsUtteranceIdFromJson()
        {
            var utteranceId = Guid.NewGuid().ToString();
            var utterance = new JsonLabeledUtterance(null, null, null);
            utterance.AdditionalProperties.Add("utteranceId", utteranceId);
            utterance.GetUtteranceId().Should().Be(utteranceId);
        }

        [Test]
        public static void EntityRoundtrip()
        {
            var entity = new Entity(null, null, null, 0)
                .WithScore(0.42);
            var roundtrip = JToken.FromObject(entity).ToObject<JsonEntity>();
            roundtrip.GetScore().Should().BeApproximately(entity.GetScore(), Epsilon);
        }
    }
}
