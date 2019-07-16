// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace NLU.DevOps.CommandLine.Tests
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Models;

    internal sealed class NLUServiceMock : INLUService
    {
        public string ServiceName => "MockService";

        public void Dispose()
        {
        }

        public async Task<LabeledUtterance> TestAsync(INLUQuery query, CancellationToken cancellationToken)
        {
            return await Task.FromResult<LabeledUtterance>(null).ConfigureAwait(false);
        }

        public async Task<LabeledUtterance> TestSpeechAsync(string speechFile, INLUQuery query, CancellationToken cancellationToken)
        {
            return await Task.FromResult<LabeledUtterance>(null).ConfigureAwait(false);
        }

        public Task CleanupAsync(CancellationToken cancellationToken) =>
            Task.FromResult(0);

        public Task TrainAsync(IEnumerable<LabeledUtterance> utterances, CancellationToken cancellationToken) =>
            Task.FromResult(0);
    }
}
