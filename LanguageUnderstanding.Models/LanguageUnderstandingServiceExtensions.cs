// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace LanguageUnderstanding.Models
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Language understanding service extensions.
    /// </summary>
    public static class LanguageUnderstandingServiceExtensions
    {
        /// <summary>
        /// Trains the language understanding service.
        /// </summary>
        /// <returns>A task to await the training operation.</returns>
        /// <param name="instance">Language understanding service instance.</param>
        /// <param name="utterances">Labeled utterances to train on.</param>
        /// <param name="entityTypes">Entity types to include in the model.</param>
        public static Task TrainAsync(this ILanguageUnderstandingService instance, IEnumerable<LabeledUtterance> utterances, IEnumerable<EntityType> entityTypes)
        {
            return instance.TrainAsync(utterances, entityTypes, CancellationToken.None);
        }

        /// <summary>
        /// Tests the language understanding service.
        /// </summary>
        /// <returns>A task to await the resulting labeled utterances.</returns>
        /// <param name="instance">Language understanding service instance.</param>
        /// <param name="utterances">Unlabeled utterances to test on.</param>
        public static Task<IEnumerable<LabeledUtterance>> TestAsync(this ILanguageUnderstandingService instance, IEnumerable<string> utterances)
        {
            return instance.TestAsync(utterances, CancellationToken.None);
        }

        /// <summary>
        /// Cleans up the language understanding service.
        /// </summary>
        /// <returns>A task to await the cleanup operation.</returns>
        /// <param name="instance">Language understanding service instance.</param>
        public static Task CleanupAsync(this ILanguageUnderstandingService instance)
        {
            return instance.CleanupAsync(CancellationToken.None);
        }
    }
}
