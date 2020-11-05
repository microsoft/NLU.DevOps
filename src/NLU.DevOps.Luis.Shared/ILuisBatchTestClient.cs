// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace NLU.DevOps.Luis
{
    using System.Threading;
    using System.Threading.Tasks;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// LUIS interface for testing operations.
    /// </summary>
    public interface ILuisBatchTestClient
    {
        /// <summary>
        /// Starts a batch evaluation of LUIS queries.
        /// </summary>
        /// <param name="batchInput">Batch input JSON.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task to await the operation ID.</returns>
        Task<OperationResponse<string>> CreateEvaluationsOperationAsync(JToken batchInput, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the batch evaluation operation status.
        /// </summary>
        /// <param name="operationId">Operation ID.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task to await the operation status.</returns>
        Task<OperationResponse<string>> GetEvaluationsStatusAsync(string operationId, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the batch evaluation results.
        /// </summary>
        /// <param name="operationId">Operation ID.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task to await the batch evaluation result.</returns>
        Task<JToken> GetEvaluationsResultAsync(string operationId, CancellationToken cancellationToken);
    }
}
