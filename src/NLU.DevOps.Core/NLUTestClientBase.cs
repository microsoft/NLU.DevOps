// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace NLU.DevOps.Core
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Models;

    /// <summary>
    /// Base class for NLU test client implementation.
    /// </summary>
    /// <typeparam name="TQuery">Type of test query.</typeparam>
    public abstract class NLUTestClientBase<TQuery> : INLUTestClient
        where TQuery : INLUQuery
    {
        /// <inheritdoc />
        public Task<LabeledUtterance> TestAsync(INLUQuery query, CancellationToken cancellationToken)
        {
            if (query == null)
            {
                throw new ArgumentNullException(nameof(query));
            }

            if (query is TQuery typedQuery)
            {
                return this.TestAsync(typedQuery, cancellationToken);
            }

            throw new ArgumentException($"Expected query of type '{typeof(TQuery)}.'", nameof(query));
        }

        /// <inheritdoc />
        public Task<LabeledUtterance> TestSpeechAsync(string speechFile, INLUQuery query, CancellationToken cancellationToken)
        {
            if (speechFile == null)
            {
                throw new ArgumentNullException(nameof(speechFile));
            }

            if (query is TQuery typedQuery)
            {
                return this.TestSpeechAsync(speechFile, typedQuery, cancellationToken);
            }

            if (query == null)
            {
                return this.TestSpeechAsync(speechFile, default(TQuery), cancellationToken);
            }

            throw new ArgumentException($"Expected query of type '{typeof(TQuery)}.'", nameof(query));
        }

        /// <inheritdoc />
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Tests the NLU service.
        /// </summary>
        /// <returns>Task to await the resulting labeled utterance.</returns>
        /// <param name="query">Query to test.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        protected abstract Task<LabeledUtterance> TestAsync(TQuery query, CancellationToken cancellationToken);

        /// <summary>
        /// Tests the NLU service using speech.
        /// </summary>
        /// <returns>Task to await the resulting labeled utterance.</returns>
        /// <param name="speechFile">Speech file to test on.</param>
        /// <param name="query">Query to test.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        protected abstract Task<LabeledUtterance> TestSpeechAsync(string speechFile, TQuery query, CancellationToken cancellationToken);

        /// <summary>
        /// Disposes the NLU service.
        /// </summary>
        /// <param name="disposing">
        /// <code>true</code> if disposing, otherwise <code>false</code>.
        /// </param>
        protected abstract void Dispose(bool disposing);
    }
}
