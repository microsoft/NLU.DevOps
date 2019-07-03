// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace NLU.DevOps.Models
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// NLU service extensions.
    /// </summary>
    public static class NLUServiceExtensions
    {
        /// <summary>
        /// Trains the NLU service.
        /// </summary>
        /// <returns>Task to await the training operation.</returns>
        /// <param name="instance">NLU service instance.</param>
        /// <param name="utterances">Labeled utterances to train on.</param>
        public static Task TrainAsync(this INLUService instance, IEnumerable<LabeledUtterance> utterances)
        {
            return instance.TrainAsync(utterances, CancellationToken.None);
        }

        /// <summary>
        /// Tests the NLU service.
        /// </summary>
        /// <returns>Task to await the resulting labeled utterance.</returns>
        /// <param name="instance">NLU service instance.</param>
        /// <param name="query">Query to test.</param>
        public static Task<LabeledUtterance> TestAsync(this INLUService instance, INLUQuery query)
        {
            return instance.TestAsync(query, CancellationToken.None);
        }

        /// <summary>
        /// Tests the NLU service using speech.
        /// </summary>
        /// <returns>Task to await the resulting labeled utterance.</returns>
        /// <param name="instance">NLU service instance.</param>
        /// <param name="speechFile">Speech file.</param>
        public static Task<LabeledUtterance> TestSpeechAsync(this INLUService instance, string speechFile)
        {
            return instance.TestSpeechAsync(speechFile, null, CancellationToken.None);
        }

        /// <summary>
        /// Tests the NLU service using speech.
        /// </summary>
        /// <returns>Task to await the resulting labeled utterance.</returns>
        /// <param name="instance">NLU service instance.</param>
        /// <param name="speechFile">Speech file.</param>
        /// <param name="query">Query to test.</param>
        public static Task<LabeledUtterance> TestSpeechAsync(this INLUService instance, string speechFile, INLUQuery query)
        {
            return instance.TestSpeechAsync(speechFile, query, CancellationToken.None);
        }

        /// <summary>
        /// Cleans up the NLU service.
        /// </summary>
        /// <returns>Task to await the cleanup operation.</returns>
        /// <param name="instance">NLU service instance.</param>
        public static Task CleanupAsync(this INLUService instance)
        {
            return instance.CleanupAsync(CancellationToken.None);
        }
    }
}
