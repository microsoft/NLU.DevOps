// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace NLU.DevOps.Lex
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Amazon.LexModelBuildingService.Model;

    /// <summary>
    /// Lex client interface for training operations.
    /// </summary>
    public interface ILexTrainClient : IDisposable
    {
        /// <summary>
        /// Calls the 'DeleteBotAlias' action on Lex.
        /// </summary>
        /// <returns>Task to await the operation.</returns>
        /// <param name="request">Request.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task DeleteBotAliasAsync(DeleteBotAliasRequest request, CancellationToken cancellationToken);

        /// <summary>
        /// Calls the 'DeleteBot' action on Lex.
        /// </summary>
        /// <returns>Task to await the operation.</returns>
        /// <param name="request">Request.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task DeleteBotAsync(DeleteBotRequest request, CancellationToken cancellationToken);

        /// <summary>
        /// Calls the 'GetBotAliases' action on Lex.
        /// </summary>
        /// <returns>Task to await the bot aliases response.</returns>
        /// <param name="request">Request.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task<GetBotAliasesResponse> GetBotAliasesAsync(GetBotAliasesRequest request, CancellationToken cancellationToken);

        /// <summary>
        /// Calls the 'GetBot' action on Lex.
        /// </summary>
        /// <returns>Task to await the bot response.</returns>
        /// <param name="request">Request.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task<GetBotResponse> GetBotAsync(GetBotRequest request, CancellationToken cancellationToken);

        /// <summary>
        /// Calls the 'GetBots' action on Lex.
        /// </summary>
        /// <returns>Task to await the bots response.</returns>
        /// <param name="request">Request.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task<GetBotsResponse> GetBotsAsync(GetBotsRequest request, CancellationToken cancellationToken);

        /// <summary>
        /// Calls the 'GetImport' action on Lex.
        /// </summary>
        /// <returns>Task to await the import response.</returns>
        /// <param name="request">Request.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task<GetImportResponse> GetImportAsync(GetImportRequest request, CancellationToken cancellationToken);

        /// <summary>
        /// Calls the 'PutBotAlias' action on Lex.
        /// </summary>
        /// <returns>Task to await the operation.</returns>
        /// <param name="request">Request.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task PutBotAliasAsync(PutBotAliasRequest request, CancellationToken cancellationToken);

        /// <summary>
        /// Calls the 'PutBot' action on Lex.
        /// </summary>
        /// <returns>Task to await the operation.</returns>
        /// <param name="request">Request.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task PutBotAsync(PutBotRequest request, CancellationToken cancellationToken);

        /// <summary>
        /// Calls the 'StartImport' action on Lex.
        /// </summary>
        /// <returns>Task to await the import response.</returns>
        /// <param name="request">Request.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task<StartImportResponse> StartImportAsync(StartImportRequest request, CancellationToken cancellationToken);
    }
}
