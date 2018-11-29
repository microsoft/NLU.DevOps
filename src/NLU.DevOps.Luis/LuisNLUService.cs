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

        internal LuisNLUService(string appName, string appId, string versionId, ILuisClient luisClient)
        {
            this.LuisAppName = appName ?? (appId != null ? default(string) : throw new ArgumentNullException(nameof(appName)));
            this.LuisAppId = appId;
            this.LuisVersionId = versionId ?? "0.1.1";
            this.LuisClient = luisClient;
        }

        /// <summary>
        /// Gets the name of the LUIS app.
        /// </summary>
        public string LuisAppName { get; }

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
                this.LuisAppId = await this.LuisClient.CreateAppAsync(this.LuisAppName, cancellationToken).ConfigureAwait(false);
                Logger.LogTrace($"Created LUIS app '{this.LuisAppName}' with ID '{this.LuisAppId}'.");
            }

            // Create LUIS import JSON
            var luisApp = this.CreateLuisApp(utterances, entityTypes);

            // Import the LUIS model
            Logger.LogTrace($"Importing LUIS app '{this.LuisAppName ?? this.LuisAppId}' version '{this.LuisVersionId}'.");
            await this.LuisClient.ImportVersionAsync(this.LuisAppId, this.LuisVersionId, luisApp, cancellationToken).ConfigureAwait(false);

            // Train the LUIS model
            Logger.LogTrace($"Training LUIS app '{this.LuisAppName ?? this.LuisAppId}' version '{this.LuisVersionId}'.");
            await this.LuisClient.TrainAsync(this.LuisAppId, this.LuisVersionId, cancellationToken).ConfigureAwait(false);

            // Wait for training to complete
            await this.PollTrainingStatusAsync(cancellationToken).ConfigureAwait(false);

            // Publishes the LUIS app version
            Logger.LogTrace($"Publishing LUIS app '{this.LuisAppName ?? this.LuisAppId}' version '{this.LuisVersionId}'.");
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
            var luisApp = new LuisApp(
                name: this.LuisAppName,
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

            // Add intents to model
            luisApp.Intents = utterances
                .Select(utterance => utterance.Intent)
                .Append("None")
                .Distinct()
                .Select(intent => new HierarchicalModel { Name = intent })
                .ToList();

            // Add utterances to model
            luisApp.Utterances = utterances
                .Select(utterance => utterance.ToJSONUtterance(entityTypes))
                .ToList();

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

            return luisApp;
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
