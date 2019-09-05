// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace NLU.DevOps.Models
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// NLU testing interface.
    /// </summary>
    public interface INLUTestClient : IDisposable
    {
        /// <summary>
        /// Tests the NLU model.
        /// </summary>
        /// <returns>Task to await the resulting labeled utterance.</returns>
        /// <param name="query">Query to test.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task<LabeledUtterance> TestAsync(
            JToken query,
            CancellationToken cancellationToken);

        /// <summary>
        /// Tests the NLU model using speech.
        /// </summary>
        /// <returns>Task to await the resulting labeled utterance.</returns>
        /// <param name="speechFile">Speech file to test on.</param>
        /// <param name="query">Query to test.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task<LabeledUtterance> TestSpeechAsync(
            string speechFile,
            JToken query,
            CancellationToken cancellationToken);
    }
}
