// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace LanguageUnderstanding.Loaders.Tests
{
    using System.IO;
    using FluentAssertions;
    using Models;
    using NUnit.Framework;

    [TestFixture]
    internal class LoadersTestFunctions
    {
        [Test]
        public void TestEntitiesFunction()
        {
            var entities = Loader.LoadEntities(Path.Combine(".", "models", "entities.json"));
            entities[0].Kind.Should().Be(EntityTypeKind.Builtin);
            entities[0].Name.Should().Be("BookFlight");
            entities[1].Kind.Should().Be(EntityTypeKind.Simple);
            entities[1].Name.Should().Be("FlightType");
        }

        [Test]
        public void TestUtterancesFunction()
        {
            var utterances = Loader.LoadUtterances(Path.Combine(".", "models", "utterances.json"));
            utterances.Count.Should().Be(2);
            utterances[0].Text.Should().Be("Book me a flight to Cairo");
            utterances[0].Intent.Should().Be("BookFlight");
            utterances[0].Entities[0].EntityType.Should().Be("City");
            utterances[0].Entities[0].EntityValue.Should().Be("Cairo");
            utterances[0].Entities[0].MatchToken.Should().Be("Egypt");
            utterances[0].Entities[0].MatchIndex.Should().Be(0);
        }
    }
}