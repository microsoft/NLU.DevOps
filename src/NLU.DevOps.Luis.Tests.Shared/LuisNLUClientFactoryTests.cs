// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace NLU.DevOps.Luis.Tests
{
    using System;
    using FluentAssertions;
    using NUnit.Framework;

    [TestFixture]
    internal static class LuisNLUClientFactoryTests
    {
        [Test]
        public static void ThrowsArgumentNull()
        {
            var factory = new LuisNLUClientFactory();
            Action nullTrainConfiguration = () => factory.CreateTrainInstance(null, string.Empty);
            Action nullTestConfiguration = () => factory.CreateTestInstance(null, string.Empty);
            nullTrainConfiguration.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("configuration");
            nullTestConfiguration.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("configuration");
        }
    }
}
