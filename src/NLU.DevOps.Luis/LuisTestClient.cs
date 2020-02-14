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
    using Microsoft.CognitiveServices.Speech;
    using Microsoft.CognitiveServices.Speech.Audio;
    using Microsoft.CognitiveServices.Speech.Intent;
    using Microsoft.Extensions.Logging;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    internal sealed class LuisTestClient : ILuisTestClient
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

        private static Lazy<ILogger> LazyLogger { get; } = new Lazy<ILogger>(() => ApplicationLogger.LoggerFactory.CreateLogger<LuisNLUTrainClient>());

        private ILuisConfiguration LuisConfiguration { get; }

        private LUISRuntimeClient RuntimeClient { get; }

        public async Task<LuisResult> QueryAsync(string text, CancellationToken cancellationToken)
        {
            while (true)
            {
                try
                {
                    return await this.RuntimeClient.Prediction.ResolveAsync(
                            this.LuisConfiguration.AppId,
                            text,
                            staging: this.LuisConfiguration.IsStaging,
                            cancellationToken: cancellationToken)
                        .ConfigureAwait(false);
                }
                catch (APIErrorException ex)
                when ((int)ex.Response.StatusCode == 429)
                {
                    Logger.LogTrace("Received HTTP 429 result from Cognitive Services. Retrying.");
                    await Task.Delay(ThrottleQueryDelay, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        public Task<SpeechLuisResult> RecognizeSpeechAsync(string speechFile, CancellationToken cancellationToken)
        {
            return this.LuisConfiguration.UseSpeechEndpoint
                ? this.RecognizeSpeechWithEndpointAsync(speechFile, cancellationToken)
                : this.RecognizeSpeechWithIntentRecognizerAsync(speechFile);
        }

        public void Dispose()
        {
            this.RuntimeClient.Dispose();
        }

        private async Task<SpeechLuisResult> RecognizeSpeechWithIntentRecognizerAsync(string speechFile)
        {
            var speechConfig = SpeechConfig.FromSubscription(this.LuisConfiguration.SpeechKey, this.LuisConfiguration.SpeechRegion);
            using (var audioInput = AudioConfig.FromWavFileInput(speechFile))
            using (var recognizer = new IntentRecognizer(speechConfig, audioInput))
            {
                // Add intents to intent recognizer
                var model = LanguageUnderstandingModel.FromAppId(this.LuisConfiguration.AppId);
                recognizer.AddIntent(model, "None", "None");
                var result = await recognizer.RecognizeOnceAsync().ConfigureAwait(false);

                // Checks result.
                // For some reason RecognizeOnceAsync always return ResultReason.RecognizedSpeech
                // when intent is recognized. It's because we don't add all possible intents (note that this IS intentional)
                // in code via AddIntent method.
                if (result.Reason == ResultReason.RecognizedSpeech || result.Reason == ResultReason.RecognizedIntent)
                {
                    var content = result.Properties.GetProperty(PropertyId.LanguageUnderstandingServiceResponse_JsonResult);
                    var luisResult = JsonConvert.DeserializeObject<LuisResult>(content);
                    var speechContent = result.Properties.GetProperty(PropertyId.SpeechServiceResponse_JsonResult);
                    var speechContentJson = JObject.Parse(speechContent);
                    var textScore = speechContentJson["NBest"]?.Max(t => t.Value<double?>("Confidence"));
                    return new SpeechLuisResult(luisResult, textScore);
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

        private async Task<SpeechLuisResult> RecognizeSpeechWithEndpointAsync(string speechFile, CancellationToken cancellationToken)
        {
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

            var speechMatch = responseJson["NBest"].OrderByDescending(t => t.Value<double?>("Confidence") ?? 0.0).First();
            var text = speechMatch.Value<string>("Display");
            var textScore = speechMatch.Value<double?>("Confidence");
            var luisResult = await this.QueryAsync(text, cancellationToken).ConfigureAwait(false);
            return new SpeechLuisResult(luisResult, textScore);
        }
    }
}
