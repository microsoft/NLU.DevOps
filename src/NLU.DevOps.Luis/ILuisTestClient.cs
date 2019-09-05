// Copyright (c) Microsoft Corporation.
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
        /// <param name="text">Query text.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task<LuisResult> QueryAsync(string text, CancellationToken cancellationToken);

        /// <summary>
        /// Performs intent recognition from speech using the given audio file.
        /// </summary>
        /// <returns>Task to await the LUIS results.</returns>
        /// <param name="speechFile">Path to file.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task<SpeechLuisResult> RecognizeSpeechAsync(string speechFile, CancellationToken cancellationToken);
    }
}
