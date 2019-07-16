// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace NLU.DevOps.CommandLine.Tests
{
    using System.Collections.Generic;
    using System.Linq;
    using CommandLine.Train;
    using FluentAssertions;
    using global::CommandLine;
    using Microsoft.Extensions.Logging;
    using NUnit.Framework;

    /// <summary>
    /// Unit tests for <see cref="BaseCommand{T}"/>
    /// </summary>
    /// <remarks>
    /// Known bug with Microsoft.Extesions.Logging.Console where lowest log level setting must be run first.
    /// </remarks>
    [TestFixture]
    internal class BaseCommandTests
    {
        private List<string> options;

        [SetUp]
        public void SetUp()
        {
            this.options = new List<string>
            {
                "-s",
                "luis",
                "-i",
                "../"
            };
        }

        [Test]
        public void WhenBaseOptionsAreSetACommandIsReturned()
        {
            var args = this.options.ToArray();
            ParserResult<TrainOptions> results = Parser.Default.ParseArguments<TrainOptions>(args);
            results.Tag.Should().Be(ParserResultType.Parsed);
        }

        [Test]
        public void WhenInvalidOptionAppearsParsingFails()
        {
            this.options.Add("-z");
            var args = this.options.ToArray();
            ParserResult<TrainOptions> results = Parser.Default.ParseArguments<TrainOptions>(args);
            results.Tag.Should().Be(ParserResultType.NotParsed);
            var notParsed = (NotParsed<TrainOptions>)results;
            notParsed.Errors.First().Tag.Should().Be(ErrorType.UnknownOptionError);
        }

        [Test]
        [Order(1)]
        public void WhenLoggingOptionsAreSetToQuietLogLevelIsWarning()
        {
            this.options.Add("-q");
            var args = this.options.ToArray();
            var results = Parser.Default.ParseArguments<BaseOptions>(args);
            var logOptions = (Parsed<BaseOptions>)results;
            var command = new BaseCommandMock(logOptions.Value);
            var logger = command.Logger;
            logger.IsEnabled(LogLevel.Information).Should().BeFalse();
            logger.IsEnabled(LogLevel.Warning).Should().BeTrue();
        }

        [Test]
        [Order(2)]
        public void WhenLoggingOptionsAreNotSetLogLevelIsInformation()
        {
            var args = this.options.ToArray();
            var results = Parser.Default.ParseArguments<BaseOptions>(args);
            var logOptions = (Parsed<BaseOptions>)results;
            var command = new BaseCommandMock(logOptions.Value);
            var logger = command.Logger;
            logger.IsEnabled(LogLevel.Debug).Should().BeFalse();
            logger.IsEnabled(LogLevel.Information).Should().BeTrue();
        }

        [Test]
        [Order(3)]
        public void WhenLoggingOptionsAreSetToVerboseLogLevelIsTrace()
        {
            this.options.Add("-v");
            var args = this.options.ToArray();
            var results = Parser.Default.ParseArguments<BaseOptions>(args);
            var logOptions = (Parsed<BaseOptions>)results;
            var command = new BaseCommandMock(logOptions.Value);
            var logger = command.Logger;
            logger.IsEnabled(LogLevel.Trace).Should().BeTrue();
        }
    }
}
