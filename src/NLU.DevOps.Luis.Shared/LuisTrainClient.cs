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
        private const string Protocol = "https://";
        private const string Domain = ".api.cognitive.microsoft.com";

        public LuisTrainClient(ILuisConfiguration luisConfiguration)
        {
            this.LuisConfiguration = luisConfiguration ?? throw new ArgumentNullException(nameof(luisConfiguration));
            var authoringCredentials = new ApiKeyServiceClientCredentials(luisConfiguration.AuthoringKey);
            this.AuthoringClient = new LUISAuthoringClient(authoringCredentials)
            {
                Endpoint = $"{Protocol}{luisConfiguration.AuthoringRegion}{Domain}",
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
#if LUIS_V3
            var request = new ApplicationPublishObjectWithDirectVersionPublish();
#else
            var request = new ApplicationPublishObject();
#endif
            request.IsStaging = this.LuisConfiguration.IsStaging;
            request.VersionId = this.LuisConfiguration.VersionId;
#if LUIS_V2
            request.Region = this.LuisConfiguration.EndpointRegion;
#endif
#if LUIS_V3
            request.DirectVersionPublish = this.LuisConfiguration.DirectVersionPublish;
#endif
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

#if LUIS_V3
        private class ApplicationPublishObjectWithDirectVersionPublish : ApplicationPublishObject
        {
            public bool DirectVersionPublish { get; set; }
        }
#endif
    }
}
