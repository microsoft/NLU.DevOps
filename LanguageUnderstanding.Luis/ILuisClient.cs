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
        Task<HttpResponseMessage> GetAsync(Uri uri, CancellationToken cancellationToken);

        /// <summary>
        /// Sends a GET request for query as an async operation.
        /// </summary>
        /// <returns>A Task to await the HTTP request response.</returns>
        /// <param name="uri">The URI.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <remarks>
        /// Should use the endpoint key instead of the authoring key.
        /// </remarks>
        Task<HttpResponseMessage> QueryAsync(Uri uri, CancellationToken cancellationToken);

        /// <summary>
        /// Sends a POST request as an async operation.
        /// </summary>
        /// <returns>A Task to await the HTTP request response.</returns>
        /// <param name="uri">The URI.</param>
        /// <param name="requestBody">Request body.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task<HttpResponseMessage> PostAsync(Uri uri, string requestBody, CancellationToken cancellationToken);

        /// <summary>
        /// Sends a DELETE request as an async operation.
        /// </summary>
        /// <returns>A Task to await the HTTP request response.</returns>
        /// <param name="uri">The URI.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task<HttpResponseMessage> DeleteAsync(Uri uri, CancellationToken cancellationToken);

        /// <summary>
        /// Performs speech recognition on LUIS using the given audio file.
        /// </summary>
        /// <param name="appId">LUIS app ID.</param>
        /// <param name="speechFile">Path to file.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>JSON string result from LUIS recognition.</returns>
        Task<string> RecognizeSpeechAsync(string appId, string speechFile, CancellationToken cancellationToken);
    }
}
