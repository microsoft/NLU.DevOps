// Copyright (c) Microsoft Corporation.
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
        /// <param name="luisConfiguration">LUIS configuration.</param>
        /// <param name="luisTemplate">LUIS app template.</param>
        /// <param name="luisClient">LUIS client.</param>
        public LuisNLUTrainClient(ILuisConfiguration luisConfiguration, LuisApp luisTemplate, ILuisTrainClient luisClient)
        {
            this.LuisConfiguration = luisConfiguration ?? throw new ArgumentNullException(nameof(luisConfiguration));
            this.LuisTemplate = luisTemplate ?? throw new ArgumentNullException(nameof(luisTemplate));
            this.LuisClient = luisClient ?? throw new ArgumentNullException(nameof(luisClient));
            this.LuisAppId = luisConfiguration.AppId;
            this.LuisAppCreated = luisConfiguration.AppCreated;
        }

        /// <summary>
        /// Gets the LUIS app ID.
        /// </summary>
        public string LuisAppId { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the app was created.
        /// </summary>
        /// <remarks>
        /// Used by the <see cref="CleanupAsync(CancellationToken)"/> method to
        /// determine whether the LUIS application should be cleaned up.
        /// </remarks>
        public bool LuisAppCreated { get; private set; }

        private static ILogger Logger => LazyLogger.Value;

        private static Lazy<ILogger> LazyLogger { get; } = new Lazy<ILogger>(() => ApplicationLogger.LoggerFactory.CreateLogger<LuisNLUTrainClient>());

        private ILuisConfiguration LuisConfiguration { get; }

        private LuisApp LuisTemplate { get; }

        private ILuisTrainClient LuisClient { get; }

        /// <inheritdoc />
        public async Task TrainAsync(
            IEnumerable<Models.LabeledUtterance> utterances,
            CancellationToken cancellationToken)
        {
            try
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
                    this.LuisAppId = await this.LuisClient.CreateAppAsync(this.LuisConfiguration.AppName, cancellationToken).ConfigureAwait(false);
                    this.LuisAppCreated = true;
                    Logger.LogTrace($"Created LUIS app '{this.LuisConfiguration.AppName}' with ID '{this.LuisConfiguration}'.");
                }

                // Create LUIS import JSON
                var luisApp = this.CreateLuisApp(utterances);

                // Import the LUIS model
                Logger.LogTrace($"Importing LUIS app '{this.LuisConfiguration.AppName ?? this.LuisAppId}' version '{this.LuisConfiguration.VersionId}'.");
                await this.LuisClient.ImportVersionAsync(this.LuisAppId, this.LuisConfiguration.VersionId, luisApp, cancellationToken).ConfigureAwait(false);

                // Train the LUIS model
                Logger.LogTrace($"Training LUIS app '{this.LuisConfiguration.AppName ?? this.LuisAppId}' version '{this.LuisConfiguration.VersionId}'.");
                await this.LuisClient.TrainAsync(this.LuisAppId, this.LuisConfiguration.VersionId, cancellationToken).ConfigureAwait(false);

                // Wait for training to complete
                await this.PollTrainingStatusAsync(cancellationToken).ConfigureAwait(false);

                // Publishes the LUIS app version
                Logger.LogTrace($"Publishing LUIS app '{this.LuisConfiguration.AppName ?? this.LuisAppId}' version '{this.LuisConfiguration.VersionId}'.");
                await this.LuisClient.PublishAppAsync(this.LuisAppId, this.LuisConfiguration.VersionId, cancellationToken).ConfigureAwait(false);
            }
            catch (ErrorResponseException ex)
            {
                if (ex.Body == null)
                {
                    Logger.LogError($"Received error with message '{ex.Message}'.");
                }
                else
                {
                    Logger.LogError($"Received error with status code '{ex.Body.Code}' and message '{ex.Body.Message}'.");
                }

                throw;
            }
        }

        /// <inheritdoc />
        public Task CleanupAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (this.LuisAppId == null)
                {
                    throw new InvalidOperationException(
                        $"The '{nameof(this.LuisAppId)}' must be set before calling '{nameof(LuisNLUTrainClient.CleanupAsync)}'.");
                }

                return this.LuisAppCreated
                    ? this.LuisClient.DeleteAppAsync(this.LuisAppId, cancellationToken)
                    : this.LuisClient.DeleteVersionAsync(this.LuisAppId, this.LuisConfiguration.VersionId, cancellationToken);
            }
            catch (ErrorResponseException ex)
            {
                if (ex.Body == null)
                {
                    Logger.LogError($"Received error with message '{ex.Message}'.");
                }
                else
                {
                    Logger.LogError($"Received error with status code '{ex.Body.Code}' and message '{ex.Body.Message}'.");
                }

                throw;
            }
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
                .Select(utterance => utterance.ToJSONUtterance(luisApp))
                .ToList()
                .ForEach(luisApp.Utterances.Add);

            return luisApp;
        }

        private LuisApp CreateLuisAppTemplate()
        {
            var defaultTemplate = new LuisApp(
                name: this.LuisConfiguration.AppName,
                versionId: this.LuisConfiguration.VersionId,
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
            templateJson.Merge(JObject.FromObject(this.LuisTemplate));
            return templateJson.ToObject<LuisApp>();
        }

        private async Task PollTrainingStatusAsync(CancellationToken cancellationToken)
        {
            while (true)
            {
                try
                {
                    var trainingStatus = await this.LuisClient.GetTrainingStatusAsync(this.LuisAppId, this.LuisConfiguration.VersionId, cancellationToken).ConfigureAwait(false);
                    var inProgress = trainingStatus
                        .Select(modelInfo => modelInfo.Details.Status)
                        .Any(status => status == "InProgress" || status == "Queued");

                    if (!inProgress)
                    {
                        if (trainingStatus.Any(modelInfo => modelInfo.Details.Status == "Fail"))
                        {
                            var failureReasons = trainingStatus
                                .Where(modelInfo => modelInfo.Details.Status == "Fail")
                                .Select(modelInfo => $"- {modelInfo.Details.FailureReason}");

                            throw new InvalidOperationException($"Failure occurred while training LUIS model:\n{string.Join('\n', failureReasons)}");
                        }

                        break;
                    }

                    Logger.LogTrace($"Training jobs not complete. Polling again.");
                    await Task.Delay(TrainStatusDelay, cancellationToken).ConfigureAwait(false);
                }
                catch (ErrorResponseException ex)
                when ((int)ex.Response.StatusCode == 429)
                {
                    Logger.LogTrace("Received HTTP 429 result from LUIS. Retrying.");
                    await Task.Delay(TrainStatusDelay, cancellationToken).ConfigureAwait(false);
                }
            }
        }
    }
}
