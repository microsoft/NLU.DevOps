// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace LanguageUnderstanding.Luis
{
    using System;
    using System.Net.Http;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.CognitiveServices.Speech;
    using Microsoft.CognitiveServices.Speech.Audio;
    using Microsoft.CognitiveServices.Speech.Intent;

    /// <summary>
    /// Assists in making http requests to LUIS.
    /// </summary>
    internal sealed class LuisClient : ILuisClient
    {
        /// <summary>
        /// Subscription key header for LUIS requests.
        /// </summary>
        private const string SubscriptionKeyHeader = "Ocp-Apim-Subscription-Key";

        /// <summary>
        /// Initializes a new instance of the <see cref="LuisClient"/> class.
        /// </summary>
        /// <param name="authoringKey">LUIS authoring key.</param>
        /// <param name="endpointKey">LUIS endpoint key.</param>
        /// <param name="endpointRegion">LUIS endpoint region.</param>
        public LuisClient(string authoringKey, string endpointKey, string endpointRegion)
        {
            this.HttpClient = new HttpClient
            {
                DefaultRequestHeaders =
                {
                    { SubscriptionKeyHeader, authoringKey },
                }
            };

            this.LazySpeechConfig = new Lazy<SpeechConfig>(() => SpeechConfig.FromSubscription(endpointKey, endpointRegion));
        }

        /// <summary> Gets the HTTP client configured with the LUIS subscription key header.</summary>
        private HttpClient HttpClient { get; }

        /// <summary> Gets the configuration for the speech recognizer. </summary>
        private Lazy<SpeechConfig> LazySpeechConfig { get; }

        /// <inheritdoc />
        public async Task<HttpResponseMessage> GetAsync(Uri uri, CancellationToken cancellationToken)
        {
            using (var request = new HttpRequestMessage())
            {
                request.Method = HttpMethod.Get;
                request.RequestUri = uri;
                return await this.HttpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <inheritdoc />
        public async Task<HttpResponseMessage> PostAsync(Uri uri, string requestBody, CancellationToken cancellationToken)
        {
            using (var request = new HttpRequestMessage())
            {
                request.Method = HttpMethod.Post;
                request.RequestUri = uri;

                if (!string.IsNullOrEmpty(requestBody))
                {
                    request.Content = new StringContent(requestBody, Encoding.UTF8, "text/json");
                }

                return await this.HttpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <inheritdoc />
        public async Task<HttpResponseMessage> DeleteAsync(Uri uri, CancellationToken cancellationToken)
        {
            using (var request = new HttpRequestMessage())
            {
                return await this.HttpClient.DeleteAsync(uri, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <inheritdoc />
        public async Task<string> RecognizeSpeechAsync(string appId, string speechFile, CancellationToken cancellationToken)
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
                    return result.Properties.GetProperty(PropertyId.LanguageUnderstandingServiceResponse_JsonResult);
                }
                else
                {
                    throw new InvalidOperationException($"Failed to get speech recognition result. Reason = '{result.Reason}'");
                }
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            this.HttpClient.Dispose();
        }
    }
}
