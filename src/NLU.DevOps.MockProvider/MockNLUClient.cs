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
            this.Utterances = new List<LabeledUtterance>();
            if (trainedUtterances != null)
            {
                this.Utterances.AddRange(
                    JsonConvert.DeserializeObject<IEnumerable<LabeledUtterance>>(trainedUtterances));
            }
        }

        public string TrainedUtterances => JsonConvert.SerializeObject(this.Utterances);

        private List<LabeledUtterance> Utterances { get; }

        public Task TrainAsync(IEnumerable<LabeledUtterance> utterances, CancellationToken cancellationToken)
        {
            this.Utterances.AddRange(utterances);
            return Task.CompletedTask;
        }

        public Task CleanupAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        protected override Task<LabeledUtterance> TestAsync(string utterance, CancellationToken cancellationToken)
        {
            var matchedUtterance = this.Utterances.FirstOrDefault(u => u.Text == utterance);
            return Task.FromResult(matchedUtterance ?? new LabeledUtterance(null, null, null));
        }

        protected override Task<LabeledUtterance> TestSpeechAsync(string speechFile, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        protected override void Dispose(bool disposing)
        {
        }
    }
}
