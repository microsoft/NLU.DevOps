// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace NLU.DevOps.Models
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// NLU service interface.
    /// </summary>
    public interface INLUService : IDisposable
    {
        /// <summary>
        /// Trains the NLU service.
        /// </summary>
        /// <returns>Task to await the training operation.</returns>
        /// <param name="utterances">Labeled utterances to train on.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task TrainAsync(
            IEnumerable<LabeledUtterance> utterances,
            CancellationToken cancellationToken);

        /// <summary>
        /// Tests the NLU service.
        /// </summary>
        /// <returns>Task to await the resulting labeled utterance.</returns>
        /// <param name="utterance">Unlabeled utterances to test on.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task<LabeledUtterance> TestAsync(
            string utterance,
            CancellationToken cancellationToken);

        /// <summary>
        /// Tests the NLU service using speech.
        /// </summary>
        /// <returns>Task to await the resulting labeled utterance.</returns>
        /// <param name="speechFile">Speech files to test on.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task<LabeledUtterance> TestSpeechAsync(
            string speechFile,
            CancellationToken cancellationToken);

        /// <summary>
        /// Cleans up the NLU service.
        /// </summary>
        /// <returns>Task to await the cleanup operation.</returns>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task CleanupAsync(CancellationToken cancellationToken);
    }
}
