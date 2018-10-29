// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace LanguageUnderstanding.Lex
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Amazon.Lex.Model;
    using Amazon.LexModelBuildingService.Model;

    /// <summary>
    /// Interface for Lex client.
    /// </summary>
    public interface ILexClient : IDisposable
    {
        /// <summary>
        /// Calls the 'DeleteBot' action on Lex.
        /// </summary>
        /// <returns>Task to await the operation.</returns>
        /// <param name="request">Request.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task DeleteBotAsync(DeleteBotRequest request, CancellationToken cancellationToken);

        /// <summary>
        /// Calls the 'GetBot' action on Lex.
        /// </summary>
        /// <returns>Task to await the bot response.</returns>
        /// <param name="request">Request.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task<GetBotResponse> GetBotAsync(GetBotRequest request, CancellationToken cancellationToken);

        /// <summary>
        /// Calls the 'GetImport' action on Lex.
        /// </summary>
        /// <returns>Task to await the import response.</returns>
        /// <param name="request">Request.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task<GetImportResponse> GetImportAsync(GetImportRequest request, CancellationToken cancellationToken);

        /// <summary>
        /// Calls the 'PostContent' action on Lex.
        /// </summary>
        /// <returns>Task to await the bot response.</returns>
        /// <param name="request">Request.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task<PostContentResponse> PostContentAsync(PostContentRequest request, CancellationToken cancellationToken);

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
