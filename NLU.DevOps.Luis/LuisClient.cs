// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace NLU.DevOps.Luis
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Logging;
    using Microsoft.Azure.CognitiveServices.Language.LUIS.Authoring;
    using Microsoft.Azure.CognitiveServices.Language.LUIS.Authoring.Models;
    using Microsoft.Azure.CognitiveServices.Language.LUIS.Runtime;
    using Microsoft.Azure.CognitiveServices.Language.LUIS.Runtime.Models;
    using Microsoft.CognitiveServices.Speech;
    using Microsoft.CognitiveServices.Speech.Audio;
    using Microsoft.CognitiveServices.Speech.Intent;
    using Microsoft.Extensions.Logging;
    using Newtonsoft.Json;

    internal sealed class LuisClient : ILuisClient
    {
        private const string Protocol = "https://";
        private const string Domain = ".api.cognitive.microsoft.com";

        private static readonly TimeSpan ThrottleQueryDelay = TimeSpan.FromMilliseconds(100);

        public LuisClient(
            string authoringKey,
            string authoringRegion,
            string endpointKey,
            string endpointRegion,
            bool isStaging)
        {
            this.IsStaging = isStaging;

            var endpointOrAuthoringKey = endpointKey ?? authoringKey ?? throw new ArgumentException($"Must specify either '{nameof(authoringKey)}' or '{nameof(endpointKey)}'.");
            this.EndpointRegion = endpointRegion ?? authoringRegion ?? throw new ArgumentException($"Must specify either '{nameof(authoringRegion)}' or '{nameof(endpointRegion)}'.");
            var endpointCredentials = new Microsoft.Azure.CognitiveServices.Language.LUIS.Runtime.ApiKeyServiceClientCredentials(endpointOrAuthoringKey);
            this.RuntimeClient = new LUISRuntimeClient(endpointCredentials)
            {
                Endpoint = $"{Protocol}{this.EndpointRegion}{Domain}",
            };

            this.LazyAuthoringClient = new Lazy<LUISAuthoringClient>(() =>
            {
                if (authoringKey == null || authoringRegion == null)
                {
                    throw new InvalidOperationException("Must provide authoring key and region to perform authoring operations.");
                }

                var authoringCredentials = new Microsoft.Azure.CognitiveServices.Language.LUIS.Authoring.ApiKeyServiceClientCredentials(authoringKey);
                return new LUISAuthoringClient(authoringCredentials)
                {
                    Endpoint = $"{Protocol}{authoringRegion}{Domain}",
                };
            });

            this.LazySpeechConfig = new Lazy<SpeechConfig>(() => SpeechConfig.FromSubscription(endpointKey, endpointRegion));
        }

        private static ILogger Logger => LazyLogger.Value;

        private static Lazy<ILogger> LazyLogger { get; } = new Lazy<ILogger>(() => ApplicationLogger.LoggerFactory.CreateLogger<LuisNLUService>());

        private string EndpointRegion { get; }

        private bool IsStaging { get; }

        private LUISRuntimeClient RuntimeClient { get; }

        private LUISAuthoringClient AuthoringClient => this.LazyAuthoringClient.Value;

        private Lazy<LUISAuthoringClient> LazyAuthoringClient { get; }

        private Lazy<SpeechConfig> LazySpeechConfig { get; }

        public async Task<string> CreateAppAsync(string appName, CancellationToken cancellationToken)
        {
            var request = new ApplicationCreateObject
            {
                Name = appName,
                Culture = "en-us",
            };

            var appId = await this.AuthoringClient.Apps.AddAsync(request, cancellationToken).ConfigureAwait(false);
            return appId.ToString();
        }

        public Task DeleteAppAsync(string appId, CancellationToken cancellationToken)
        {
            return this.AuthoringClient.Apps.DeleteAsync(Guid.Parse(appId), cancellationToken);
        }

        public Task<IList<ModelTrainingInfo>> GetTrainingStatusAsync(string appId, string versionId, CancellationToken cancellationToken)
        {
            return this.AuthoringClient.Train.GetStatusAsync(Guid.Parse(appId), versionId, cancellationToken);
        }

        public Task ImportVersionAsync(string appId, string versionId, LuisApp luisApp, CancellationToken cancellationToken)
        {
            return this.AuthoringClient.Versions.ImportAsync(Guid.Parse(appId), luisApp, versionId, cancellationToken);
        }

        public Task PublishAppAsync(string appId, string versionId, CancellationToken cancellationToken)
        {
            var request = new ApplicationPublishObject
            {
                IsStaging = this.IsStaging,
                Region = this.EndpointRegion,
                VersionId = versionId,
            };

            return this.AuthoringClient.Apps.PublishAsync(Guid.Parse(appId), request, cancellationToken);
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

        public async Task<LuisResult> RecognizeSpeechAsync(string appId, string speechFile, CancellationToken cancellationToken)
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

        public Task TrainAsync(string appId, string versionId, CancellationToken cancellationToken)
        {
            return this.AuthoringClient.Train.TrainVersionAsync(Guid.Parse(appId), versionId, cancellationToken);
        }

        public void Dispose()
        {
            using (this.AuthoringClient)
            using (this.RuntimeClient)
            {
            }
        }
    }
}
