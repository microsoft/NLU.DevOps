// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace NLU.DevOps.Luis
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Core;
    using Microsoft.Azure.CognitiveServices.Language.LUIS.Runtime.Models;
    using Microsoft.Extensions.Logging;
    using Models;
    using Newtonsoft.Json.Linq;
    using NLU.DevOps.Logging;

    /// <summary>
    /// Test a LUIS model with text and speech.
    /// Implementation of <see cref="INLUTestClient"/>
    /// </summary>
    public sealed class LuisNLUTestClient : LuisNLUBatchTestClientBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LuisNLUTestClient"/> class.
        /// </summary>
        /// <param name="luisConfiguration">LUIS configuration.</param>
        /// <param name="luisClient">LUIS client.</param>
        /// <param name="luisBatchTestClient">LUIS batch test client.</param>
        public LuisNLUTestClient(ILuisConfiguration luisConfiguration, ILuisTestClient luisClient, ILuisBatchTestClient luisBatchTestClient)
            : base(luisConfiguration, luisBatchTestClient)
        {
            this.LuisClient = luisClient ?? throw new ArgumentNullException(nameof(luisClient));
        }

        private static ILogger Logger => LazyLogger.Value;

        private static Lazy<ILogger> LazyLogger { get; } = new Lazy<ILogger>(() => ApplicationLogger.LoggerFactory.CreateLogger<LuisNLUTestClient>());

        private ILuisTestClient LuisClient { get; }

        /// <inheritdoc />
        public override async Task<ILabeledUtterance> TestAsync(
            JToken query,
            CancellationToken cancellationToken)
        {
            try
            {
                if (query == null)
                {
                    throw new ArgumentNullException(nameof(query));
                }

                var predictionRequest = query.ToObject<PredictionRequest>();
                predictionRequest.Query = predictionRequest.Query ?? query.Value<string>("text");
                var luisResult = await this.LuisClient.QueryAsync(predictionRequest, cancellationToken).ConfigureAwait(false);
                return this.LuisResultToLabeledUtterance(new SpeechPredictionResponse(luisResult, 0));
            }
            catch (ErrorException ex)
            {
                if (ex.Body == null)
                {
                    Logger.LogError($"Received error with message '{ex.Message}'.");
                }
                else
                {
                    Logger.LogError($"Received error with status code '{ex.Body.ErrorProperty.Code}' and message '{ex.Body.ErrorProperty.Message}'.");
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

            var predictionRequest = query?.ToObject<PredictionRequest>();
            if (predictionRequest != null)
            {
                predictionRequest.Query = predictionRequest.Query ?? query.Value<string>("text");
            }

            var luisResult = await this.LuisClient.RecognizeSpeechAsync(speechFile, predictionRequest, cancellationToken).ConfigureAwait(false);
            return this.LuisResultToLabeledUtterance(luisResult);
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.LuisClient.Dispose();
            }
        }

        private static IEnumerable<IEntity> GetEntities(string utterance, IDictionary<string, object> entities)
        {
            if (entities == null || entities.Count == 0)
            {
                return null;
            }

            IEnumerable<IEntity> getEntitiesForType(string type, object instances, JToken metadata)
            {
                if (instances is JArray instancesJson)
                {
                    var typeMetadata = metadata?[type];
                    return instancesJson
                        .Zip(
                            typeMetadata,
                            (instance, instanceMetadata) =>
                                getEntitiesRecursive(type, instance, instanceMetadata))
                        .SelectMany(e => e);
                }

                return Array.Empty<IEntity>();
            }

            IEnumerable<IEntity> getEntitiesRecursive(string entityType, JToken entityJson, JToken entityMetadata)
            {
                var startIndex = entityMetadata.Value<int>("startIndex");
                var length = entityMetadata.Value<int>("length");
                var score = entityMetadata.Value<double?>("score");
                var matchText = utterance.Substring(startIndex, length);
                var matchIndex = 0;
                var currentStart = 0;
                while ((currentStart = utterance.IndexOf(matchText, currentStart, StringComparison.Ordinal)) != startIndex)
                {
                    ++matchIndex;
                    currentStart++;
                }

                var entityValue = PruneMetadata(entityJson);
                if (entityJson is JObject entityJsonObject && entityJsonObject.TryGetValue("$instance", out var innerMetadata))
                {
                    var children = ((IDictionary<string, JToken>)entityJsonObject)
                        .SelectMany(pair => getEntitiesForType(pair.Key, pair.Value, innerMetadata));

                    foreach (var child in children)
                    {
                        yield return child;
                    }
                }

                yield return new Entity(entityType, entityValue, matchText, matchIndex)
                    .WithScore(score);
            }

            var globalMetadata = default(JToken);
            if (entities.TryGetValue("$instance", out var metadataValue) && metadataValue is JToken metadataJson)
            {
                globalMetadata = metadataJson;
            }
            else
            {
                throw new InvalidOperationException("Expected top-level metadata for entities.");
            }

            return entities.SelectMany(pair =>
                getEntitiesForType(pair.Key, pair.Value, globalMetadata));
        }

        private static JToken PruneMetadata(JToken json)
        {
            if (json is JObject jsonObject)
            {
                var prunedObject = new JObject();
                foreach (var property in jsonObject.Properties())
                {
                    if (property.Name != "$instance")
                    {
                        prunedObject.Add(property.Name, PruneMetadata(property.Value));
                    }
                }

                return prunedObject;
            }

            if (json is JArray jsonArray)
            {
                var prunedArray = new JArray();
                foreach (var item in jsonArray)
                {
                    prunedArray.Add(PruneMetadata(item));
                }

                return prunedArray;
            }

            return json;
        }

        private ILabeledUtterance LuisResultToLabeledUtterance(SpeechPredictionResponse speechPredictionResponse)
        {
            if (speechPredictionResponse == null)
            {
                return new LabeledUtterance(null, null, null);
            }

            var query = speechPredictionResponse.PredictionResponse.Query;
            var entities = GetEntities(
                    query,
                    speechPredictionResponse.PredictionResponse.Prediction.Entities)?
                .ToList();

            var intent = speechPredictionResponse.PredictionResponse.Prediction.TopIntent;
            var intents = speechPredictionResponse.PredictionResponse.Prediction.Intents?.Select(i => new { Intent = i.Key, i.Value.Score });
            var intentData = default(Intent);
            speechPredictionResponse.PredictionResponse.Prediction.Intents?.TryGetValue(intent, out intentData);
            return new LabeledUtterance(query, intent, entities)
                .WithProperty("intents", intents)
                .WithScore(intentData?.Score)
                .WithTextScore(speechPredictionResponse.TextScore)
                .WithTimestamp(DateTimeOffset.Now);
        }
    }
}
