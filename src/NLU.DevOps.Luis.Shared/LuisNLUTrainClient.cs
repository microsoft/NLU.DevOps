// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace NLU.DevOps.Luis
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Logging;
    using Microsoft.Azure.CognitiveServices.Language.LUIS.Authoring.Models;
    using Microsoft.Extensions.Logging;
    using Models;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Train and cleanup a LUIS model.
    /// Implementation of <see cref="INLUTrainClient"/>
    /// </summary>
    public sealed class LuisNLUTrainClient : INLUTrainClient
    {
        private static readonly TimeSpan TrainStatusDelay = TimeSpan.FromSeconds(2);

        /// <summary>
        /// Initializes a new instance of the <see cref="LuisNLUTrainClient"/> class.
        /// </summary>
        /// <param name="appName">App name.</param>
        /// <param name="appId">App ID.</param>
        /// <param name="versionId">Version ID.</param>
        /// <param name="luisSettings">LUIS settings.</param>
        /// <param name="luisClient">LUIS client.</param>
        public LuisNLUTrainClient(string appName, string appId, string versionId, LuisSettings luisSettings, ILuisTrainClient luisClient)
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

        private static Lazy<ILogger> LazyLogger { get; } = new Lazy<ILogger>(() => ApplicationLogger.LoggerFactory.CreateLogger<LuisNLUTrainClient>());

        private string AppName { get; }

        private string LuisVersionId { get; }

        private LuisSettings LuisSettings { get; }

        private ILuisTrainClient LuisClient { get; }

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
        public Task CleanupAsync(CancellationToken cancellationToken)
        {
            if (this.LuisAppId == null)
            {
                throw new InvalidOperationException(
                    $"The '{nameof(this.LuisAppId)}' must be set before calling '{nameof(LuisNLUTrainClient.CleanupAsync)}'.");
            }

            return this.LuisClient.DeleteAppAsync(this.LuisAppId, cancellationToken);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            this.LuisClient.Dispose();
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
    }
}
