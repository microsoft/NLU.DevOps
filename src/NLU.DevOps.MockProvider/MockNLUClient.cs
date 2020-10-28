// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace NLU.DevOps.MockProvider
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Core;
    using Models;
    using Newtonsoft.Json;

    internal class MockNLUClient : DefaultNLUTestClient, INLUTrainClient
    {
        public MockNLUClient(string trainedUtterances)
        {
            this.Utterances = new List<ILabeledUtterance>();
            if (trainedUtterances != null)
            {
                this.Utterances.AddRange(
                    JsonConvert.DeserializeObject<IEnumerable<LabeledUtterance>>(trainedUtterances));
            }
        }

        public string TrainedUtterances => JsonConvert.SerializeObject(this.Utterances);

        private List<ILabeledUtterance> Utterances { get; }

        public Task TrainAsync(IEnumerable<ILabeledUtterance> utterances, CancellationToken cancellationToken)
        {
            this.Utterances.AddRange(utterances);
            return Task.CompletedTask;
        }

        public Task CleanupAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        protected override Task<ILabeledUtterance> TestAsync(string utterance, CancellationToken cancellationToken)
        {
            var matchedUtterance = this.Utterances.FirstOrDefault(u => u.Text == utterance) ?? new LabeledUtterance(null, null, null);
            return Task.FromResult(matchedUtterance.WithTimestamp(DateTimeOffset.Now));
        }

        protected override Task<ILabeledUtterance> TestSpeechAsync(string speechFile, CancellationToken cancellationToken)
        {
            return Task.FromResult<ILabeledUtterance>(new LabeledUtterance(null, null, null));
        }

        protected override void Dispose(bool disposing)
        {
        }
    }
}
