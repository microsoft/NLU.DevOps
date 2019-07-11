// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace NLU.DevOps.Luis
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Core;
    using Logging;
    using Microsoft.Azure.CognitiveServices.Language.LUIS.Runtime.Models;
    using Microsoft.Extensions.Logging;
    using Models;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Test a LUIS model with text and speech.
    /// Implementation of <see cref="INLUTestClient"/>
    /// </summary>
    public sealed class LuisNLUTestClient : NLUTestClientBase<LuisNLUQuery>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LuisNLUTestClient"/> class.
        /// </summary>
        /// <param name="appId">App ID.</param>
        /// <param name="luisSettings">LUIS settings.</param>
        /// <param name="luisClient">LUIS client.</param>
        public LuisNLUTestClient(string appId, LuisSettings luisSettings, ILuisTestClient luisClient)
        {
            this.LuisAppId = appId ?? throw new ArgumentNullException(nameof(appId));
            this.LuisSettings = luisSettings ?? throw new ArgumentNullException(nameof(luisSettings));
            this.LuisClient = luisClient ?? throw new ArgumentNullException(nameof(luisClient));
        }

        private static ILogger Logger => LazyLogger.Value;

        private static Lazy<ILogger> LazyLogger { get; } = new Lazy<ILogger>(() => ApplicationLogger.LoggerFactory.CreateLogger<LuisNLUTestClient>());

        private string LuisAppId { get; }

        private LuisSettings LuisSettings { get; }

        private ILuisTestClient LuisClient { get; }

        /// <inheritdoc />
        protected override async Task<Models.LabeledUtterance> TestAsync(
            LuisNLUQuery query,
            CancellationToken cancellationToken)
        {
            var luisResult = await this.LuisClient.QueryAsync(this.LuisAppId, query.PredictionRequest, cancellationToken).ConfigureAwait(false);
            return this.LuisResultToLabeledUtterance(luisResult);
        }

        /// <inheritdoc />
        protected override async Task<Models.LabeledUtterance> TestSpeechAsync(
            string speechFile,
            LuisNLUQuery query,
            CancellationToken cancellationToken)
        {
            var luisResult = await this.LuisClient.RecognizeSpeechAsync(this.LuisAppId, speechFile, query?.PredictionRequest, cancellationToken).ConfigureAwait(false);
            return this.LuisResultToLabeledUtterance(luisResult);
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            this.LuisClient.Dispose();
        }

        private static IEnumerable<Entity> GetEntities(string utterance, IDictionary<string, object> entities, IDictionary<string, string> mappedTypes)
        {
            if (entities == null)
            {
                return null;
            }

            Entity getEntity(string entityType, JToken entityJson, JToken entityMetadata)
            {
                var startIndex = entityMetadata.Value<int>("startIndex");
                var length = entityMetadata.Value<int>("length");
                var score = entityMetadata.Value<double?>("score");
                var matchText = utterance.Substring(startIndex, length);
                var matchIndex = 0;
                var currentStart = 0;
                var nextStart = 0;
                while ((nextStart = utterance.IndexOf(matchText, currentStart, StringComparison.Ordinal)) != startIndex)
                {
                    ++matchIndex;
                    currentStart = nextStart + 1;
                }

                // TODO: support complex entity resolution values
                var entityValue = entityJson.Type == JTokenType.String
                    ? entityJson.Value<string>()
                    : null;

                var modifiedEntityType = entityType;
                if (mappedTypes.TryGetValue(entityType, out var mappedEntityType))
                {
                    modifiedEntityType = mappedEntityType;
                }

                return score.HasValue
                    ? new ScoredEntity(modifiedEntityType, entityValue, matchText, matchIndex, score.Value)
                    : new Entity(modifiedEntityType, entityValue, matchText, matchIndex);
            }

            var instanceMetadata = default(JObject);
            if (entities.TryGetValue("$instance", out var instanceJson))
            {
                instanceMetadata = instanceJson as JObject;
            }

            return entities
                .Where(pair => pair.Key != "$instance")
                .Select(pair =>
                    new
                    {
                        EntityType = pair.Key,
                        Entities = ((JArray)pair.Value).Zip(
                            instanceMetadata?[pair.Key],
                            (entityValue, entityMetadata) =>
                                new
                                {
                                    EntityValue = entityValue,
                                    EntityMetadata = entityMetadata
                                })
                    })
                .SelectMany(entityInfo =>
                    entityInfo.Entities.Select(entity =>
                        getEntity(entityInfo.EntityType, entity.EntityValue, entity.EntityMetadata)));
        }

        private Models.LabeledUtterance LuisResultToLabeledUtterance(PredictionResponse predictionResponse)
        {
            if (predictionResponse == null)
            {
                return new Models.LabeledUtterance(null, null, null);
            }

            var mappedTypes = this.LuisSettings.PrebuiltEntityTypes
                .ToDictionary(pair => $"builtin.{pair.Value}", pair => pair.Key);

            var intent = predictionResponse.Prediction.TopIntent;
            var entities = GetEntities(predictionResponse.Query, predictionResponse.Prediction.Entities, mappedTypes)?.ToList();
            var intentData = default(Intent);
            predictionResponse.Prediction.Intents?.TryGetValue(intent, out intentData);
            return intentData != null && intentData.Score.HasValue
                ? new ScoredLabeledUtterance(predictionResponse.Query, intent, intentData.Score.Value, entities)
                : new Models.LabeledUtterance(predictionResponse.Query, intent, entities);
        }
    }
}
