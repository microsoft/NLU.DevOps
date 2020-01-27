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
    public sealed class LuisNLUTestClient : DefaultNLUTestClient
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LuisNLUTestClient"/> class.
        /// </summary>
        /// <param name="luisSettings">LUIS settings.</param>
        /// <param name="luisClient">LUIS test client.</param>
        public LuisNLUTestClient(LuisSettings luisSettings, ILuisTestClient luisClient)
        {
            this.LuisSettings = luisSettings ?? throw new ArgumentNullException(nameof(luisSettings));
            this.LuisClient = luisClient ?? throw new ArgumentNullException(nameof(luisClient));
        }

        private static ILogger Logger => LazyLogger.Value;

        private static Lazy<ILogger> LazyLogger { get; } = new Lazy<ILogger>(() => ApplicationLogger.LoggerFactory.CreateLogger<LuisNLUTestClient>());

        private LuisSettings LuisSettings { get; }

        private ILuisTestClient LuisClient { get; }

        /// <inheritdoc />
        protected override async Task<LabeledUtterance> TestAsync(
            string utterance,
            CancellationToken cancellationToken)
        {
            try
            {
                var luisResult = await this.LuisClient.QueryAsync(utterance, cancellationToken).ConfigureAwait(false);
                return this.LuisResultToLabeledUtterance(new SpeechLuisResult(luisResult, 0));
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
        protected override async Task<LabeledUtterance> TestSpeechAsync(
            string speechFile,
            CancellationToken cancellationToken)
        {
            var luisResult = await this.LuisClient.RecognizeSpeechAsync(speechFile, cancellationToken).ConfigureAwait(false);
            return this.LuisResultToLabeledUtterance(luisResult);
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            this.LuisClient.Dispose();
        }

        private LabeledUtterance LuisResultToLabeledUtterance(SpeechLuisResult speechLuisResult)
        {
            if (speechLuisResult == null)
            {
                return new LabeledUtterance(null, null, null);
            }

            var mappedTypes = this.LuisSettings.PrebuiltEntityTypes
                .ToDictionary(pair => $"builtin.{pair.Value}", pair => pair.Key);

            Entity getEntity(EntityModel entity)
            {
                var entityType = entity.Type;
                var hasRole = false;
                if (entity.AdditionalProperties != null &&
                    entity.AdditionalProperties.TryGetValue("role", out var roleValue) &&
                    roleValue is string role &&
                    !string.IsNullOrWhiteSpace(role))
                {
                    entityType = role;
                    hasRole = true;
                }

                if (!hasRole && entityType != null && mappedTypes.TryGetValue(entityType, out var mappedType))
                {
                    entityType = mappedType;
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
