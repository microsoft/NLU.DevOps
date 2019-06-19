// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace NLU.DevOps.Luis
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.CognitiveServices.Language.LUIS.Authoring.Models;
    using Microsoft.Azure.CognitiveServices.Language.LUIS.Runtime.Models;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Interface for LUIS operations.
    /// </summary>
    public interface ILuisClient : IDisposable
    {
        /// <summary>
        /// Creates the LUIS app.
        /// </summary>
        /// <returns>Task to await the LUIS app ID of the newly created app.</returns>
        /// <param name="appName">LUIS app name.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task<string> CreateAppAsync(string appName, CancellationToken cancellationToken);

        /// <summary>
        /// Deletes the LUIS app.
        /// </summary>
        /// <returns>Task to await the delete operation.</returns>
        /// <param name="appId">LUIS app ID.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task DeleteAppAsync(string appId, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the training status for the LUIS app version.
        /// </summary>
        /// <returns>Task to await the training status response.</returns>
        /// <param name="appId">LUIS app ID.</param>
        /// <param name="versionId">LUIS version ID.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task<ModelTrainingStatus> GetTrainingStatusAsync(string appId, string versionId, CancellationToken cancellationToken);

        /// <summary>
        /// Imports the LUIS app version.
        /// </summary>
        /// <returns>Task to await the import operation.</returns>
        /// <param name="appId">LUIS app ID.</param>
        /// <param name="versionId">LUIS version ID.</param>
        /// <param name="luisApp">LUIS app to import.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task ImportVersionAsync(string appId, string versionId, LuisApp luisApp, CancellationToken cancellationToken);

        /// <summary>
        /// Publishes the LUIS app version.
        /// </summary>
        /// <returns>Task to await the publish operation.</returns>
        /// <param name="appId">LUIS app ID.</param>
        /// <param name="versionId">LUIS version ID.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task PublishAppAsync(string appId, string versionId, CancellationToken cancellationToken);

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

        /// <summary>
        /// Trains the LUIS app version.
        /// </summary>
        /// <returns>Task to await the train operation.</returns>
        /// <param name="appId">LUIS app identifier.</param>
        /// <param name="versionId">LUIS version ID.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task TrainAsync(string appId, string versionId, CancellationToken cancellationToken);
    }
}