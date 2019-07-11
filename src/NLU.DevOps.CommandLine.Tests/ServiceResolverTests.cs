// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace NLU.DevOps.CommandLine.Tests
{
    using System.Collections.Generic;
    using CommandLine.Train;
    using FluentAssertions;
    using global::CommandLine;
    using Models;
    using NUnit.Framework;

    [TestFixture]
    internal class ServiceResolverTests
    {
        private string[] args;
        private List<string> options;

        [SetUp]
        public void SetUp()
        {
            this.options = new List<string>();
            this.options.Add("-s");
        }

        [Test]
        public void LuisSettingCreatesLuisServiceFactory()
        {
            this.options.Add("luis");
            this.args = this.options.ToArray();
            var serviceFactory = this.GetClientFactory();
            serviceFactory.Should().NotBeNull();
        }

        [Test]
        public void LexSettingCreatesLuisServiceFactory()
        {
            this.options.Add("lex");
            this.args = this.options.ToArray();
            var serviceFactory = this.GetClientFactory();
            serviceFactory.Should().NotBeNull();
        }

        private INLUClientFactory GetClientFactory()
        {
            BaseOptions baseOptions = null;
            var parser = Parser.Default.ParseArguments<TrainOptions>(this.args).WithParsed<TrainOptions>(o =>
            {
                baseOptions = o;
            });
            ServiceResolver.TryResolve<INLUClientFactory>(baseOptions, out var serviceFactory);
            return serviceFactory;
        }
    }
}
