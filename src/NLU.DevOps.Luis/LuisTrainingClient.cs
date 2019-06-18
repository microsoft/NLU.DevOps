// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace NLU.DevOps.Luis
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.CognitiveServices.Language.LUIS.Authoring;
    using Microsoft.Azure.CognitiveServices.Language.LUIS.Authoring.Models;
    using Newtonsoft.Json;

    internal sealed class LuisTrainingClient : ILuisTrainingClient, IDisposable
    {
        public LuisTrainingClient(string authoringKey, string authoringRegion, AzureSubscriptionInfo azureSubscriptionInfo, bool isStaging)
        {
            this.IsStaging = isStaging;

            this.AuthoringKey = authoringKey ?? throw new ArgumentNullException(nameof(authoringKey));
            this.EndpointRegion = authoringRegion ?? throw new ArgumentNullException(nameof(authoringRegion));
            this.AzureSubscriptionInfo = azureSubscriptionInfo;
            this.LazyAuthoringClient = new Lazy<LUISAuthoringClient>(() =>
            {
                var authoringCredentials =
                    new ApiKeyServiceClientCredentials(authoringKey);
                return new LUISAuthoringClient(authoringCredentials)
                {
                    Endpoint = $"{LuisClient.Protocol}{authoringRegion}{LuisClient.Domain}",
                };
            });
        }

        private string EndpointRegion { get; }

        private AzureSubscriptionInfo AzureSubscriptionInfo { get; }

        private string AuthoringKey { get; }

        private bool IsStaging { get; }

        private LUISAuthoringClient AuthoringClient => this.LazyAuthoringClient.Value;

        private Lazy<LUISAuthoringClient> LazyAuthoringClient { get; }

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
            return this.AuthoringClient.Apps.DeleteAsync(Guid.Parse(appId), cancellationToken);
        }

        public async Task<IEnumerable<string>> GetTrainingStatusAsync(string appId, string versionId, CancellationToken cancellationToken)
        {
            IList<ModelTrainingInfo> modelTrainingInfos = await this.AuthoringClient.Train.GetStatusAsync(Guid.Parse(appId), versionId, cancellationToken).ConfigureAwait(false);
            return modelTrainingInfos.Select(modelInfo => modelInfo.Details.Status);
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
                Region = this.EndpointRegion,
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
            this.AuthoringClient.Dispose();
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