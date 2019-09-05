// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace NLU.DevOps.CommandLine.Tests
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Models;

    internal sealed class MockNLUTrainClient : INLUTrainClient
    {
        public string ServiceName => "MockService";

        public Task CleanupAsync(CancellationToken cancellationToken) =>
            Task.FromResult(0);

        public Task TrainAsync(IEnumerable<LabeledUtterance> utterances, CancellationToken cancellationToken) =>
            Task.FromResult(0);

        public void Dispose()
        {
        }
    }
}
