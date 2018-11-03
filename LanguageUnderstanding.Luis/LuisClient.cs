// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace LanguageUnderstanding.Luis
{
    using System;
    using System.Net.Http;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Assists in making http requests to LUIS.
    /// </summary>
    internal class LuisClient : ILuisClient
    {
        /// <summary>
        /// Subscription key header for LUIS requests.
        /// </summary>
        private const string SubscriptionKeyHeader = "Ocp-Apim-Subscription-Key";

        /// <summary> HTTP client instance to be used throughout application lifetime.</summary>
        private HttpClient client = new HttpClient();

        /// <summary>
        /// Initializes a new instance of the <see cref="LuisClient"/> class.
        /// </summary>
        /// <param name="authoringKey">LUIS authoring key.</param>
        public LuisClient(string authoringKey)
        {
            this.AuthoringKey = authoringKey;
            this.client = new HttpClient
            {
                DefaultRequestHeaders =
                {
                    { SubscriptionKeyHeader, authoringKey },
                }
            };
        }

        /// <summary>
        /// Gets LUIS authoring key.
        /// </summary>
        internal string AuthoringKey { get; }

        /// <inheritdoc />
        public async Task<HttpResponseMessage> GetAsync(string uri, CancellationToken cancellationToken)
        {
            using (var request = new HttpRequestMessage())
            {
                request.Method = HttpMethod.Get;
                request.RequestUri = new Uri(uri);
                return await this.client.SendAsync(request, cancellationToken);
            }
        }

        /// <inheritdoc />
        public async Task<HttpResponseMessage> PostAsync(string uri, string requestBody, CancellationToken cancellationToken)
        {
            using (var request = new HttpRequestMessage())
            {
                request.Method = HttpMethod.Post;
                request.RequestUri = new Uri(uri);

                if (!string.IsNullOrEmpty(requestBody))
                {
                    request.Content = new StringContent(requestBody, Encoding.UTF8, "text/json");
                }

                return await this.client.SendAsync(request, cancellationToken);
            }
        }

        /// <inheritdoc />
        public async Task<HttpResponseMessage> DeleteAsync(string uri, CancellationToken cancellationToken)
        {
            using (var request = new HttpRequestMessage())
            {
                return await this.client.DeleteAsync(uri, cancellationToken);
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            this.client.Dispose();
        }
    }
}
