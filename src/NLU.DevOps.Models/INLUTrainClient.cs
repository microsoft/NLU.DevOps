// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace NLU.DevOps.Models
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// NLU training interface.
    /// </summary>
    public interface INLUTrainClient : IDisposable
    {
        /// <summary>
        /// Trains the NLU model.
        /// </summary>
        /// <returns>Task to await the training operation.</returns>
        /// <param name="utterances">Labeled utterances to train on.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task TrainAsync(
            IEnumerable<ILabeledUtterance> utterances,
            CancellationToken cancellationToken);

        /// <summary>
        /// Cleans up the NLU model.
        /// </summary>
        /// <returns>Task to await the cleanup operation.</returns>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task CleanupAsync(CancellationToken cancellationToken);
    }
}
