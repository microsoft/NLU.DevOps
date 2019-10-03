// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace NLU.DevOps.Luis.Tests
{
    using System;
    using FluentAssertions;
    using Microsoft.Extensions.Configuration;
    using NUnit.Framework;

    [TestFixture]
    internal static class TestLuisConfigurationTests
    {
        [Test]
        public static void MissingAppIdThrowsInvalidOperation()
        {
            var luisConfiguration = new TestLuisConfiguration(new ConfigurationBuilder().Build());
            Func<string> missingAppId = () => luisConfiguration.AppId;
            missingAppId.Should().Throw<InvalidOperationException>().And.Message.Should().Contain("luisAppId");
        }
    }
}
