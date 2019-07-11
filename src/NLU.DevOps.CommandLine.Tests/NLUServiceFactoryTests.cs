// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace NLU.DevOps.CommandLine.Tests
{
    using System;
    using CommandLine.Train;
    using FluentAssertions;
    using global::CommandLine;
    using Microsoft.Extensions.Configuration;
    using NUnit.Framework;

    [TestFixture]
    internal static class NLUServiceFactoryTests
    {
        [Test]
        public static void SettingUnknownServiceThrowsException()
        {
            var options = default(BaseOptions);
            var args = new[]
            {
                "nlu",
                "command",
                "-s",
                "foo"
            };
            var parser = Parser.Default.ParseArguments<TrainOptions>(args).WithParsed<TrainOptions>(o =>
            {
                options = o;
            });

            IConfiguration configuration = new ConfigurationBuilder()
                .Build();

            Action createTrainInstance = () => NLUClientFactory.CreateTrainInstance(options, configuration);
            createTrainInstance.Should().Throw<InvalidOperationException>().WithMessage("Invalid service type 'foo'.");
            Action createTestInstance = () => NLUClientFactory.CreateTestInstance(options, configuration);
            createTestInstance.Should().Throw<InvalidOperationException>().WithMessage("Invalid service type 'foo'.");
        }
    }
}
