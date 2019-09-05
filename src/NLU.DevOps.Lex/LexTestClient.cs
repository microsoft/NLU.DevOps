// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace NLU.DevOps.Lex
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Amazon;
    using Amazon.Lex;
    using Amazon.Lex.Model;
    using Amazon.Runtime;
    using Logging;
    using Microsoft.Extensions.Logging;

    internal sealed class LexTestClient : ILexTestClient
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

        public LexTestClient(AWSCredentials credentials, RegionEndpoint regionEndpoint)
        {
            this.AmazonLexClient = new AmazonLexClient(credentials, regionEndpoint);
        }

        private static ILogger Logger => LazyLogger.Value;

        private static Lazy<ILogger> LazyLogger { get; } = new Lazy<ILogger>(() => ApplicationLogger.LoggerFactory.CreateLogger<LexNLUTrainClient>());

        private AmazonLexClient AmazonLexClient { get; }

        public Task<PostContentResponse> PostContentAsync(PostContentRequest request, CancellationToken cancellationToken)
        {
            return RetryAsync(this.AmazonLexClient.PostContentAsync, request, cancellationToken);
        }

        public Task<PostTextResponse> PostTextAsync(PostTextRequest request, CancellationToken cancellationToken)
        {
            return RetryAsync(this.AmazonLexClient.PostTextAsync, request, cancellationToken);
        }

        public void Dispose()
        {
            this.AmazonLexClient.Dispose();
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
            Task onErrorAsync(Exception ex, TimeSpan delay, string name)
            {
                Logger.LogWarning(ex, $"Encountered '{name}', retrying.");
                return Task.Delay(delay, cancellationToken);
            }

            var count = 0;
            while (count++ < RetryCount)
            {
                try
                {
                    return await actionAsync(request, cancellationToken).ConfigureAwait(false);
                }
                catch (ConflictException ex)
                when (count < RetryCount)
                {
                    await onErrorAsync(ex, RetryConflictDelay, "ConflictException").ConfigureAwait(false);
                }
                catch (LimitExceededException ex)
                when (count < RetryCount)
                {
                    await onErrorAsync(ex, RetryLimitExceededDelay, "LimitExceededException").ConfigureAwait(false);
                }
            }

            throw new InvalidOperationException("Exception will be rethrown before reaching this point.");
        }
    }
}
