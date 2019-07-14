// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace NLU.DevOps.Luis
{
    using System;
    using System.IO;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using Logging;
    using Microsoft.Azure.CognitiveServices.Language.LUIS.Runtime;
    using Microsoft.Azure.CognitiveServices.Language.LUIS.Runtime.Models;
    using Microsoft.Extensions.Logging;
    using Newtonsoft.Json.Linq;

    internal class LuisTestClient : ILuisTestClient
    {
        private const string Protocol = "https://";
        private const string Domain = ".api.cognitive.microsoft.com";

        private static readonly TimeSpan ThrottleQueryDelay = TimeSpan.FromMilliseconds(100);

        public LuisTestClient(ILuisConfiguration luisConfiguration)
        {
            this.LuisConfiguration = luisConfiguration ?? throw new ArgumentNullException(nameof(luisConfiguration));
            var endpointCredentials = new ApiKeyServiceClientCredentials(luisConfiguration.EndpointKey);
            this.RuntimeClient = new LUISRuntimeClient(endpointCredentials)
            {
                Endpoint = $"{Protocol}{luisConfiguration.EndpointRegion}{Domain}",
            };
        }

        private static ILogger Logger => LazyLogger.Value;

        private static Lazy<ILogger> LazyLogger { get; } = new Lazy<ILogger>(() => ApplicationLogger.LoggerFactory.CreateLogger<LuisNLUTestClient>());

        private ILuisConfiguration LuisConfiguration { get; }

        private LUISRuntimeClient RuntimeClient { get; }

        public async Task<PredictionResponse> QueryAsync(PredictionRequest predictionRequest, CancellationToken cancellationToken)
        {
            while (true)
            {
                try
                {
                    if (this.LuisConfiguration.DirectVersionPublish)
                    {
                        Logger.LogTrace($"Testing on app '{this.LuisConfiguration.AppId}' version '{this.LuisConfiguration.VersionId}'.");
                        return await this.RuntimeClient.Prediction.GetVersionPredictionAsync(
                                Guid.Parse(this.LuisConfiguration.AppId),
                                this.LuisConfiguration.VersionId,
                                predictionRequest,
                                verbose: true,
                                cancellationToken: cancellationToken)
                            .ConfigureAwait(false);
                    }

                    Logger.LogTrace($"Testing on app '{this.LuisConfiguration.AppId}' slot '{this.LuisConfiguration.SlotName}'.");
                    return await this.RuntimeClient.Prediction.GetSlotPredictionAsync(
                            Guid.Parse(this.LuisConfiguration.AppId),
                            this.LuisConfiguration.SlotName,
                            predictionRequest,
                            verbose: true,
                            cancellationToken: cancellationToken)
                        .ConfigureAwait(false);
                }
                catch (ErrorException ex)
                when ((int)ex.Response.StatusCode == 429)
                {
                    Logger.LogWarning("Received HTTP 429 result from Cognitive Services. Retrying.");
                    await Task.Delay(ThrottleQueryDelay, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        public async Task<PredictionResponse> RecognizeSpeechAsync(string speechFile, PredictionRequest predictionRequest, CancellationToken cancellationToken)
        {
            if (this.LuisConfiguration.SpeechKey == null)
            {
                throw new InvalidOperationException("Must provide speech key to perform speech intent recognition.");
            }

            var request = (HttpWebRequest)WebRequest.Create(this.LuisConfiguration.SpeechEndpoint);
            request.Method = "POST";
            request.ContentType = "audio/wav; codec=audio/pcm; samplerate=16000";
            request.ServicePoint.Expect100Continue = true;
            request.SendChunked = true;
            request.Accept = "application/json";
            request.Headers.Add("Ocp-Apim-Subscription-Key", this.LuisConfiguration.SpeechKey);

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

            return await this.QueryAsync(predictionRequest, cancellationToken).ConfigureAwait(false);
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
                this.RuntimeClient.Dispose();
            }
        }
    }
}
