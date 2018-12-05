// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace NLU.DevOps.Lex.Tests
{
    using System;
    using FluentAssertions;
    using NUnit.Framework;

    [TestFixture]
    internal static class LexSettingsTests
    {
        [Test]
        public static void ThrowsArgumentExceptions()
        {
            Action nullEntityType = () => new LexSettings(new EntityType[] { null });
            nullEntityType.Should().Throw<ArgumentException>().And.ParamName.Should().Be("entityTypes");
        }
    }
}
