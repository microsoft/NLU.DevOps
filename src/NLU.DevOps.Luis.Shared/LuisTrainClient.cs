// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace NLU.DevOps.Luis
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Http;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Logging;
    using Microsoft.Azure.CognitiveServices.Language.LUIS.Authoring;
    using Microsoft.Azure.CognitiveServices.Language.LUIS.Authoring.Models;
    using Microsoft.Extensions.Logging;
    using Newtonsoft.Json;

    internal class LuisTrainClient : ILuisTrainClient
    {
        private const string Protocol = "https://";
        private const string Domain = ".api.cognitive.microsoft.com";

        public LuisTrainClient(
            string authoringKey,
            string authoringRegion,
            string endpointRegion,
            AzureSubscriptionInfo azureSubscriptionInfo,
            bool isStaging)
        {
            this.IsStaging = isStaging;
            this.AzureSubscriptionInfo = azureSubscriptionInfo;
            this.AuthoringKey = authoringKey ?? throw new ArgumentNullException(nameof(authoringKey));

            var validAuthoringRegion = authoringRegion ?? throw new ArgumentNullException(nameof(authoringRegion));
            this.EndpointRegion = endpointRegion ?? validAuthoringRegion;

            var authoringCredentials = new ApiKeyServiceClientCredentials(
                authoringKey ?? throw new ArgumentNullException(nameof(authoringKey)));

            this.AuthoringClient = new LUISAuthoringClient(authoringCredentials)
            {
                Endpoint = $"{Protocol}{validAuthoringRegion}{Domain}",
            };
        }

        private static ILogger Logger => LazyLogger.Value;

        private static Lazy<ILogger> LazyLogger { get; } = new Lazy<ILogger>(() => ApplicationLogger.LoggerFactory.CreateLogger<LuisNLUTrainClient>());

        private AzureSubscriptionInfo AzureSubscriptionInfo { get; }

        private string AuthoringKey { get; }

        private string EndpointRegion { get; }

        private string VersionId { get; }

        private bool IsStaging { get; }

        private LUISAuthoringClient AuthoringClient { get; }

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
#if LUIS_V2
                Region = this.EndpointRegion,
#endif
                VersionId = versionId,
            };

            return this.AuthoringClient.Apps.PublishAsync(Guid.Parse(appId), request, cancellationToken);
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
                this.AuthoringClient.Dispose();
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
