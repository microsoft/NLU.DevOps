// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace NLU.DevOps.Luis
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
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
    public sealed class LuisNLUService : INLUService, IDisposable
    {
        private static readonly TimeSpan TrainStatusDelay = TimeSpan.FromSeconds(2);

        /// <summary>
        /// Initializes a new instance of the <see cref="LuisNLUService"/> class.
        /// </summary>
        /// <param name="appId">App ID.</param>
        /// <param name="versionId">Version ID.</param>
        /// <param name="appName">App name.</param>
        /// <param name="appTemplate">App template.</param>
        /// <param name="luisClient">Luis client.</param>
        public LuisNLUService(string appId, string versionId, string appName, LuisApp appTemplate, ILuisClient luisClient)
        {
            if (appName == null && appId == null && appTemplate == null)
            {
                throw new ArgumentNullException(nameof(appName), $"Must supply one of '{nameof(appName)}', '{nameof(appId)}', or '{nameof(appTemplate)}'.");
            }

            this.LuisAppId = appId;
            this.LuisVersionId = versionId ?? "0.1.1";
            this.AppName = appName;
            this.AppTemplate = appTemplate;
            this.LuisClient = luisClient;
        }

        /// <summary>
        /// Gets the LUIS app ID.
        /// </summary>
        public string LuisAppId { get; private set; }

        /// <summary>
        /// Gets the LUIS version ID.
        /// </summary>
        public string LuisVersionId { get; }

        private static ILogger Logger => LazyLogger.Value;

        private static Lazy<ILogger> LazyLogger { get; } = new Lazy<ILogger>(() => ApplicationLogger.LoggerFactory.CreateLogger<LuisNLUService>());

        private string AppName { get; }

        private LuisApp AppTemplate { get; }

        private ILuisClient LuisClient { get; }

        /// <inheritdoc />
        public async Task TrainAsync(
            IEnumerable<Models.LabeledUtterance> utterances,
            IEnumerable<EntityType> entityTypes,
            CancellationToken cancellationToken)
        {
            // Validate arguments
            ValidateTrainingArguments(utterances, entityTypes);

            // Create application if not passed in.
            if (this.LuisAppId == null)
            {
                this.LuisAppId = await this.LuisClient.CreateAppAsync(this.AppName, cancellationToken).ConfigureAwait(false);
                Logger.LogTrace($"Created LUIS app '{this.AppName}' with ID '{this.LuisAppId}'.");
            }

            // Create LUIS import JSON
            var luisApp = this.CreateLuisApp(utterances, entityTypes);

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
        public async Task<Models.LabeledUtterance> TestAsync(
            string utterance,
            IEnumerable<EntityType> entityTypes,
            CancellationToken cancellationToken)
        {
            if (utterance == null)
            {
                throw new ArgumentNullException(nameof(utterance));
            }

            if (entityTypes == null)
            {
                throw new ArgumentNullException(nameof(entityTypes));
            }

            if (entityTypes.Any(entityType => entityType == null))
            {
                throw new ArgumentException("Entity types must not be null.", nameof(entityTypes));
            }

            if (this.LuisAppId == null)
            {
                throw new InvalidOperationException(
                    $"The '{nameof(this.LuisAppId)}' must be set before calling '{nameof(LuisNLUService.TestAsync)}'.");
            }

            var luisResult = await this.LuisClient.QueryAsync(this.LuisAppId, utterance, cancellationToken).ConfigureAwait(false);
            return LuisResultToLabeledUtterance(luisResult, entityTypes);
        }

        /// <inheritdoc />
        public async Task<Models.LabeledUtterance> TestSpeechAsync(
            string speechFile,
            IEnumerable<EntityType> entityTypes,
            CancellationToken cancellationToken)
        {
            if (speechFile == null)
            {
                throw new ArgumentNullException(nameof(speechFile));
            }

            if (entityTypes == null)
            {
                throw new ArgumentNullException(nameof(entityTypes));
            }

            if (entityTypes.Any(entityType => entityType == null))
            {
                throw new ArgumentException("Entity types must not be null.", nameof(entityTypes));
            }

            if (this.LuisAppId == null)
            {
                throw new InvalidOperationException(
                    $"The '{nameof(this.LuisAppId)}' must be set before calling '{nameof(LuisNLUService.TestSpeechAsync)}'.");
            }

            var luisResult = await this.LuisClient.RecognizeSpeechAsync(this.LuisAppId, speechFile, cancellationToken).ConfigureAwait(false);
            return LuisResultToLabeledUtterance(luisResult, entityTypes);
        }

        /// <inheritdoc />
        public Task CleanupAsync(CancellationToken cancellationToken)
        {
            if (this.LuisAppId == null)
            {
                throw new InvalidOperationException(
                    $"The '{nameof(this.LuisAppId)}' must be set before calling '{nameof(LuisNLUService.CleanupAsync)}'.");
            }

            return this.LuisClient.DeleteAppAsync(this.LuisAppId, cancellationToken);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            this.LuisClient.Dispose();
        }

        private static void ValidateTrainingArguments(IEnumerable<Models.LabeledUtterance> utterances, IEnumerable<EntityType> entityTypes)
        {
            if (utterances == null)
            {
                throw new ArgumentNullException(nameof(utterances));
            }

            if (entityTypes == null)
            {
                throw new ArgumentNullException(nameof(entityTypes));
            }

            if (utterances.Any(utterance => utterance == null))
            {
                throw new ArgumentException("Utterances must not be null.", nameof(utterances));
            }

            if (entityTypes.Any(entityType => entityType == null))
            {
                throw new ArgumentException("Entity types must not be null.", nameof(entityTypes));
            }
        }

        private static Models.LabeledUtterance LuisResultToLabeledUtterance(LuisResult luisResult, IEnumerable<EntityType> entityTypes)
        {
            if (luisResult == null)
            {
                return new Models.LabeledUtterance(null, null, null);
            }

            var renamedEntityTypes = entityTypes
                .Where(entityType => entityType.Kind == "prebuiltEntities")
                .ToDictionary(entityType => $"builtin.{entityType.Data.Value<string>("name")}", entityType => entityType.Name);

            Entity getEntity(EntityModel entity)
            {
                var entityType = entity.Type;
                if (entityType != null && renamedEntityTypes.TryGetValue(entityType, out var renamedEntityType))
                {
                    entityType = renamedEntityType;
                }

                var entityValue = GetEntityValue(entity);

                var matchText = entity.Entity;
                var matches = Regex.Matches(luisResult.Query, matchText, RegexOptions.IgnoreCase);
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
                return new Entity(entityType, entityValue, matchText, matchIndex);
            }

            return new Models.LabeledUtterance(
                luisResult.Query,
                luisResult.TopScoringIntent?.Intent,
                luisResult.Entities?.Select(getEntity).ToList());
        }

        private static string GetEntityValue(EntityModel entity)
        {
            var resolution = default(JToken);
            if (entity.AdditionalProperties == null ||
                !entity.AdditionalProperties.TryGetValue("resolution", out var resolutionJson) ||
                (resolution = resolutionJson as JToken) == null)
            {
                return null;
            }

            var value = resolution.Value<string>("value");
            if (value != null)
            {
                return value;
            }

            // TODO: choose "the best" entity value from resolution.
            var resolvedValue = resolution["values"][0];
            return resolvedValue is JObject resolvedObject
                ? resolvedObject.Value<string>("value")
                : resolvedValue.Value<string>();
        }

        private LuisApp CreateLuisApp(IEnumerable<Models.LabeledUtterance> utterances, IEnumerable<EntityType> entityTypes)
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

            // Add entities to model
            foreach (var entityType in entityTypes)
            {
                void addEntityType<T>(IList<T> list, Action<T, string> setName = null)
                    where T : new()
                {
                    var instance = entityType.Data != null ? entityType.Data.ToObject<T>() : new T();
                    setName?.Invoke(instance, entityType.Name);
                    list.Add(instance);
                }

                switch (entityType.Kind)
                {
                    case "entities":
                        addEntityType(luisApp.Entities, (i, n) => i.Name = n);
                        break;
                    case "prebuiltEntities":
                        addEntityType(luisApp.PrebuiltEntities);
                        break;
                    case "closedList":
                        addEntityType(luisApp.ClosedLists, (i, n) => i.Name = n);
                        break;
                    case "model_features":
                        addEntityType(luisApp.ModelFeatures, (i, n) => i.Name = n);
                        break;
                    default:
                        throw new NotImplementedException($"Entity type '{entityType.Kind}' has not been implemented.");
                }
            }

            // Add utterances to model
            luisApp.Utterances = luisApp.Utterances ?? new List<JSONUtterance>();
            utterances
                .Select(utterance => utterance.ToJSONUtterance(entityTypes, luisApp))
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

            if (this.AppTemplate == null)
            {
                return defaultTemplate;
            }

            var templateJson = JObject.FromObject(defaultTemplate);
            templateJson.Merge(JObject.FromObject(this.AppTemplate));
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
    }
}
