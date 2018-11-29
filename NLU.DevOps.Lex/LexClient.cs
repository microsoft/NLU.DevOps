// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace NLU.DevOps.Lex
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
    using Logging;
    using Microsoft.Extensions.Logging;

    internal sealed class LexClient : ILexClient
    {
        /// <summary>
        /// Number of retries per Lex model building service request.
        /// </summary>
        /// <remarks>
        /// Experimental results show 5 retries is sufficient for avoiding <see cref="Amazon.LexModelBuildingService.Model.ConflictException"/>.
        /// </remarks>
        private const int RetryCount = 5;

        private static readonly TimeSpan RetryConflictDelay = TimeSpan.FromSeconds(10);
        private static readonly TimeSpan RetryLimitExceededDelay = TimeSpan.FromSeconds(1);

        public LexClient(AWSCredentials credentials, RegionEndpoint regionEndpoint)
        {
            this.AmazonLexClient = new AmazonLexClient(credentials, regionEndpoint);
            this.AmazonLexModelClient = new AmazonLexModelBuildingServiceClient(credentials, regionEndpoint);
        }

        private static ILogger Logger => LazyLogger.Value;

        private static Lazy<ILogger> LazyLogger { get; } = new Lazy<ILogger>(() => ApplicationLogger.LoggerFactory.CreateLogger<LexNLUService>());

        private AmazonLexClient AmazonLexClient { get; }

        private AmazonLexModelBuildingServiceClient AmazonLexModelClient { get; }

        public Task DeleteBotAliasAsync(DeleteBotAliasRequest request, CancellationToken cancellationToken)
        {
            return RetryAsync(this.AmazonLexModelClient.DeleteBotAliasAsync, request, cancellationToken);
        }

        public Task DeleteBotAsync(DeleteBotRequest request, CancellationToken cancellationToken)
        {
            return RetryAsync(this.AmazonLexModelClient.DeleteBotAsync, request, cancellationToken);
        }

        public Task<GetBotAliasesResponse> GetBotAliasesAsync(GetBotAliasesRequest request, CancellationToken cancellationToken)
        {
            return RetryAsync(this.AmazonLexModelClient.GetBotAliasesAsync, request, cancellationToken);
        }

        public Task<GetBotResponse> GetBotAsync(GetBotRequest request, CancellationToken cancellationToken)
        {
            return RetryAsync(this.AmazonLexModelClient.GetBotAsync, request, cancellationToken);
        }

        public Task<GetBotsResponse> GetBotsAsync(GetBotsRequest request, CancellationToken cancellationToken)
        {
            return RetryAsync(this.AmazonLexModelClient.GetBotsAsync, request, cancellationToken);
        }

        public Task<GetImportResponse> GetImportAsync(GetImportRequest request, CancellationToken cancellationToken)
        {
            return RetryAsync(this.AmazonLexModelClient.GetImportAsync, request, cancellationToken);
        }

        public Task<PostContentResponse> PostContentAsync(PostContentRequest request, CancellationToken cancellationToken)
        {
            return RetryAsync(this.AmazonLexClient.PostContentAsync, request, cancellationToken);
        }

        public Task<PostTextResponse> PostTextAsync(PostTextRequest request, CancellationToken cancellationToken)
        {
            return RetryAsync(this.AmazonLexClient.PostTextAsync, request, cancellationToken);
        }

        public Task PutBotAliasAsync(PutBotAliasRequest request, CancellationToken cancellationToken)
        {
            return RetryAsync(this.AmazonLexModelClient.PutBotAliasAsync, request, cancellationToken);
        }

        public Task PutBotAsync(PutBotRequest request, CancellationToken cancellationToken)
        {
            return RetryAsync(this.AmazonLexModelClient.PutBotAsync, request, cancellationToken);
        }

        public Task<StartImportResponse> StartImportAsync(StartImportRequest request, CancellationToken cancellationToken)
        {
            return RetryAsync(this.AmazonLexModelClient.StartImportAsync, request, cancellationToken);
        }

        public void Dispose()
        {
            using (this.AmazonLexClient)
            using (this.AmazonLexModelClient)
            {
            }
        }

        private static Task RetryAsync<TRequest>(Func<TRequest, CancellationToken, Task> actionAsync, TRequest request, CancellationToken cancellationToken)
        {
            async Task<bool> asyncActionWrapper(TRequest requestParam, CancellationToken cancellationTokenParam)
            {
                await actionAsync(requestParam, cancellationTokenParam).ConfigureAwait(false);
                return true;
            }

            return RetryAsync(asyncActionWrapper, request, cancellationToken);
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
                catch (Amazon.LexModelBuildingService.Model.ConflictException conflictException)
                when (count < RetryCount)
                {
                    Logger.LogWarning(conflictException, $"Encountered 'ConflictException', retrying.");
                    await Task.Delay(RetryConflictDelay, cancellationToken).ConfigureAwait(false);
                }
                catch (Amazon.Lex.Model.LimitExceededException limitExceededException)
                when (count < RetryCount)
                {
                    Logger.LogWarning(limitExceededException, "Encountered 'LimitExceededException', retrying.");
                    await Task.Delay(RetryLimitExceededDelay, cancellationToken).ConfigureAwait(false);
                }
            }

            throw new InvalidOperationException("Exception will be rethrown before reaching this point.");
        }
    }
}
