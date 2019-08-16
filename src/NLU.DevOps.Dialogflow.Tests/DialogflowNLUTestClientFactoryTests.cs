// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace NLU.DevOps.Dialogflow.Tests
{
    using System;
    using FluentAssertions;
    using Microsoft.Extensions.Configuration;
    using NUnit.Framework;

    [TestFixture]
    internal static class DialogflowNLUTestClientFactoryTests
    {
        [Test]
        public static void ThrowsArgumentNull()
        {
            var factory = new DialogflowNLUClientFactory();
            Action nullConfiguration = () => factory.CreateTestInstance(null, null);
            nullConfiguration.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("configuration");
        }

        [Test]
        public static void CreateTrainInstanceShouldThrowNotSupported()
        {
            var factory = new DialogflowNLUClientFactory();
            Action createTrainInstance = () => factory.CreateTrainInstance(null, null);
            createTrainInstance.Should().Throw<NotSupportedException>();
        }

        [Test]
        public static void CreatesTestInstance()
        {
            var factory = new DialogflowNLUClientFactory();
            var configuration = new ConfigurationBuilder().Build();
            using (var client = factory.CreateTestInstance(configuration, null))
            {
                client.Should().NotBeNull();
            }
        }
    }
}