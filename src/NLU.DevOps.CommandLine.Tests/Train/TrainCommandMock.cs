// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace NLU.DevOps.CommandLine.Tests.Train
{
    using CommandLine.Train;
    using Models;

    internal class TrainCommandMock : TrainCommand
    {
        public TrainCommandMock(TrainOptions options)
            : base(options)
        {
        }

        protected override INLUTrainClient CreateNLUTrainClient() =>
            new MockNLUTrainClient();
    }
}
