// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace NLU.DevOps.Models
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// NLU batch testing interface.
    /// </summary>
    public interface INLUBatchTestClient
    {
        /// <summary>
        /// Gets a value indicating whether batch testing is enabled.
        /// </summary>
        bool IsBatchEnabled { get; }

        /// <summary>
        /// Tests the NLU model with a batch of queries.
        /// </summary>
        /// <param name="queries">Queries to test.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Task to await the resulting labeled utterances.</returns>
        Task<IEnumerable<ILabeledUtterance>> TestAsync(
            IEnumerable<JToken> queries,
            CancellationToken cancellationToken);
    }
}
