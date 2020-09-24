// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace NLU.DevOps.CommandLine.Tests
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using Models;
    using Moq;

    internal class BaseCommandMock : BaseCommand<BaseOptions>
    {
        public BaseCommandMock(BaseOptions options)
            : base(options)
        {
        }

        public new ILogger Logger => base.Logger;

        public override Task<int> RunAsync()
        {
            throw new NotImplementedException();
        }

        protected override INLUTrainClient CreateNLUTrainClient()
        {
            return new Mock<INLUTrainClient>().Object;
        }

        protected override INLUTestClient CreateNLUTestClient()
        {
            return new Mock<INLUTestClient>().Object;
        }
    }
}
