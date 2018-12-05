// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace NLU.DevOps.Luis.Tests
{
    using System;
    using FluentAssertions;
    using Microsoft.Extensions.Configuration;
    using NUnit.Framework;

    [TestFixture]
    internal static class LuisNLUServiceFactoryTests
    {
        [Test]
        public static void ThrowsArgumentNull()
        {
            var factory = new LuisNLUServiceFactory();
            Action nullConfiguration = () => factory.CreateInstance(null, string.Empty);
            nullConfiguration.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("configuration");
        }
    }
}
