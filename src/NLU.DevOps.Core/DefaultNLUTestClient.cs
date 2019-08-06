// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace NLU.DevOps.Core
{
    using System.Threading;
    using System.Threading.Tasks;
    using Models;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Default NLU test client, which extracts the text from the JSON query object.
    /// </summary>
    public abstract class DefaultNLUTestClient : NLUTestClientBase<DefaultQuery>
    {
        /// <inheritdoc />
        protected sealed override Task<LabeledUtterance> TestAsync(DefaultQuery query, CancellationToken cancellationToken)
        {
            return this.TestAsync(query.Text, cancellationToken);
        }

        /// <summary>
        /// Tests the NLU model.
        /// </summary>
        /// <returns>Task to await the resulting labeled utterance.</returns>
        /// <param name="utterance">Unlabeled utterance to test on.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        protected abstract Task<LabeledUtterance> TestAsync(string utterance, CancellationToken cancellationToken);

        /// <inheritdoc />
        protected sealed override Task<LabeledUtterance> TestSpeechAsync(string speechFile, DefaultQuery query, CancellationToken cancellationToken)
        {
            return this.TestSpeechAsync(speechFile, cancellationToken);
        }

        /// <summary>
        /// Tests the NLU model using speech.
        /// </summary>
        /// <returns>Task to await the resulting labeled utterance.</returns>
        /// <param name="speechFile">Speech files to test on.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        protected abstract Task<LabeledUtterance> TestSpeechAsync(string speechFile, CancellationToken cancellationToken);
    }
}
