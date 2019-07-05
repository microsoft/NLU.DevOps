// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace NLU.DevOps.Luis
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
    using System.Net.Http;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Logging;
    using Microsoft.Azure.CognitiveServices.Language.LUIS.Authoring;
    using Microsoft.Azure.CognitiveServices.Language.LUIS.Authoring.Models;
    using Microsoft.Azure.CognitiveServices.Language.LUIS.Runtime;
    using Microsoft.Azure.CognitiveServices.Language.LUIS.Runtime.Models;
    using Microsoft.CognitiveServices.Speech;
    using Microsoft.Extensions.Logging;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    internal class LuisClient : ILuisClient
    {
        private const string Protocol = "https://";
        private const string Domain = ".api.cognitive.microsoft.com";

        private static readonly TimeSpan ThrottleQueryDelay = TimeSpan.FromMilliseconds(100);

        public LuisClient(
            string authoringKey,
            string authoringRegion,
            string endpointKey,
            string endpointRegion,
            string speechKey,
            string slotName,
            string versionId,
            AzureSubscriptionInfo azureSubscriptionInfo,
            bool isStaging)
        {
            this.IsStaging = isStaging;
            this.SpeechKey = speechKey ?? throw new ArgumentNullException(nameof(speechKey));

            var endpointOrAuthoringKey = endpointKey ?? authoringKey ?? throw new ArgumentException($"Must specify either '{nameof(authoringKey)}' or '{nameof(endpointKey)}'.");
            this.EndpointRegion = endpointRegion ?? authoringRegion ?? throw new ArgumentException($"Must specify either '{nameof(authoringRegion)}' or '{nameof(endpointRegion)}'.");
            this.AzureSubscriptionInfo = azureSubscriptionInfo;
            this.AuthoringKey = authoringKey;

            this.SlotName = slotName;
            this.VersionId = versionId;
            if (slotName == null && versionId == null)
            {
                throw new ArgumentException($"Either '{nameof(slotName)}' or '{nameof(versionId)}' must not be null.");
            }

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

        private AzureSubscriptionInfo AzureSubscriptionInfo { get; }

        private string AuthoringKey { get; }

        private bool IsStaging { get; }

        private string SpeechEndpoint { get; }

        private string SpeechKey { get; }

        private string SlotName { get; }

        private string VersionId { get; }

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

            // Creating LUIS app.
            var appId = await this.AuthoringClient.Apps.AddAsync(request, cancellationToken).ConfigureAwait(false);

            // Assign Azure resource to LUIS app.
            if (this.AzureSubscriptionInfo != null)
            {
                await this.AssignAzureResourceAsync(appId).ConfigureAwait(false);
            }

            return appId.ToString();
        }

        public Task DeleteAppAsync(string appId, CancellationToken cancellationToken)
        {
            return this.AuthoringClient.Apps.DeleteAsync(Guid.Parse(appId), cancellationToken: cancellationToken);
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
                VersionId = versionId,
            };

            return this.AuthoringClient.Apps.PublishAsync(Guid.Parse(appId), request, cancellationToken);
        }

        public async Task<PredictionResponse> QueryAsync(string appId, PredictionRequest predictionRequest, CancellationToken cancellationToken)
        {
            while (true)
            {
                try
                {
                    return await (this.SlotName != null
                        ? this.RuntimeClient.Prediction.GetSlotPredictionAsync(Guid.Parse(appId), this.SlotName, predictionRequest, verbose: true, cancellationToken: cancellationToken).ConfigureAwait(false)
                        : this.RuntimeClient.Prediction.GetVersionPredictionAsync(Guid.Parse(appId), this.VersionId, predictionRequest, verbose: true, cancellationToken: cancellationToken).ConfigureAwait(false));
                }
                catch (ErrorException ex)
                when ((int)ex.Response.StatusCode == 429)
                {
                    Logger.LogWarning("Received HTTP 429 result from Cognitive Services. Retrying.");
                    await Task.Delay(ThrottleQueryDelay, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        public async Task<PredictionResponse> RecognizeSpeechAsync(string appId, string speechFile, PredictionRequest predictionRequest, CancellationToken cancellationToken)
        {
            var request = (HttpWebRequest)WebRequest.Create(this.SpeechEndpoint);
            request.Method = "POST";
            request.ContentType = "audio/wav; codec=audio/pcm; samplerate=16000";
            request.ServicePoint.Expect100Continue = true;
            request.SendChunked = true;
            request.Accept = "application/json";
            request.Headers.Add("Ocp-Apim-Subscription-Key", this.SpeechKey);

            JObject responseJson;
            using (var fileStream = File.OpenRead(speechFile))
            using (var requestStream = await request.GetRequestStreamAsync().ConfigureAwait(false))
            {
                await fileStream.CopyToAsync(requestStream).ConfigureAwait(false);
                using (var response = await request.GetResponseAsync().ConfigureAwait(false))
                using (var streamReader = new StreamReader(response.GetResponseStream()))
                {
                    var responseText = await streamReader.ReadToEndAsync().ConfigureAwait(false);
                    responseJson = JObject.Parse(responseText);
                }
            }

            if (responseJson.Value<string>("RecognitionStatus") != "Success")
            {
                throw new InvalidOperationException($"Received error from LUIS speech service: {responseJson}");
            }

            var speechPredictionRequest = new PredictionRequest
            {
                Query = responseJson.Value<string>("DisplayText"),
                DynamicLists = predictionRequest?.DynamicLists,
                ExternalEntities = predictionRequest?.ExternalEntities,
                Options = predictionRequest?.Options,
            };

            return await this.QueryAsync(appId, predictionRequest, cancellationToken).ConfigureAwait(false);
        }

        public Task TrainAsync(string appId, string versionId, CancellationToken cancellationToken)
        {
            return this.AuthoringClient.Train.TrainVersionAsync(Guid.Parse(appId), versionId, cancellationToken);
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
                using (this.AuthoringClient)
                using (this.RuntimeClient)
                {
                }
            }
        }

        private async Task AssignAzureResourceAsync(Guid appId)
        {
            var jsonBody = JsonConvert.SerializeObject(this.AzureSubscriptionInfo);
            var data = new StringContent(jsonBody, Encoding.UTF8, "application/json");
            var url = $"{this.AuthoringClient.Endpoint}/luis/api/v2.0/apps/{appId}/azureaccounts";
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri(url),
                Headers =
                {
                    { HttpRequestHeader.Authorization.ToString(), $"Bearer {this.AzureSubscriptionInfo.ArmToken}" },
                    { "Ocp-Apim-Subscription-Key", this.AuthoringKey }
                },
                Content = data,
            };

            var result = await this.AuthoringClient.HttpClient.SendAsync(request).ConfigureAwait(false);
            result.EnsureSuccessStatusCode();
        }
    }
}
