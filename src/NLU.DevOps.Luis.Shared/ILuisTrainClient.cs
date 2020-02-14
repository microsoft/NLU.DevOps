// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace NLU.DevOps.Luis
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.CognitiveServices.Language.LUIS.Authoring.Models;

    /// <summary>
    /// LUIS interface for training operations.
    /// </summary>
    public interface ILuisTrainClient : IDisposable
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
        /// Deletes the LUIS app version.
        /// </summary>
        /// <returns>Task to await the delete operation.</returns>
        /// <param name="appId">LUIS app ID.</param>
        /// <param name="versionId">LUIS version ID.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task DeleteVersionAsync(string appId, string versionId, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the training status for the LUIS app version.
        /// </summary>
        /// <returns>Task to await the training status response.</returns>
        /// <param name="appId">LUIS app ID.</param>
        /// <param name="versionId">LUIS version ID.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task<IList<ModelTrainingInfo>> GetTrainingStatusAsync(string appId, string versionId, CancellationToken cancellationToken);

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
        /// Trains the LUIS app version.
        /// </summary>
        /// <returns>Task to await the train operation.</returns>
        /// <param name="appId">LUIS app identifier.</param>
        /// <param name="versionId">LUIS version ID.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task TrainAsync(string appId, string versionId, CancellationToken cancellationToken);
    }
}
