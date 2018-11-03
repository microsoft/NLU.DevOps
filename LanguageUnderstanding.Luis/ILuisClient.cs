// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace LanguageUnderstanding.Luis
{
    using System;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Interface for LUIS http utility methods.
    /// </summary>
    public interface ILuisClient : IDisposable
    {
        /// <summary>
        /// Sends a GET request as an async operation.
        /// </summary>
        /// <returns>A Task to await the HTTP request response.</returns>
        /// <param name="uri">The URI.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task<HttpResponseMessage> GetAsync(string uri, CancellationToken cancellationToken);

        /// <summary>
        /// Sends a POST request as an async operation.
        /// </summary>
        /// <returns>A Task to await the HTTP request response.</returns>
        /// <param name="uri">The URI.</param>
        /// <param name="requestBody">Request body.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task<HttpResponseMessage> PostAsync(string uri, string requestBody, CancellationToken cancellationToken);

        /// <summary>
        /// Sends a DELETE request as an async operation.
        /// </summary>
        /// <returns>A Task to await the HTTP request response.</returns>
        /// <param name="uri">The URI.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task<HttpResponseMessage> DeleteAsync(string uri, CancellationToken cancellationToken);
    }
}
