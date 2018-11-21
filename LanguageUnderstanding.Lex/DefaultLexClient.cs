// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace LanguageUnderstanding.Lex
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Amazon;
    using Amazon.Lex;
    using Amazon.Lex.Model;
    using Amazon.LexModelBuildingService;
    using Amazon.LexModelBuildingService.Model;
    using Amazon.Runtime;

    internal sealed class DefaultLexClient : ILexClient
    {
        /// <summary>
        /// Number of retries per Lex model building service request.
        /// </summary>
        /// <remarks>
        /// Experimental results show 5 retries is sufficient for avoiding <see cref="Amazon.LexModelBuildingService.Model.ConflictException"/>.
        /// </remarks>
        private const int RetryCount = 5;

        private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(10);

        public DefaultLexClient(AWSCredentials credentials, RegionEndpoint regionEndpoint)
        {
            this.LexClient = new AmazonLexClient(credentials, regionEndpoint);
            this.LexModelClient = new AmazonLexModelBuildingServiceClient(credentials, regionEndpoint);
        }

        private AmazonLexClient LexClient { get; }

        private AmazonLexModelBuildingServiceClient LexModelClient { get; }

        public Task DeleteBotAliasAsync(DeleteBotAliasRequest request, CancellationToken cancellationToken)
        {
            return RetryAsync(this.LexModelClient.DeleteBotAliasAsync, request, cancellationToken);
        }

        public Task DeleteBotAsync(DeleteBotRequest request, CancellationToken cancellationToken)
        {
            return RetryAsync(this.LexModelClient.DeleteBotAsync, request, cancellationToken);
        }

        public Task<GetBotAliasesResponse> GetBotAliasesAsync(GetBotAliasesRequest request, CancellationToken cancellationToken)
        {
            return RetryAsync(this.LexModelClient.GetBotAliasesAsync, request, cancellationToken);
        }

        public Task<GetBotResponse> GetBotAsync(GetBotRequest request, CancellationToken cancellationToken)
        {
            return RetryAsync(this.LexModelClient.GetBotAsync, request, cancellationToken);
        }

        public Task<GetBotsResponse> GetBotsAsync(GetBotsRequest request, CancellationToken cancellationToken)
        {
            return RetryAsync(this.LexModelClient.GetBotsAsync, request, cancellationToken);
        }

        public Task<GetImportResponse> GetImportAsync(GetImportRequest request, CancellationToken cancellationToken)
        {
            return RetryAsync(this.LexModelClient.GetImportAsync, request, cancellationToken);
        }

        public Task<PostContentResponse> PostContentAsync(PostContentRequest request, CancellationToken cancellationToken)
        {
            return this.LexClient.PostContentAsync(request, cancellationToken);
        }

        public Task<PostTextResponse> PostTextAsync(PostTextRequest request, CancellationToken cancellationToken)
        {
            return this.LexClient.PostTextAsync(request, cancellationToken);
        }

        public Task PutBotAliasAsync(PutBotAliasRequest request, CancellationToken cancellationToken)
        {
            return RetryAsync(this.LexModelClient.PutBotAliasAsync, request, cancellationToken);
        }

        public Task PutBotAsync(PutBotRequest request, CancellationToken cancellationToken)
        {
            return RetryAsync(this.LexModelClient.PutBotAsync, request, cancellationToken);
        }

        public Task<StartImportResponse> StartImportAsync(StartImportRequest request, CancellationToken cancellationToken)
        {
            return RetryAsync(this.LexModelClient.StartImportAsync, request, cancellationToken);
        }

        public void Dispose()
        {
            this.LexClient.Dispose();
            this.LexModelClient.Dispose();
        }

        private static async Task RetryAsync<TRequest>(Func<TRequest, CancellationToken, Task> actionAsync, TRequest request, CancellationToken cancellationToken)
        {
            var count = 0;
            while (count++ < RetryCount)
            {
                try
                {
                    await actionAsync(request, cancellationToken).ConfigureAwait(false);
                    return;
                }
                catch (Amazon.LexModelBuildingService.Model.ConflictException)
                when (count < RetryCount)
                {
                    await Task.Delay(RetryDelay, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        private static async Task<TResponse> RetryAsync<TRequest, TResponse>(Func<TRequest, CancellationToken, Task<TResponse>> actionAsync, TRequest request, CancellationToken cancellationToken)
        {
            var count = 0;
            while (count++ < RetryCount)
            {
                try
                {
                    return await actionAsync(request, cancellationToken).ConfigureAwait(false);
                }
                catch (Amazon.LexModelBuildingService.Model.ConflictException)
                when (count < RetryCount)
                {
                    await Task.Delay(RetryDelay, cancellationToken).ConfigureAwait(false);
                }
            }

            throw new InvalidOperationException("Exception will be rethrown before reaching this point.");
        }
    }
}
