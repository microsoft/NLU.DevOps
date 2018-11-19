// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace LanguageUnderstanding.Luis.Tests
{
    using System;
    using FluentAssertions;
    using NUnit.Framework;

    [TestFixture]
    internal static class LuisLanguageUnderstandingServiceBuilderTests
    {
        [Test]
        public static void ThrowsInvalidOperation()
        {
            var builder = new LuisLanguageUnderstandingServiceBuilder();
            Action action = () => builder.Build();

            action.Should().Throw<InvalidOperationException>();

            builder.AppName = Guid.NewGuid().ToString();
            action.Should().Throw<InvalidOperationException>();

            builder.AuthoringRegion = Guid.NewGuid().ToString();
            action.Should().Throw<InvalidOperationException>();
        }
    }
}
