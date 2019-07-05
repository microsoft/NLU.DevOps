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
    using Microsoft.Azure.CognitiveServices.Language.LUIS.Authoring.Models;
    using Microsoft.Azure.CognitiveServices.Language.LUIS.Runtime.Models;
    using Microsoft.Extensions.Logging;
    using Models;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Train, test, and cleanup a LUIS model.
    /// Implementation of <see cref="INLUService"/>
    /// </summary>
    public sealed class LuisNLUService : NLUServiceBase<LuisNLUQuery>
    {
        private static readonly TimeSpan TrainStatusDelay = TimeSpan.FromSeconds(2);

        /// <summary>
        /// Initializes a new instance of the <see cref="LuisNLUService"/> class.
        /// </summary>
        /// <param name="appName">App name.</param>
        /// <param name="appId">App ID.</param>
        /// <param name="versionId">Version ID.</param>
        /// <param name="luisSettings">LUIS settings.</param>
        /// <param name="luisClient">LUIS client.</param>
        public LuisNLUService(string appName, string appId, string versionId, LuisSettings luisSettings, ILuisClient luisClient)
        {
            this.LuisAppId = appId;
            this.LuisVersionId = versionId ?? "0.1.1";
            this.LuisSettings = luisSettings ?? throw new ArgumentNullException(nameof(luisSettings));
            this.LuisClient = luisClient ?? throw new ArgumentNullException(nameof(luisClient));

            this.AppName = appName ?? ((appId == null && luisSettings.AppTemplate.Name == null)
                ? throw new ArgumentNullException(nameof(appName), $"Must supply one of '{nameof(appName)}', '{nameof(appId)}', or '{nameof(Luis.LuisSettings)}.{nameof(Luis.LuisSettings.AppTemplate)}'.")
                : appName);
        }

        /// <summary>
        /// Gets the LUIS app ID.
        /// </summary>
        public string LuisAppId { get; private set; }

        private static ILogger Logger => LazyLogger.Value;

        private static Lazy<ILogger> LazyLogger { get; } = new Lazy<ILogger>(() => ApplicationLogger.LoggerFactory.CreateLogger<LuisNLUService>());

        private string AppName { get; }

        private string LuisVersionId { get; }

        private LuisSettings LuisSettings { get; }

        private ILuisClient LuisClient { get; }

        /// <inheritdoc />
        public override async Task TrainAsync(
            IEnumerable<Models.LabeledUtterance> utterances,
            CancellationToken cancellationToken)
        {
            // Validate arguments
            if (utterances == null)
            {
                throw new ArgumentNullException(nameof(utterances));
            }

            if (utterances.Any(utterance => utterance == null))
            {
                throw new ArgumentException("Utterances must not be null.", nameof(utterances));
            }

            // Create application if not passed in.
            if (this.LuisAppId == null)
            {
                this.LuisAppId = await this.LuisClient.CreateAppAsync(this.AppName, cancellationToken).ConfigureAwait(false);
                Logger.LogTrace($"Created LUIS app '{this.AppName}' with ID '{this.LuisAppId}'.");
            }

            // Create LUIS import JSON
            var luisApp = this.CreateLuisApp(utterances);

            // Import the LUIS model
            Logger.LogTrace($"Importing LUIS app '{this.AppName ?? this.LuisAppId}' version '{this.LuisVersionId}'.");
            await this.LuisClient.ImportVersionAsync(this.LuisAppId, this.LuisVersionId, luisApp, cancellationToken).ConfigureAwait(false);

            // Train the LUIS model
            Logger.LogTrace($"Training LUIS app '{this.AppName ?? this.LuisAppId}' version '{this.LuisVersionId}'.");
            await this.LuisClient.TrainAsync(this.LuisAppId, this.LuisVersionId, cancellationToken).ConfigureAwait(false);

            // Wait for training to complete
            await this.PollTrainingStatusAsync(cancellationToken).ConfigureAwait(false);

            // Publishes the LUIS app version
            Logger.LogTrace($"Publishing LUIS app '{this.AppName ?? this.LuisAppId}' version '{this.LuisVersionId}'.");
            await this.LuisClient.PublishAppAsync(this.LuisAppId, this.LuisVersionId, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public override Task CleanupAsync(CancellationToken cancellationToken)
        {
            if (this.LuisAppId == null)
            {
                throw new InvalidOperationException(
                    $"The '{nameof(this.LuisAppId)}' must be set before calling '{nameof(LuisNLUService.CleanupAsync)}'.");
            }

            return this.LuisClient.DeleteAppAsync(this.LuisAppId, cancellationToken);
        }

        /// <inheritdoc />
        protected override async Task<Models.LabeledUtterance> TestAsync(
            LuisNLUQuery query,
            CancellationToken cancellationToken)
        {
            if (this.LuisAppId == null)
            {
                throw new InvalidOperationException(
                    $"The '{nameof(this.LuisAppId)}' must be set before calling '{nameof(LuisNLUService.TestAsync)}'.");
            }

            var luisResult = await this.LuisClient.QueryAsync(this.LuisAppId, query.PredictionRequest, cancellationToken).ConfigureAwait(false);
            return this.LuisResultToLabeledUtterance(luisResult);
        }

        /// <inheritdoc />
        protected override async Task<Models.LabeledUtterance> TestSpeechAsync(
            string speechFile,
            LuisNLUQuery query,
            CancellationToken cancellationToken)
        {
            if (this.LuisAppId == null)
            {
                throw new InvalidOperationException(
                    $"The '{nameof(this.LuisAppId)}' must be set before calling '{nameof(LuisNLUService.TestSpeechAsync)}'.");
            }

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

        private LuisApp CreateLuisApp(IEnumerable<Models.LabeledUtterance> utterances)
        {
            var luisApp = this.CreateLuisAppTemplate();

            // Add intents to model
            luisApp.Intents = luisApp.Intents ?? new List<HierarchicalModel>();
            utterances
                .Select(utterance => utterance.Intent)
                .Append("None")
                .Distinct()
                .Where(intent => !luisApp.Intents.Any(i => i.Name == intent))
                .Select(intent => new HierarchicalModel { Name = intent })
                .ToList()
                .ForEach(luisApp.Intents.Add);

            // Add utterances to model
            luisApp.Utterances = luisApp.Utterances ?? new List<JSONUtterance>();
            utterances
                .Select(utterance => utterance.ToJSONUtterance(this.LuisSettings.PrebuiltEntityTypes))
                .ToList()
                .ForEach(luisApp.Utterances.Add);

            return luisApp;
        }

        private LuisApp CreateLuisAppTemplate()
        {
            var defaultTemplate = new LuisApp(
                name: this.AppName,
                versionId: this.LuisVersionId,
                desc: string.Empty,
                culture: "en-us",
                entities: new List<HierarchicalModel>(),
                closedLists: new List<ClosedList>(),
                composites: new List<HierarchicalModel>(),
                patternAnyEntities: new List<PatternAny>(),
                regexEntities: new List<RegexEntity>(),
                prebuiltEntities: new List<PrebuiltEntity>(),
                regexFeatures: new List<JSONRegexFeature>(),
                modelFeatures: new List<JSONModelFeature>(),
                patterns: new List<PatternRule>());

            var templateJson = JObject.FromObject(defaultTemplate);
            templateJson.Merge(JObject.FromObject(this.LuisSettings.AppTemplate));
            return templateJson.ToObject<LuisApp>();
        }

        private async Task PollTrainingStatusAsync(CancellationToken cancellationToken)
        {
            while (true)
            {
                var trainingStatus = await this.LuisClient.GetTrainingStatusAsync(this.LuisAppId, this.LuisVersionId, cancellationToken).ConfigureAwait(false);
                var inProgress = trainingStatus
                    .Select(modelInfo => modelInfo.Details.Status)
                    .Any(status => status == "InProgress" || status == "Queued");

                if (!inProgress)
                {
                    if (trainingStatus.Any(modelInfo => modelInfo.Details.Status == "Fail"))
                    {
                        throw new InvalidOperationException("Failure occurred while training LUIS model.");
                    }

                    break;
                }

                Logger.LogTrace($"Training jobs not complete. Polling again.");
                await Task.Delay(TrainStatusDelay, cancellationToken).ConfigureAwait(false);
            }
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
