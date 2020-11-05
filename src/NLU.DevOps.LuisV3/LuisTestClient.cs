// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace NLU.DevOps.Luis
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using Logging;
    using Microsoft.Azure.CognitiveServices.Language.LUIS.Runtime;
    using Microsoft.Azure.CognitiveServices.Language.LUIS.Runtime.Models;
    using Microsoft.Extensions.Logging;
    using Newtonsoft.Json.Linq;

    internal sealed class LuisTestClient : ILuisTestClient
    {
        private static readonly TimeSpan ThrottleQueryDelay = TimeSpan.FromMilliseconds(100);

        public LuisTestClient(ILuisConfiguration luisConfiguration)
        {
            this.LuisConfiguration = luisConfiguration ?? throw new ArgumentNullException(nameof(luisConfiguration));
            var endpointCredentials = new ApiKeyServiceClientCredentials(luisConfiguration.PredictionKey);
            this.RuntimeClient = new LUISRuntimeClient(endpointCredentials)
            {
                Endpoint = luisConfiguration.PredictionEndpoint,
            };
        }

        private static ILogger Logger => LazyLogger.Value;

        private static Lazy<ILogger> LazyLogger { get; } = new Lazy<ILogger>(() => ApplicationLogger.LoggerFactory.CreateLogger<LuisNLUTestClient>());

        private ILuisConfiguration LuisConfiguration { get; }

        private LUISRuntimeClient RuntimeClient { get; }

        private bool QueryTargetTraced { get; set; }

        public async Task<PredictionResponse> QueryAsync(PredictionRequest predictionRequest, CancellationToken cancellationToken)
        {
            while (true)
            {
                try
                {
                    this.TraceQueryTarget();
                    if (this.LuisConfiguration.DirectVersionPublish)
                    {
                        return await this.RuntimeClient.Prediction.GetVersionPredictionAsync(
                                Guid.Parse(this.LuisConfiguration.AppId),
                                this.LuisConfiguration.VersionId,
                                predictionRequest,
                                verbose: true,
                                log: false,
                                cancellationToken: cancellationToken)
                            .ConfigureAwait(false);
                    }

                    return await this.RuntimeClient.Prediction.GetSlotPredictionAsync(
                            Guid.Parse(this.LuisConfiguration.AppId),
                            this.LuisConfiguration.SlotName,
                            predictionRequest,
                            verbose: true,
                            log: false,
                            cancellationToken: cancellationToken)
                        .ConfigureAwait(false);
                }
                catch (ErrorException ex)
                when (Retry.IsTransientStatusCode(ex.Response.StatusCode))
                {
                    Logger.LogTrace($"Received HTTP {(int)ex.Response.StatusCode} result from Cognitive Services. Retrying.");
                    await Task.Delay(ThrottleQueryDelay, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        public async Task<SpeechPredictionResponse> RecognizeSpeechAsync(string speechFile, PredictionRequest predictionRequest, CancellationToken cancellationToken)
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
            while (true)
            {
                try
                {
                    using (var fileStream = File.OpenRead(speechFile))
                    using (var requestStream = await request.GetRequestStreamAsync().ConfigureAwait(false))
                    {
                        await fileStream.CopyToAsync(requestStream).ConfigureAwait(false);
                        using (var response = await request.GetResponseAsync().ConfigureAwait(false))
                        using (var streamReader = new StreamReader(response.GetResponseStream()))
                        {
                            var responseText = await streamReader.ReadToEndAsync().ConfigureAwait(false);
                            responseJson = JObject.Parse(responseText);
                            break;
                        }
                    }
                }
                catch (WebException ex)
                when (ex.Response is HttpWebResponse response && Retry.IsTransientStatusCode(response.StatusCode))
                {
                    Logger.LogTrace($"Received HTTP {(int)response.StatusCode} result from Cognitive Services. Retrying.");
                    await Task.Delay(ThrottleQueryDelay, cancellationToken).ConfigureAwait(false);
                }
            }

            if (responseJson.Value<string>("RecognitionStatus") != "Success")
            {
                throw new InvalidOperationException($"Received error from LUIS speech service: {responseJson}");
            }

            var speechMatch = responseJson["NBest"].OrderByDescending(t => t.Value<double?>("Confidence") ?? 0.0).First();
            var text = speechMatch.Value<string>("Display");
            var textScore = speechMatch.Value<double?>("Confidence");

            var speechPredictionRequest = new PredictionRequest
            {
                Query = text,
                DynamicLists = predictionRequest?.DynamicLists,
                ExternalEntities = predictionRequest?.ExternalEntities,
                Options = predictionRequest?.Options,
            };

            var predictionResponse = await this.QueryAsync(speechPredictionRequest, cancellationToken).ConfigureAwait(false);
            return new SpeechPredictionResponse(predictionResponse, textScore);
        }

        public void Dispose()
        {
            this.RuntimeClient.Dispose();
        }

        private void TraceQueryTarget()
        {
            if (!this.QueryTargetTraced)
            {
                this.QueryTargetTraced = true;
                var queryTarget = this.LuisConfiguration.DirectVersionPublish
                    ? $"version '{this.LuisConfiguration.VersionId}'"
                    : $"slot '{this.LuisConfiguration.SlotName}'";

                Logger.LogTrace($"Testing on app '{this.LuisConfiguration.AppId}' {queryTarget}.");
            }
        }
    }
}
