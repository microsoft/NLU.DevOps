// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace LanguageUnderstanding.Models
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Language understanding service interface.
    /// </summary>
    public interface ILanguageUnderstandingService
    {
        /// <summary>
        /// Trains the language understanding service.
        /// </summary>
        /// <returns>A task to await the training operation.</returns>
        /// <param name="utterances">Labeled utterances to train on.</param>
        /// <param name="entityTypes">Entity types to include in the model.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task TrainAsync(
            IEnumerable<LabeledUtterance> utterances,
            IEnumerable<EntityType> entityTypes,
            CancellationToken cancellationToken);

        /// <summary>
        /// Tests the language understanding service.
        /// </summary>
        /// <returns>A task to await the resulting labeled utterances.</returns>
        /// <param name="utterances">Unlabeled utterances to test on.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task<IEnumerable<LabeledUtterance>> TestAsync(IEnumerable<string> utterances, CancellationToken cancellationToken);

        /// <summary>
        /// Cleans up the language understanding service.
        /// </summary>
        /// <returns>A task to await the cleanup operation.</returns>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task CleanupAsync(CancellationToken cancellationToken);
    }
}
