// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace NLU.DevOps.Luis
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.CognitiveServices.Language.LUIS.Runtime.Models;

    /// <summary>
    /// LUIS interface for testing operations.
    /// </summary>
    public interface ILuisTestClient : IDisposable
    {
        /// <summary>
        /// Queries the LUIS app to extract intent and entities.
        /// </summary>
        /// <returns>Task to await the LUIS results.</returns>
        /// <param name="predictionRequest">Prediction request.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task<PredictionResponse> QueryAsync(PredictionRequest predictionRequest, CancellationToken cancellationToken);

        /// <summary>
        /// Performs intent recognition from speech using the given audio file.
        /// </summary>
        /// <returns>Task to await the LUIS results.</returns>
        /// <param name="speechFile">Path to file.</param>
        /// <param name="predictionRequest">Prediction request.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task<PredictionResponse> RecognizeSpeechAsync(string speechFile, PredictionRequest predictionRequest, CancellationToken cancellationToken);
    }
}
