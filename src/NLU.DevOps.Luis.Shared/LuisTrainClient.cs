// Copyright (c) Microsoft Corporation.
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
    using Microsoft.Azure.CognitiveServices.Language.LUIS.Authoring;
    using Microsoft.Azure.CognitiveServices.Language.LUIS.Authoring.Models;
    using Newtonsoft.Json;

    internal class LuisTrainClient : ILuisTrainClient
    {
        public LuisTrainClient(ILuisConfiguration luisConfiguration)
        {
            this.LuisConfiguration = luisConfiguration ?? throw new ArgumentNullException(nameof(luisConfiguration));
            var authoringCredentials = new ApiKeyServiceClientCredentials(luisConfiguration.AuthoringKey);
            this.AuthoringClient = new LUISAuthoringClient(authoringCredentials)
            {
                Endpoint = luisConfiguration.AuthoringEndpoint,
            };
        }

        private ILuisConfiguration LuisConfiguration { get; }

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
            var azureSubscriptionInfo = AzureSubscriptionInfo.Create(this.LuisConfiguration);
            if (azureSubscriptionInfo != null)
            {
                await this.AssignAzureResourceAsync(azureSubscriptionInfo, appId).ConfigureAwait(false);
            }

            return appId.ToString();
        }

        public Task DeleteAppAsync(string appId, CancellationToken cancellationToken)
        {
            return this.AuthoringClient.Apps.DeleteAsync(Guid.Parse(appId), cancellationToken: cancellationToken);
        }

        public Task DeleteVersionAsync(string appId, string versionId, CancellationToken cancellationToken)
        {
            return this.AuthoringClient.Versions.DeleteAsync(Guid.Parse(appId), versionId, cancellationToken);
        }

        public async Task<OperationResponse<IList<ModelTrainingInfo>>> GetTrainingStatusAsync(string appId, string versionId, CancellationToken cancellationToken)
        {
            var operationResponse = await this.AuthoringClient.Train.GetStatusWithHttpMessagesAsync(Guid.Parse(appId), versionId, cancellationToken: cancellationToken).ConfigureAwait(false);
            return OperationResponse.Create(operationResponse.Body, operationResponse.Response);
        }

        public Task ImportVersionAsync(string appId, string versionId, LuisApp luisApp, CancellationToken cancellationToken)
        {
            return this.AuthoringClient.Versions.ImportAsync(Guid.Parse(appId), luisApp, versionId, cancellationToken);
        }

        public Task PublishAppAsync(string appId, string versionId, CancellationToken cancellationToken)
        {
            var request = new ApplicationPublishObjectWithDirectVersionPublish();
            request.IsStaging = this.LuisConfiguration.IsStaging;
            request.VersionId = this.LuisConfiguration.VersionId;
            request.DirectVersionPublish = this.LuisConfiguration.DirectVersionPublish;
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

        private async Task AssignAzureResourceAsync(AzureSubscriptionInfo azureSubscriptionInfo, Guid appId)
        {
            var jsonBody = JsonConvert.SerializeObject(azureSubscriptionInfo);
            var data = new StringContent(jsonBody, Encoding.UTF8, "application/json");
            var url = $"{this.AuthoringClient.Endpoint}/luis/api/v2.0/apps/{appId}/azureaccounts";
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri(url),
                Headers =
                {
                    { HttpRequestHeader.Authorization.ToString(), $"Bearer {this.LuisConfiguration.ArmToken}" },
                    { "Ocp-Apim-Subscription-Key", this.LuisConfiguration.AuthoringKey }
                },
                Content = data,
            };

            var result = await this.AuthoringClient.HttpClient.SendAsync(request).ConfigureAwait(false);
            result.EnsureSuccessStatusCode();
        }

        private class ApplicationPublishObjectWithDirectVersionPublish : ApplicationPublishObject
        {
            public bool DirectVersionPublish { get; set; }
        }
    }
}
