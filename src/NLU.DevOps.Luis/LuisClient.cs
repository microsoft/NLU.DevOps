// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace NLU.DevOps.Luis
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Logging;
    using Microsoft.Azure.CognitiveServices.Language.LUIS.Authoring.Models;
    using Microsoft.Azure.CognitiveServices.Language.LUIS.Runtime;
    using Microsoft.Azure.CognitiveServices.Language.LUIS.Runtime.Models;
    using Microsoft.CognitiveServices.Speech;
    using Microsoft.CognitiveServices.Speech.Audio;
    using Microsoft.CognitiveServices.Speech.Intent;
    using Microsoft.Extensions.Logging;
    using Newtonsoft.Json;

    internal class LuisClient : ILuisClient
    {
        public const string Protocol = "https://";
        public const string Domain = ".api.cognitive.microsoft.com";

        private static readonly TimeSpan ThrottleQueryDelay = TimeSpan.FromMilliseconds(100);
        private readonly ILuisTrainingClient trainingClient;

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

            var endpointCredentials = new ApiKeyServiceClientCredentials(endpointKey);
            this.RuntimeClient = new LUISRuntimeClient(endpointCredentials)
            {
                Endpoint = $"{Protocol}{endpointRegion}{Domain}",
            };

            this.LazySpeechConfig = new Lazy<SpeechConfig>(() => SpeechConfig.FromSubscription(endpointKey, endpointRegion));
        }

        private static ILogger Logger => LazyLogger.Value;

        private static Lazy<ILogger> LazyLogger { get; } = new Lazy<ILogger>(() => ApplicationLogger.LoggerFactory.CreateLogger<LuisNLUService>());

        private LUISRuntimeClient RuntimeClient { get; }

        private Lazy<SpeechConfig> LazySpeechConfig { get; }

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

        public async Task<LuisResult> QueryAsync(string appId, string text, CancellationToken cancellationToken)
        {
            while (true)
            {
                try
                {
                    return await this.RuntimeClient.Prediction.ResolveAsync(appId, text, cancellationToken: cancellationToken).ConfigureAwait(false);
                }
                catch (APIErrorException ex)
                when ((int)ex.Response.StatusCode == 429)
                {
                    Logger.LogWarning("Received HTTP 429 result from Cognitive Services. Retrying.");
                    await Task.Delay(ThrottleQueryDelay, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        public virtual async Task<LuisResult> RecognizeSpeechAsync(string appId, string speechFile, CancellationToken cancellationToken)
        {
            using (var audioInput = AudioConfig.FromWavFileInput(speechFile))
            using (var recognizer = new IntentRecognizer(this.LazySpeechConfig.Value, audioInput))
            {
                // Add intents to intent recognizer
                var model = LanguageUnderstandingModel.FromAppId(appId);
                recognizer.AddIntent(model, "None", "None");
                var result = await recognizer.RecognizeOnceAsync().ConfigureAwait(false);

                // Checks result.
                // For some reason RecognizeOnceAsync always return ResultReason.RecognizedSpeech
                // when intent is recognized. It's because we don't add all possible intents (note that this IS intentional)
                // in code via AddIntent method.
                if (result.Reason == ResultReason.RecognizedSpeech || result.Reason == ResultReason.RecognizedIntent)
                {
                    var content = result.Properties.GetProperty(PropertyId.LanguageUnderstandingServiceResponse_JsonResult);
                    return JsonConvert.DeserializeObject<LuisResult>(content);
                }
                else if (result.Reason == ResultReason.NoMatch)
                {
                    Logger.LogWarning("Received 'NoMatch' result from Cognitive Services.");
                    return null;
                }
                else
                {
                    throw new InvalidOperationException($"Failed to get speech recognition result. Reason = '{result.Reason}'");
                }
            }
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

                using (this.RuntimeClient)
                {
                }
            }
        }
    }
}
