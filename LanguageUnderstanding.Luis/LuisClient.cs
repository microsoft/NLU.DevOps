// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace LanguageUnderstanding.Luis
{
    using System;
    using System.Net;
    using System.Net.Http;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using LanguageUnderstanding.Logging;
    using Microsoft.CognitiveServices.Speech;
    using Microsoft.CognitiveServices.Speech.Audio;
    using Microsoft.CognitiveServices.Speech.Intent;
    using Microsoft.Extensions.Logging;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    internal sealed class LuisClient : ILuisClient
    {
        private const string SubscriptionKeyHeader = "Ocp-Apim-Subscription-Key";

        private const string Protocol = "https://";

        private const string Domain = ".api.cognitive.microsoft.com";

        private const string BasePath = "/luis/api/v2.0/apps/";

        private static readonly TimeSpan ThrottleQueryDelay = TimeSpan.FromMilliseconds(100);

        public LuisClient(
            string authoringKey,
            string authoringRegion,
            string endpointKey,
            string endpointRegion,
            bool isStaging)
        {
            this.AuthoringRegion = authoringRegion;
            this.EndpointKey = endpointKey ?? authoringKey ?? throw new ArgumentException($"Must specify either '{nameof(authoringKey)}' or '{nameof(endpointKey)}'.");
            this.EndpointRegion = endpointRegion ?? authoringRegion ?? throw new ArgumentException($"Must specify either '{nameof(authoringRegion)}' or '{nameof(endpointRegion)}'.");
            this.IsStaging = isStaging;

            this.HttpClient = new HttpClient
            {
                DefaultRequestHeaders =
                {
                    { SubscriptionKeyHeader, authoringKey },
                },
            };

            this.LazySpeechConfig = new Lazy<SpeechConfig>(() => SpeechConfig.FromSubscription(endpointKey, endpointRegion));
        }

        private static ILogger Logger => LazyLogger.Value;

        private static Lazy<ILogger> LazyLogger { get; } = new Lazy<ILogger>(() => ApplicationLogger.LoggerFactory.CreateLogger<LuisLanguageUnderstandingService>());

        private string AuthoringRegion { get; }

        private string EndpointKey { get; }

        private string EndpointRegion { get; }

        private bool IsStaging { get; }

        private HttpClient HttpClient { get; }

        private Lazy<SpeechConfig> LazySpeechConfig { get; }

        private string AuthoringHost => $"{Protocol}{this.AuthoringRegion}{Domain}";

        public async Task<string> CreateAppAsync(string appName, CancellationToken cancellationToken)
        {
            var requestJson = new JObject
            {
                { "name", appName },
                { "culture", "en-us" },
            };

            var uri = new Uri($"{this.AuthoringHost}{BasePath}");
            using (var content = new StringContent(requestJson.ToString(Formatting.None), Encoding.UTF8, "text/json"))
            using (var httpResponse = await this.HttpClient.PostAsync(uri, content, cancellationToken).ConfigureAwait(false))
            {
                httpResponse.EnsureSuccessStatusCode();
                var jsonString = await httpResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
                var json = JToken.Parse(jsonString);
                return json.ToString();
            }
        }

        public async Task DeleteAppAsync(string appId, CancellationToken cancellationToken)
        {
            var uri = new Uri($"{this.AuthoringHost}{BasePath}{appId}/");
            using (var httpResponse = await this.HttpClient.DeleteAsync(uri, cancellationToken).ConfigureAwait(false))
            {
                httpResponse.EnsureSuccessStatusCode();
            }
        }

        public async Task ImportVersionAsync(string appId, string appVersion, JObject importJson, CancellationToken cancellationToken)
        {
            var uri = new Uri($"{this.AuthoringHost}{BasePath}{appId}/versions/import?versionId={appVersion}");
            using (var content = new StringContent(importJson.ToString(Formatting.None), Encoding.UTF8, "text/json"))
            using (var httpResponse = await this.HttpClient.PostAsync(uri, content, cancellationToken).ConfigureAwait(false))
            {
                httpResponse.EnsureSuccessStatusCode();
            }
        }

        public async Task<JArray> GetTrainingStatusAsync(string appId, string appVersion, CancellationToken cancellationToken)
        {
            var uri = new Uri($"{this.AuthoringHost}{BasePath}{appId}/versions/{appVersion}/train");
            using (var httpResponse = await this.HttpClient.GetAsync(uri, cancellationToken).ConfigureAwait(false))
            {
                httpResponse.EnsureSuccessStatusCode();
                var content = await httpResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
                return JArray.Parse(content);
            }
        }

        public async Task PublishAppAsync(string appId, string appVersion, CancellationToken cancellationToken)
        {
            var requestJson = new JObject
            {
                { "versionId", appVersion },
                { "isStaging", this.IsStaging },
                { "region", this.EndpointRegion },
            };

            var uri = new Uri($"{this.AuthoringHost}{BasePath}{appId}/publish");
            using (var content = new StringContent(requestJson.ToString(Formatting.None), Encoding.UTF8, "text/json"))
            using (var httpResponse = await this.HttpClient.PostAsync(uri, content, cancellationToken).ConfigureAwait(false))
            {
                httpResponse.EnsureSuccessStatusCode();
            }
        }

        public async Task<JObject> QueryAsync(string appId, string text, CancellationToken cancellationToken)
        {
            var staging = this.IsStaging ? "&staging=true" : string.Empty;
            var uri = new Uri($"{Protocol}{this.EndpointRegion}{Domain}/luis/v2.0/apps/{appId}?q={text}{staging}");
            using (var httpRequest = GetQueryRequest(uri, this.EndpointKey))
            {
                while (true)
                {
                    using (var httpResponse = await this.HttpClient.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false))
                    {
                        if (httpResponse.StatusCode == (HttpStatusCode)429)
                        {
                            Logger.LogWarning("Received HTTP 429 result from Cognitive Services. Retrying.");
                            await Task.Delay(ThrottleQueryDelay, cancellationToken).ConfigureAwait(false);
                            continue;
                        }

                        httpResponse.EnsureSuccessStatusCode();
                        var content = await httpResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
                        return JObject.Parse(content);
                    }
                }
            }
        }

        public async Task<JObject> RecognizeSpeechAsync(string appId, string speechFile, CancellationToken cancellationToken)
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
                    return JObject.Parse(content);
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

        public async Task TrainAsync(string appId, string appVersion, CancellationToken cancellationToken)
        {
            var uri = new Uri($"{this.AuthoringHost}{BasePath}{appId}/versions/{appVersion}/train");
            using (var httpResponse = await this.HttpClient.PostAsync(uri, null, cancellationToken).ConfigureAwait(false))
            {
                httpResponse.EnsureSuccessStatusCode();
            }
        }

        public void Dispose()
        {
            this.HttpClient.Dispose();
        }

        private static HttpRequestMessage GetQueryRequest(Uri uri, string apiKey)
        {
            return new HttpRequestMessage(HttpMethod.Get, uri)
            {
                Headers =
                {
                    { SubscriptionKeyHeader, apiKey }
                }
            };
        }
    }
}
