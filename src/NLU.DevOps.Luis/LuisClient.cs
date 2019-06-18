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

    internal class LuisClient : ILuisClient
    {
        public const string Protocol = "https://";
        public const string Domain = ".api.cognitive.microsoft.com";

        private readonly ILuisTrainingClient trainingClient;
        private readonly ILuisPredictionClient predictionClient;

        public LuisClient(
            string authoringKey,
            string authoringRegion,
            string endpointKey,
            string endpointRegion,
            AzureSubscriptionInfo azureSubscriptionInfo,
            bool isStaging)
        {
            if (authoringKey != null && authoringRegion != null)
            {
                this.trainingClient = new LuisTrainingClient(authoringKey, authoringRegion, azureSubscriptionInfo, isStaging);
            }

            if (endpointKey != null && endpointRegion != null)
            {
                this.predictionClient = new LuisPredictionClient(endpointKey, endpointRegion);
            }
        }

        public Task<string> CreateAppAsync(string appName, CancellationToken cancellationToken)
        {
            return this.trainingClient?.CreateAppAsync(appName, cancellationToken);
        }

        public Task DeleteAppAsync(string appId, CancellationToken cancellationToken)
        {
            return this.trainingClient?.DeleteAppAsync(appId, cancellationToken);
        }

        public Task<IEnumerable<string>> GetTrainingStatusAsync(string appId, string versionId, CancellationToken cancellationToken)
        {
            return this.trainingClient?.GetTrainingStatusAsync(appId, versionId, cancellationToken);
        }

        public Task ImportVersionAsync(string appId, string versionId, LuisApp luisApp, CancellationToken cancellationToken)
        {
            return this.trainingClient?.ImportVersionAsync(appId, versionId, luisApp, cancellationToken);
        }

        public Task PublishAppAsync(string appId, string versionId, CancellationToken cancellationToken)
        {
            return this.trainingClient?.PublishAppAsync(appId, versionId, cancellationToken);
        }

        public Task TrainAsync(string appId, string versionId, CancellationToken cancellationToken)
        {
            return this.trainingClient?.TrainAsync(appId, versionId, cancellationToken);
        }

        public Task<LuisResult> QueryAsync(string appId, string text, CancellationToken cancellationToken)
        {
            return this.predictionClient?.QueryAsync(appId, text, cancellationToken);
        }

        public virtual Task<LuisResult> RecognizeSpeechAsync(string appId, string speechFile, CancellationToken cancellationToken)
        {
            return this.predictionClient?.RecognizeSpeechAsync(appId, speechFile, cancellationToken);
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected void Dispose(bool disposing)
        {
            if (disposing)
            {
                (this.trainingClient as IDisposable)?.Dispose();
                (this.predictionClient as IDisposable)?.Dispose();
            }
        }
    }
}
