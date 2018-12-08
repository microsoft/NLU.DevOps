// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace NLU.DevOps.Luis
{
    using System;
    using System.Globalization;
    using System.IO;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.CognitiveServices.Language.LUIS.Runtime.Models;
    using Newtonsoft.Json.Linq;

    internal sealed class RestSpeechLuisClient : LuisClient
    {
        private const string SpeechEndpointTemplate = "https://{0}.stt.speech.microsoft.com/speech/recognition/conversation/cognitiveservices/v1?language={1}";

        public RestSpeechLuisClient(
            string authoringKey,
            string authoringRegion,
            string endpointKey,
            string endpointRegion,
            AzureSubscriptionInfo azureSubscriptionInfo,
            string speechKey,
            bool isStaging)
            : base(authoringKey, authoringRegion, endpointKey, endpointRegion, azureSubscriptionInfo, isStaging)
        {
            this.SpeechKey = speechKey;
            this.SpeechEndpoint = string.Format(CultureInfo.InvariantCulture, SpeechEndpointTemplate, endpointRegion ?? authoringRegion, "en-US");
        }

        private string SpeechEndpoint { get; }

        private string SpeechKey { get; }

        public override async Task<LuisResult> RecognizeSpeechAsync(string appId, string speechFile, CancellationToken cancellationToken)
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

            return await this.QueryAsync(appId, responseJson.Value<string>("DisplayText"), cancellationToken).ConfigureAwait(false);
        }
    }
}
