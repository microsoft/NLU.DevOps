// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace NLU.DevOps.Core
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Models;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Base class for NLU test client implementation.
    /// </summary>
    /// <typeparam name="TQuery">Type of test query.</typeparam>
    public abstract class NLUTestClientBase<TQuery> : INLUTestClient
    {
        /// <inheritdoc />
        public Task<LabeledUtterance> TestAsync(JToken query, CancellationToken cancellationToken)
        {
            if (query == null)
            {
                throw new ArgumentNullException(nameof(query));
            }

            var typedQuery = query.ToObject<TQuery>();
            return this.TestAsync(typedQuery, cancellationToken);
        }

        /// <inheritdoc />
        public Task<LabeledUtterance> TestSpeechAsync(string speechFile, JToken query, CancellationToken cancellationToken)
        {
            if (speechFile == null)
            {
                throw new ArgumentNullException(nameof(speechFile));
            }

            if (query != null)
            {
                var typedQuery = query.ToObject<TQuery>();
                return this.TestSpeechAsync(speechFile, typedQuery, cancellationToken);
            }

            return this.TestSpeechAsync(speechFile, default(TQuery), cancellationToken);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Tests the NLU model.
        /// </summary>
        /// <returns>Task to await the resulting labeled utterance.</returns>
        /// <param name="query">Query to test.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        protected abstract Task<LabeledUtterance> TestAsync(TQuery query, CancellationToken cancellationToken);

        /// <summary>
        /// Tests the NLU model using speech.
        /// </summary>
        /// <returns>Task to await the resulting labeled utterance.</returns>
        /// <param name="speechFile">Speech file to test on.</param>
        /// <param name="query">Query to test.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        protected abstract Task<LabeledUtterance> TestSpeechAsync(string speechFile, TQuery query, CancellationToken cancellationToken);

        /// <summary>
        /// Disposes the NLU client.
        /// </summary>
        /// <param name="disposing">
        /// <code>true</code> if disposing, otherwise <code>false</code>.
        /// </param>
        protected abstract void Dispose(bool disposing);
    }
}
