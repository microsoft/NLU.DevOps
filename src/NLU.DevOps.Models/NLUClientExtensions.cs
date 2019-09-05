// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace NLU.DevOps.Models
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// NLU client extensions.
    /// </summary>
    public static class NLUClientExtensions
    {
        /// <summary>
        /// Trains the NLU model.
        /// </summary>
        /// <returns>Task to await the training operation.</returns>
        /// <param name="instance">NLU training client instance.</param>
        /// <param name="utterances">Labeled utterances to train on.</param>
        public static Task TrainAsync(this INLUTrainClient instance, IEnumerable<LabeledUtterance> utterances)
        {
            return instance.TrainAsync(utterances, CancellationToken.None);
        }

        /// <summary>
        /// Cleans up the NLU model.
        /// </summary>
        /// <returns>Task to await the cleanup operation.</returns>
        /// <param name="instance">NLU training client instance.</param>
        public static Task CleanupAsync(this INLUTrainClient instance)
        {
            return instance.CleanupAsync(CancellationToken.None);
        }

        /// <summary>
        /// Tests the NLU model.
        /// </summary>
        /// <returns>Task to await the resulting labeled utterance.</returns>
        /// <param name="instance">NLU testing client instance.</param>
        /// <param name="query">Query to test.</param>
        public static Task<LabeledUtterance> TestAsync(this INLUTestClient instance, JToken query)
        {
            return instance.TestAsync(query, CancellationToken.None);
        }

        /// <summary>
        /// Tests the NLU model using speech.
        /// </summary>
        /// <returns>Task to await the resulting labeled utterance.</returns>
        /// <param name="instance">NLU testing client instance.</param>
        /// <param name="speechFile">Speech file.</param>
        public static Task<LabeledUtterance> TestSpeechAsync(this INLUTestClient instance, string speechFile)
        {
            return instance.TestSpeechAsync(speechFile, null);
        }

        /// <summary>
        /// Tests the NLU model using speech.
        /// </summary>
        /// <returns>Task to await the resulting labeled utterance.</returns>
        /// <param name="instance">NLU testing client instance.</param>
        /// <param name="speechFile">Speech file.</param>
        /// <param name="query">Query to test.</param>
        public static Task<LabeledUtterance> TestSpeechAsync(this INLUTestClient instance, string speechFile, JToken query)
        {
            return instance.TestSpeechAsync(speechFile, query, CancellationToken.None);
        }
    }
}
