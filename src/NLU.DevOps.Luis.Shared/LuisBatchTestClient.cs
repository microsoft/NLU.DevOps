// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace NLU.DevOps.Luis
{
    using System;
    using System.IO;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using Logging;
    using Microsoft.Extensions.Logging;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    internal class LuisBatchTestClient : ILuisBatchTestClient
    {
        public const int BatchSize = 500;

        public LuisBatchTestClient(ILuisConfiguration luisConfiguration)
        {
            this.LuisConfiguration = luisConfiguration;
        }

        private static ILogger Logger => LazyLogger.Value;

        private static Lazy<ILogger> LazyLogger { get; } = new Lazy<ILogger>(() => ApplicationLogger.LoggerFactory.CreateLogger<LuisBatchTestClient>());

        private ILuisConfiguration LuisConfiguration { get; }

        public Task<OperationResponse<string>> CreateEvaluationsOperationAsync(JToken batchInput, CancellationToken cancellationToken)
        {
            var request = (HttpWebRequest)WebRequest.Create(new Uri(this.LuisConfiguration.GetBatchEvaluationEndpoint()));
#if DEBUG
            // Temporary hack while using experimental endpoint
            request.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;
#endif
            request.Method = "POST";
            request.ContentType = "application/json";
            request.Headers.Add("Apim-Subscription-Id", this.LuisConfiguration.PredictionKey);

            async Task<OperationResponse<string>> getResponseAsync()
            {
                using (var requestStream = await request.GetRequestStreamAsync().ConfigureAwait(false))
                using (var streamWriter = new StreamWriter(requestStream))
                {
                    await streamWriter.WriteAsync(batchInput.ToString(Formatting.None)).ConfigureAwait(false);
                    using (var response = await request.GetResponseAsync().ConfigureAwait(false))
                    using (var streamReader = new StreamReader(response.GetResponseStream()))
                    {
                        var responseText = await streamReader.ReadToEndAsync().ConfigureAwait(false);
                        var operationId = JToken.Parse(responseText).Value<string>("operationId");
                        return OperationResponse.Create(operationId, response as HttpWebResponse);
                    }
                }
            }

            return Retry.OnTransientErrorAsync(getResponseAsync, cancellationToken);
        }

        public Task<JToken> GetEvaluationsResultAsync(string operationId, CancellationToken cancellationToken)
        {
            var request = (HttpWebRequest)WebRequest.Create(new Uri(this.LuisConfiguration.GetBatchResultEndpoint(operationId)));
#if DEBUG
            // Temporary hack while using experimental endpoint
            request.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;
#endif
            request.Method = "GET";
            request.Accept = "application/json";
            request.Headers.Add("Apim-Subscription-Id", this.LuisConfiguration.PredictionKey);

            async Task<JToken> getResponseAsync()
            {
                using (var response = await request.GetResponseAsync().ConfigureAwait(false))
                using (var streamReader = new StreamReader(response.GetResponseStream()))
                {
                    var responseText = await streamReader.ReadToEndAsync().ConfigureAwait(false);
                    return JToken.Parse(responseText);
                }
            }

            return Retry.OnTransientErrorAsync(getResponseAsync, cancellationToken);
        }

        public Task<OperationResponse<LuisBatchStatusInfo>> GetEvaluationsStatusAsync(string operationId, CancellationToken cancellationToken)
        {
            var request = (HttpWebRequest)WebRequest.Create(new Uri(this.LuisConfiguration.GetBatchStatusEndpoint(operationId)));
#if DEBUG
            // Temporary hack while using experimental endpoint
            request.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;
#endif
            request.Method = "GET";
            request.Accept = "application/json";
            request.Headers.Add("Apim-Subscription-Id", this.LuisConfiguration.PredictionKey);

            async Task<OperationResponse<LuisBatchStatusInfo>> getResponseAsync()
            {
                using (var response = await request.GetResponseAsync().ConfigureAwait(false))
                using (var streamReader = new StreamReader(response.GetResponseStream()))
                {
                    var responseText = await streamReader.ReadToEndAsync().ConfigureAwait(false);
                    var batchStatus = JsonConvert.DeserializeObject<LuisBatchStatusInfo>(responseText);
                    var retryAfter = response.Headers[HttpResponseHeader.RetryAfter];
                    return OperationResponse.Create(batchStatus, retryAfter);
                }
            }

            return Retry.OnTransientErrorAsync(getResponseAsync, cancellationToken);
        }
    }
}
