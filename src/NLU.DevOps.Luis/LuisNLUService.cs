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
        public async Task TrainAsync(
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
        public async Task<Models.LabeledUtterance> TestAsync(
            string utterance,
            CancellationToken cancellationToken)
        {
            if (utterance == null)
            {
                throw new ArgumentNullException(nameof(utterance));
            }

            if (this.LuisAppId == null)
            {
                throw new InvalidOperationException(
                    $"The '{nameof(this.LuisAppId)}' must be set before calling '{nameof(LuisNLUService.TestAsync)}'.");
            }

            var luisResult = await this.LuisClient.QueryAsync(this.LuisAppId, utterance, cancellationToken).ConfigureAwait(false);
            return this.LuisResultToLabeledUtterance(luisResult);
        }

        /// <inheritdoc />
        public async Task<Models.LabeledUtterance> TestSpeechAsync(
            string speechFile,
            CancellationToken cancellationToken)
        {
            if (speechFile == null)
            {
                throw new ArgumentNullException(nameof(speechFile));
            }

            if (this.LuisAppId == null)
            {
                throw new InvalidOperationException(
                    $"The '{nameof(this.LuisAppId)}' must be set before calling '{nameof(LuisNLUService.TestSpeechAsync)}'.");
            }

            var luisResult = await this.LuisClient.RecognizeSpeechAsync(this.LuisAppId, speechFile, cancellationToken).ConfigureAwait(false);
            return this.LuisResultToLabeledUtterance(luisResult);
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
                    .Any(status => status == "InProgress" || status == "Queued");

                if (!inProgress)
                {
                    if (trainingStatus.Any(status => status == "Fail"))
                    {
                        throw new InvalidOperationException("Failure occurred while training LUIS model.");
                    }

                    break;
                }

                Logger.LogTrace($"Training jobs not complete. Polling again.");
                await Task.Delay(TrainStatusDelay, cancellationToken).ConfigureAwait(false);
            }
        }

        private Models.LabeledUtterance LuisResultToLabeledUtterance(LuisResult luisResult)
        {
            if (luisResult == null)
            {
                return new Models.LabeledUtterance(null, null, null);
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

                var entityScore = default(double?);
                if (entity.AdditionalProperties != null &&
                    entity.AdditionalProperties.TryGetValue("score", out var scoreProperty) &&
                    scoreProperty is double scoreValue)
                {
                    entityScore = scoreValue;
                }

                return entityScore.HasValue
                    ? new ScoredEntity(entityType, entityValue, matchText, matchIndex, entityScore.Value)
                    : new Entity(entityType, entityValue, matchText, matchIndex);
            }

            var intent = luisResult.TopScoringIntent?.Intent;
            var score = luisResult.TopScoringIntent?.Score;
            var entities = luisResult.Entities?.Select(getEntity).ToList();
            return !score.HasValue
                ? new Models.LabeledUtterance(luisResult.Query, intent, entities)
                : new ScoredLabeledUtterance(luisResult.Query, intent, score.Value, entities);
        }
    }
}
