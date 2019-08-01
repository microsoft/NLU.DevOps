// Copyright (c) Microsoft Corporation. All rights reserved.
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
    using Models;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Test a LUIS model with text or speech.
    /// Implementation of <see cref="INLUTestClient"/>
    /// </summary>
    public sealed class LuisNLUTestClient : DefaultNLUTestClient
    {
        private const double Epsilon = 10e-6;

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

        private LuisSettings LuisSettings { get; }

        private ILuisTestClient LuisClient { get; }

        /// <inheritdoc />
        protected override async Task<LabeledUtterance> TestAsync(
            string utterance,
            CancellationToken cancellationToken)
        {
            var luisResult = await this.LuisClient.QueryAsync(utterance, cancellationToken).ConfigureAwait(false);
            return this.LuisResultToLabeledUtterance(new SpeechLuisResult(luisResult, 0));
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

        private static JToken GetEntityValue(JToken resolution)
        {
            if (resolution == null)
            {
                return null;
            }

            var value = resolution["value"];
            if (value != null)
            {
                return value;
            }

            // TODO: should we return multiple values?
            var resolvedValue = resolution["values"][0];
            return resolvedValue is JObject resolvedObject
                ? resolvedObject["value"]
                : resolvedValue;
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
                if (entityType != null && mappedTypes.TryGetValue(entityType, out var mappedType))
                {
                    entityType = mappedType;
                }

                var entityValue = default(JToken);
                var entityResolution = default(JToken);
                if (entity.AdditionalProperties != null &&
                    entity.AdditionalProperties.TryGetValue("resolution", out var resolution) &&
                    resolution is JToken resolutionJson)
                {
                    entityValue = GetEntityValue(resolutionJson);
                    entityResolution = resolutionJson;
                }

                var matchText = entity.Entity;
                var matches = Regex.Matches(speechLuisResult.LuisResult.Query, matchText, RegexOptions.IgnoreCase);
                var matchIndex = -1;
                for (var i = 0; i < matches.Count; ++i)
                {
                    if (matches[i].Index == entity.StartIndex)
                    {
                        matchIndex = i;
                        break;
                    }
                }

                Debug.Assert(matchIndex >= 0, "Invalid LUIS response.");

                var entityScore = default(double?);
                if (entity.AdditionalProperties != null &&
                    entity.AdditionalProperties.TryGetValue("score", out var scoreProperty) &&
                    scoreProperty is double scoreValue)
                {
                    entityScore = scoreValue;
                }

                return entityScore.HasValue
                    ? new ScoredEntity(entityType, entityValue, entityResolution, matchText, matchIndex, entityScore.Value)
                    : new Entity(entityType, entityValue, entityResolution, matchText, matchIndex);
            }

            var intent = speechLuisResult.LuisResult.TopScoringIntent?.Intent;
            var score = speechLuisResult.LuisResult.TopScoringIntent?.Score;
            var entities = speechLuisResult.LuisResult.Entities?.Select(getEntity).ToList();
            return !score.HasValue && Math.Abs(speechLuisResult.TextScore) < Epsilon
                ? new LabeledUtterance(speechLuisResult.LuisResult.Query, intent, entities)
                : new ScoredLabeledUtterance(speechLuisResult.LuisResult.Query, intent, score ?? 0, speechLuisResult.TextScore, entities);
        }
    }
}
