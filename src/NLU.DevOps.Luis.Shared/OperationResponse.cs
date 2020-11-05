// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace NLU.DevOps.Luis
{
    using System.Net;

    /// <summary>
    /// Factory methods for <see cref="OperationResponse{T}"/>.
    /// </summary>
    public static class OperationResponse
    {
        /// <summary>
        /// Creates an instance of <see cref="OperationResponse{T}"/>.
        /// </summary>
        /// <typeparam name="T">Type of response value.</typeparam>
        /// <param name="value">Response value.</param>
        /// <param name="retryAfter">HTTP 'Retry-After' header.</param>
        /// <param name="operationLocation">HTTP 'Operation-Location' header.</param>
        /// <returns>Instance of <see cref="OperationResponse{T}"/>.</returns>
        public static OperationResponse<T> Create<T>(T value, string retryAfter = null, string operationLocation = null)
        {
            return new OperationResponse<T>(value, retryAfter, operationLocation);
        }

        /// <summary>
        /// Creates an instance of <see cref="OperationResponse{T}"/>.
        /// </summary>
        /// <typeparam name="T">Type of response value.</typeparam>
        /// <param name="value">Response value.</param>
        /// <param name="response">HTTP response.</param>
        /// <returns>Instance of <see cref="OperationResponse{T}"/>.</returns>
        public static OperationResponse<T> Create<T>(T value, HttpWebResponse response)
        {
            var retryAfter = response?.Headers?[HttpResponseHeader.RetryAfter];
            var operationLocation = response?.Headers?["Operation-Location"];
            return Create(value, retryAfter, operationLocation);
        }
    }
}
