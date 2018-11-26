// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace LanguageUnderstanding.Luis
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using Models;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Train, test, and cleanup a LUIS model.
    /// Implementation of <see cref="ILanguageUnderstandingService"/>
    /// </summary>
    public sealed class LuisLanguageUnderstandingService : ILanguageUnderstandingService, IDisposable
    {
        private const int DegreeOfParallelism = 3;

        private static readonly TimeSpan TrainStatusDelay = TimeSpan.FromSeconds(2);

        internal LuisLanguageUnderstandingService(string appName, string appId, string appVersion, ILuisClient luisClient)
        {
            this.AppName = appName ?? throw new ArgumentNullException(nameof(appName));
            this.AppId = appId;
            this.AppVersion = appVersion ?? "0.1.1";
            this.LuisClient = luisClient ?? throw new ArgumentNullException(nameof(luisClient));
        }

        /// <summary>
        /// Gets the name of the LUIS app.
        /// </summary>
        public string AppName { get; }

        /// <summary>
        /// Gets the LUIS app ID.
        /// </summary>
        public string AppId { get; private set; }

        /// <summary>
        /// Gets the LUIS app version.
        /// </summary>
        public string AppVersion { get; }

        private ILuisClient LuisClient { get; }

        /// <inheritdoc />
        public async Task TrainAsync(
            IEnumerable<LabeledUtterance> utterances,
            IEnumerable<EntityType> entityTypes,
            CancellationToken cancellationToken)
        {
            // Validate arguments
            ValidateTrainingArguments(utterances, entityTypes);

            // Create application if not passed in.
            if (this.AppId == null)
            {
                this.AppId = await this.LuisClient.CreateAppAsync(this.AppName, cancellationToken).ConfigureAwait(false);
            }

            // Create LUIS import JSON
            var importJson = this.CreateImportJson(utterances, entityTypes);

            // Import the LUIS model
            await this.LuisClient.ImportVersionAsync(this.AppId, this.AppVersion, importJson, cancellationToken).ConfigureAwait(false);

            // Train the LUIS model
            await this.LuisClient.TrainAsync(this.AppId, this.AppVersion, cancellationToken).ConfigureAwait(false);

            // Wait for training to complete
            await this.PollTrainingStatusAsync(cancellationToken).ConfigureAwait(false);

            // Publishes the LUIS app version
            await this.LuisClient.PublishAppAsync(this.AppId, this.AppVersion, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public Task<IEnumerable<LabeledUtterance>> TestAsync(
            IEnumerable<string> utterances,
            IEnumerable<EntityType> entityTypes,
            CancellationToken cancellationToken)
        {
            if (utterances == null)
            {
                throw new ArgumentNullException(nameof(utterances));
            }

            if (entityTypes == null)
            {
                throw new ArgumentNullException(nameof(entityTypes));
            }

            if (entityTypes.Any(entityType => entityType == null))
            {
                throw new ArgumentException("Entity types must not be null.", nameof(entityTypes));
            }

            if (this.AppId == null)
            {
                throw new InvalidOperationException(
                    $"The '{nameof(this.AppId)}' must be set before calling '{nameof(LuisLanguageUnderstandingService.TestAsync)}'.");
            }

            async Task<LabeledUtterance> selector(string utterance)
            {
                if (utterance == null)
                {
                    throw new ArgumentException("Utterances must not be null.", nameof(utterances));
                }

                var json = await this.LuisClient.QueryAsync(this.AppId, utterance, cancellationToken).ConfigureAwait(false);
                return IntentJsonToLabeledUtterance(json, entityTypes);
            }

            return SelectAsync(utterances, selector);
        }

        /// <inheritdoc />
        public Task<IEnumerable<LabeledUtterance>> TestSpeechAsync(
            IEnumerable<string> speechFiles,
            IEnumerable<EntityType> entityTypes,
            CancellationToken cancellationToken)
        {
            if (speechFiles == null)
            {
                throw new ArgumentNullException(nameof(speechFiles));
            }

            if (entityTypes == null)
            {
                throw new ArgumentNullException(nameof(entityTypes));
            }

            if (entityTypes.Any(entityType => entityType == null))
            {
                throw new ArgumentException("Entity types must not be null.", nameof(entityTypes));
            }

            if (this.AppId == null)
            {
                throw new InvalidOperationException(
                    $"The '{nameof(this.AppId)}' must be set before calling '{nameof(LuisLanguageUnderstandingService.TestSpeechAsync)}'.");
            }

            async Task<LabeledUtterance> selector(string speechFile)
            {
                if (speechFile == null)
                {
                    throw new ArgumentException("Speech files must not be null.", nameof(speechFiles));
                }

                var jsonResult = await this.LuisClient.RecognizeSpeechAsync(this.AppId, speechFile, cancellationToken).ConfigureAwait(false);
                return IntentJsonToLabeledUtterance(jsonResult, entityTypes);
            }

            return SelectAsync(speechFiles, selector);
        }

        /// <inheritdoc />
        public Task CleanupAsync(CancellationToken cancellationToken)
        {
            if (this.AppId == null)
            {
                throw new InvalidOperationException(
                    $"The '{nameof(this.AppId)}' must be set before calling '{nameof(LuisLanguageUnderstandingService.CleanupAsync)}'.");
            }

            return this.LuisClient.DeleteAppAsync(this.AppId, cancellationToken);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            this.LuisClient.Dispose();
        }

        private static void ValidateTrainingArguments(IEnumerable<LabeledUtterance> utterances, IEnumerable<EntityType> entityTypes)
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

        private static LabeledUtterance IntentJsonToLabeledUtterance(JObject intentJson, IEnumerable<EntityType> entityTypes)
        {
            if (intentJson == null)
            {
                return new LabeledUtterance(null, null, null);
            }

            var renamedEntityTypes = entityTypes
                .OfType<BuiltinEntityType>()
                .ToDictionary(entityType => $"builtin.{entityType.BuiltinId}", entityType => entityType.Name);

            var text = intentJson.Value<string>("query");
            var intent = intentJson.SelectToken(".topScoringIntent.intent").Value<string>();

            var array = (JArray)intentJson["entities"];
            var entities = new List<Entity>(array.Count);
            foreach (var item in array)
            {
                var entityType = item.Value<string>("type");
                if (renamedEntityTypes.TryGetValue(entityType, out var renamedEntityType))
                {
                    entityType = renamedEntityType;
                }

                var entityValue = GetEntityValue(item);
                var startCharIndex = item.Value<int>("startIndex");
                var endCharIndex = item.Value<int>("endIndex");

                var matchText = item.Value<string>("entity");
                var matches = Regex.Matches(text, matchText, RegexOptions.IgnoreCase);
                var matchIndex = -1;
                for (var i = 0; i < matches.Count; ++i)
                {
                    if (matches[i].Index == startCharIndex)
                    {
                        matchIndex = i;
                        break;
                    }
                }

                Debug.Assert(matchIndex >= 0, "Invalid LUIS response.");
                entities.Add(new Entity(entityType, entityValue, matchText, matchIndex));
            }

            return new LabeledUtterance(text, intent, entities);
        }

        private static string GetEntityValue(JToken entityJson)
        {
            var resolution = entityJson["resolution"];
            if (resolution == null)
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

        private static async Task<IEnumerable<TResult>> SelectAsync<T, TResult>(IEnumerable<T> items, Func<T, Task<TResult>> selector)
        {
            var indexedItems = items.Select((item, i) => new { Item = item, Index = i });
            var results = new TResult[items.Count()];
            var tasks = new List<Task<Tuple<int, TResult>>>(DegreeOfParallelism);

            async Task<Tuple<int, TResult>> selectWithIndexAsync(T item, int i)
            {
                var result = await selector(item).ConfigureAwait(false);
                return Tuple.Create(i, result);
            }

            foreach (var indexedItem in indexedItems)
            {
                if (tasks.Count == DegreeOfParallelism)
                {
                    var task = await Task.WhenAny(tasks).ConfigureAwait(false);
                    tasks.Remove(task);
                    var result = await task.ConfigureAwait(false);
                    results[/* (int) */ result.Item1] = /* (TResult) */ result.Item2;
                }

                tasks.Add(selectWithIndexAsync(indexedItem.Item, indexedItem.Index));
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);
            foreach (var task in tasks)
            {
                var result = await task.ConfigureAwait(false);
                results[/* (int) */ result.Item1] = /* (TResult) */ result.Item2;
            }

            return results;
        }

        private JObject CreateImportJson(IEnumerable<LabeledUtterance> utterances, IEnumerable<EntityType> entityTypes)
        {
            // Get boilerplate JObject
            var importJson = this.GetModelStarter();

            // Add intents to model
            var intents = utterances
                .Select(utterance => utterance.Intent)
                .Append("None")
                .Distinct()
                .Select(intent => new JObject { { "name", intent } });
            var intentsArray = (JArray)importJson["intents"];
            intentsArray.AddRange(intents);

            // Add utterances to model
            var luisUtterances = utterances
                .Select(item => JObject.FromObject(LuisLabeledUtterance.FromLabeledUtterance(item, entityTypes)));
            var utteranceArray = (JArray)importJson["utterances"];
            utteranceArray.AddRange(luisUtterances);

            // Add entities to model
            var entitiesArray = (JArray)importJson["entities"];
            var prebuiltEntitiesArray = (JArray)importJson["prebuiltEntities"];
            var closedListsArray = (JArray)importJson["closedLists"];
            foreach (var entityType in entityTypes)
            {
                switch (entityType.Kind)
                {
                    case EntityTypeKind.Simple:
                        entitiesArray.Add(new JObject
                        {
                            { "name", entityType.Name },
                            { "children", new JArray() },
                            { "roles", new JArray() },
                        });
                        break;
                    case EntityTypeKind.Builtin:
                        var builtinEntityType = (BuiltinEntityType)entityType;
                        prebuiltEntitiesArray.Add(new JObject
                        {
                            { "name", builtinEntityType.BuiltinId },
                            { "roles", new JArray() },
                        });
                        break;
                    case EntityTypeKind.List:
                        var listEntityType = (ListEntityType)entityType;
                        var subLists = listEntityType.Values
                            .Select(value => new JObject
                            {
                                { "canonicalForm", value.CanonicalForm },
                                { "list", JArray.FromObject(value.Synonyms) },
                            });
                        var subListsJson = new JArray();
                        subListsJson.AddRange(subLists);
                        closedListsArray.Add(new JObject
                        {
                            { "name", listEntityType.Name },
                            { "roles", new JArray() },
                            { "subLists", subListsJson },
                        });
                        break;
                    default:
                        throw new NotImplementedException($"Entity type '{entityType.Kind}' has not been implemented.");
                }
            }

            return importJson;
        }

        private JObject GetModelStarter()
        {
            return new JObject
            {
                { "luis_schema_version", "3.0.0" },
                { "versionId", this.AppVersion },
                { "name", this.AppName },
                { "desc", string.Empty },
                { "culture", "en-us" },
                { "intents", new JArray() },
                { "entities", new JArray() },
                { "composites", new JArray() },
                { "closedLists", new JArray() },
                { "patternAnyEntities", new JArray() },
                { "regex_entities", new JArray() },
                { "prebuiltEntities", new JArray() },
                { "model_features", new JArray() },
                { "regex_features", new JArray() },
                { "patterns", new JArray() },
                { "utterances", new JArray() },
            };
        }

        private async Task PollTrainingStatusAsync(CancellationToken cancellationToken)
        {
            JArray trainStatusJson;
            while (true)
            {
                trainStatusJson = await this.LuisClient.GetTrainingStatusAsync(this.AppId, this.AppVersion, cancellationToken).ConfigureAwait(false);
                var inProgress = trainStatusJson.SelectTokens("[*].details.status")
                    .Select(statusJson => statusJson.Value<string>())
                    .Any(status => status == "InProgress" || status == "Queued");

                if (!inProgress)
                {
                    break;
                }

                await Task.Delay(TrainStatusDelay, cancellationToken).ConfigureAwait(false);
            }

            // Ensure no failures occurred while training
            var failures = trainStatusJson.SelectTokens("[?(@.details.status == 'Fail')]");
            if (failures.Any())
            {
                throw new InvalidOperationException("Failure occurred while training LUIS model.");
            }
        }
    }
}
