// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace NLU.DevOps.Luis
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using Core;
    using Microsoft.Azure.CognitiveServices.Language.LUIS.Runtime.Models;
    using Microsoft.Extensions.Logging;
    using Models;
    using Newtonsoft.Json.Linq;
    using NLU.DevOps.Logging;

    /// <summary>
    /// Test a LUIS model with text or speech.
    /// Implementation of <see cref="INLUTestClient"/>
    /// </summary>
    public sealed class LuisNLUTestClient : LuisNLUBatchTestClientBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LuisNLUTestClient"/> class.
        /// </summary>
        /// <param name="luisConfiguration">LUIS configuration.</param>
        /// <param name="luisTestClient">LUIS test client.</param>
        /// <param name="luisBatchTestClient">LUIS batch test client.</param>
        public LuisNLUTestClient(ILuisConfiguration luisConfiguration, ILuisTestClient luisTestClient, ILuisBatchTestClient luisBatchTestClient)
            : base(luisConfiguration, luisBatchTestClient)
        {
            this.LuisTestClient = luisTestClient ?? throw new ArgumentNullException(nameof(luisTestClient));
        }

        private static ILogger Logger => LazyLogger.Value;

        private static Lazy<ILogger> LazyLogger { get; } = new Lazy<ILogger>(() => ApplicationLogger.LoggerFactory.CreateLogger<LuisNLUTestClient>());

        private ILuisTestClient LuisTestClient { get; }

        /// <inheritdoc />
        public override async Task<ILabeledUtterance> TestAsync(
            JToken query,
            CancellationToken cancellationToken)
        {
            if (query == null)
            {
                throw new ArgumentNullException(nameof(query));
            }

            try
            {
                var text = query.Value<string>("text");
                var luisResult = await this.LuisTestClient.QueryAsync(text, cancellationToken).ConfigureAwait(false);
                return LuisResultToLabeledUtterance(new SpeechLuisResult(luisResult, 0));
            }
            catch (APIErrorException ex)
            {
                if (ex.Body == null)
                {
                    Logger.LogError($"Received error with message '{ex.Message}'.");
                }
                else
                {
                    Logger.LogError($"Received error with status code '{ex.Body.StatusCode}' and message '{ex.Body.Message}'.");
                }

                throw;
            }
        }

        /// <inheritdoc />
        public override async Task<ILabeledUtterance> TestSpeechAsync(
            string speechFile,
            JToken query,
            CancellationToken cancellationToken)
        {
            if (speechFile == null)
            {
                throw new ArgumentNullException(nameof(speechFile));
            }

            var luisResult = await this.LuisTestClient.RecognizeSpeechAsync(speechFile, cancellationToken).ConfigureAwait(false);
            return LuisResultToLabeledUtterance(luisResult);
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            this.LuisTestClient.Dispose();
        }

        private static ILabeledUtterance LuisResultToLabeledUtterance(SpeechLuisResult speechLuisResult)
        {
            if (speechLuisResult == null)
            {
                return new LabeledUtterance(null, null, null);
            }

            IEntity getEntity(EntityModel entity)
            {
                var entityType = entity.Type;
                if (entity.AdditionalProperties != null &&
                    entity.AdditionalProperties.TryGetValue("role", out var roleValue) &&
                    roleValue is string role &&
                    !string.IsNullOrWhiteSpace(role))
                {
                    entityType = role;
                }

                var entityValue = default(JToken);
                if (entity.AdditionalProperties != null &&
                    entity.AdditionalProperties.TryGetValue("resolution", out var resolution) &&
                    resolution is JToken resolutionJson)
                {
                    entityValue = resolutionJson;
                }

                var utterance = speechLuisResult.LuisResult.Query;
                var startIndex = entity.StartIndex;
                var matchText = utterance.Substring(startIndex, entity.EndIndex - startIndex + 1);
                var matchIndex = 0;
                var currentStart = 0;
                while ((currentStart = utterance.IndexOf(matchText, currentStart, StringComparison.Ordinal)) != startIndex)
                {
                    ++matchIndex;
                    currentStart++;
                }

                Debug.Assert(matchIndex >= 0, "Invalid LUIS response.");

                var entityScore = default(double?);
                if (entity.AdditionalProperties != null &&
                    entity.AdditionalProperties.TryGetValue("score", out var scoreProperty) &&
                    scoreProperty is double scoreValue)
                {
                    entityScore = scoreValue;
                }

                return new Entity(entityType, entityValue, matchText, matchIndex)
                    .WithScore(entityScore);
            }

            return new LabeledUtterance(
                    speechLuisResult.LuisResult.Query,
                    speechLuisResult.LuisResult.TopScoringIntent?.Intent,
                    speechLuisResult.LuisResult.Entities?.Select(getEntity).ToList())
                .WithProperty("intents", speechLuisResult.LuisResult.Intents)
                .WithScore(speechLuisResult.LuisResult.TopScoringIntent?.Score)
                .WithTextScore(speechLuisResult.TextScore)
                .WithTimestamp(DateTimeOffset.Now);
        }
    }
}
