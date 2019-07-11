// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace NLU.DevOps.Lex
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Amazon.Lex.Model;

    /// <summary>
    /// Lex client interface for testing operations.
    /// </summary>
    public interface ILexTestClient : IDisposable
    {
        /// <summary>
        /// Calls the 'PostContent' action on Lex.
        /// </summary>
        /// <returns>Task to await the bot response.</returns>
        /// <param name="request">Request.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task<PostContentResponse> PostContentAsync(PostContentRequest request, CancellationToken cancellationToken);

        /// <summary>
        /// Calls the 'PostText' action on Lex.
        /// </summary>
        /// <returns>Task to await the bot response.</returns>
        /// <param name="request">Request.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task<PostTextResponse> PostTextAsync(PostTextRequest request, CancellationToken cancellationToken);
    }
}
