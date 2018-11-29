// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace NLU.DevOps.Luis.Tests
{
    using System;
    using FluentAssertions;
    using NUnit.Framework;

    [TestFixture]
    internal static class LuisLanguageUnderstandingServiceBuilderTests
    {
        [Test]
        public static void ThrowsArgumentException()
        {
            var builder = new LuisLanguageUnderstandingServiceBuilder();
            Action action = () => builder.Build();

            // Default LUIS client checks for key
            action.Should().Throw<ArgumentException>().And.Message.Should().Contain("endpointKey").And.Contain("authoringKey");

            // Default LUIS client checks for region
            builder.AuthoringKey = Guid.NewGuid().ToString();
            action.Should().Throw<ArgumentException>().And.Message.Should().Contain("endpointRegion").And.Contain("authoringRegion");

            // LUIS implementation checks for app name
            builder.AuthoringRegion = "westus";
            action.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("appName");
        }
    }
}
