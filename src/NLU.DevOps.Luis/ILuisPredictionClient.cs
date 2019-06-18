// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace NLU.DevOps.Luis
{
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.CognitiveServices.Language.LUIS.Runtime.Models;

    /// <summary>
    /// Interface for LUIS Runtime operations
    /// </summary>
    public interface ILuisPredictionClient
    {
        /// <summary>
        /// Queries the LUIS app to extract intent and entities.
        /// </summary>
        /// <returns>Task to await the LUIS results.</returns>
        /// <param name="appId">LUIS app ID.</param>
        /// <param name="text">Query text.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task<LuisResult> QueryAsync(string appId, string text, CancellationToken cancellationToken);

        /// <summary>
        /// Performs intent recognition from speech using the given audio file.
        /// </summary>
        /// <returns>Task to await the LUIS results.</returns>
        /// <param name="appId">LUIS app ID.</param>
        /// <param name="speechFile">Path to file.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task<LuisResult> RecognizeSpeechAsync(string appId, string speechFile, CancellationToken cancellationToken);
    }
}