// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace NLU.DevOps.Luis
{
    using System.Linq;
    using System.Net.Http;

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
        /// <param name="response">HTTP response.</param>
        /// <returns>Instance of <see cref="OperationResponse{T}"/>.</returns>
        public static OperationResponse<T> Create<T>(T value, HttpResponseMessage response = default)
        {
            var retryAfter = response?.Headers?.GetValues(Retry.RetryAfterHeader).FirstOrDefault();
            return new OperationResponse<T>(value, retryAfter);
        }
    }
}
