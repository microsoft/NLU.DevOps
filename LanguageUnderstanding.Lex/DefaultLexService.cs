// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace LanguageUnderstanding.Lex
{
    using System.Threading;
    using System.Threading.Tasks;
    using Amazon;
    using Amazon.Lex;
    using Amazon.Lex.Model;
    using Amazon.LexModelBuildingService;
    using Amazon.LexModelBuildingService.Model;
    using Amazon.Runtime;

    internal sealed class DefaultLexService : ILexClient
    {
        public DefaultLexService(AWSCredentials credentials, RegionEndpoint regionEndpoint)
        {
            this.LexClient = new AmazonLexClient(credentials, regionEndpoint);
            this.LexModelClient = new AmazonLexModelBuildingServiceClient(credentials, regionEndpoint);
        }

        private AmazonLexClient LexClient { get; }

        private AmazonLexModelBuildingServiceClient LexModelClient { get; }

        public Task DeleteBotAliasAsync(DeleteBotAliasRequest request, CancellationToken cancellationToken)
        {
            return this.LexModelClient.DeleteBotAliasAsync(request, cancellationToken);
        }

        public Task DeleteBotAsync(DeleteBotRequest request, CancellationToken cancellationToken)
        {
            return this.LexModelClient.DeleteBotAsync(request, cancellationToken);
        }

        public Task<GetBotAliasesResponse> GetBotAliasesAsync(GetBotAliasesRequest request, CancellationToken cancellationToken)
        {
            return this.LexModelClient.GetBotAliasesAsync(request, cancellationToken);
        }

        public Task<GetBotResponse> GetBotAsync(GetBotRequest request, CancellationToken cancellationToken)
        {
            return this.LexModelClient.GetBotAsync(request, cancellationToken);
        }

        public Task<GetBotsResponse> GetBotsAsync(GetBotsRequest request, CancellationToken cancellationToken)
        {
            return this.LexModelClient.GetBotsAsync(request, cancellationToken);
        }

        public Task<GetImportResponse> GetImportAsync(GetImportRequest request, CancellationToken cancellationToken)
        {
            return this.LexModelClient.GetImportAsync(request, cancellationToken);
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
            return this.LexModelClient.PutBotAliasAsync(request, cancellationToken);
        }

        public Task PutBotAsync(PutBotRequest request, CancellationToken cancellationToken)
        {
            return this.LexModelClient.PutBotAsync(request, cancellationToken);
        }

        public Task<StartImportResponse> StartImportAsync(StartImportRequest request, CancellationToken cancellationToken)
        {
            return this.LexModelClient.StartImportAsync(request, cancellationToken);
        }

        public void Dispose()
        {
            this.LexClient.Dispose();
            this.LexModelClient.Dispose();
        }
    }
}
