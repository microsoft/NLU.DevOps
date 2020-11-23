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

        public Task<PredictionResponse> QueryAsync(PredictionRequest predictionRequest, CancellationToken cancellationToken)
        {
            this.TraceQueryTarget();
            return Retry.With(cancellationToken).OnTransientErrorAsync(() =>
                this.LuisConfiguration.DirectVersionPublish
                    ? this.RuntimeClient.Prediction.GetVersionPredictionAsync(
                        Guid.Parse(this.LuisConfiguration.AppId),
                        this.LuisConfiguration.VersionId,
                        predictionRequest,
                        verbose: true,
                        log: false,
                        cancellationToken: cancellationToken)
                    : this.RuntimeClient.Prediction.GetSlotPredictionAsync(
                            Guid.Parse(this.LuisConfiguration.AppId),
                            this.LuisConfiguration.SlotName,
                            predictionRequest,
                            verbose: true,
                            log: false,
                            cancellationToken: cancellationToken));
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

            var jsonPayload = await Retry.With(cancellationToken).OnTransientWebExceptionAsync(async () =>
                    {
                        using (var fileStream = File.OpenRead(speechFile))
                        using (var requestStream = await request.GetRequestStreamAsync().ConfigureAwait(false))
                        {
                            await fileStream.CopyToAsync(requestStream).ConfigureAwait(false);
                            using (var response = await request.GetResponseAsync().ConfigureAwait(false))
                            using (var streamReader = new StreamReader(response.GetResponseStream()))
                            {
                                return await streamReader.ReadToEndAsync().ConfigureAwait(false);
                            }
                        }
                    })
                .ConfigureAwait(false);

            var responseJson = JObject.Parse(jsonPayload);
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
