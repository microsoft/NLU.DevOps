// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace LanguageUnderstanding.Luis
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Net;
    using System.Runtime.CompilerServices;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using Models;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Train, test, and cleanup a LUIS model.
    /// Implementation of <see cref="ILanguageUnderstandingService"/>
    /// </summary>
    public sealed class LuisLanguageUnderstandingService : ILanguageUnderstandingService, IDisposable
    {
        /// <summary>Maximum number of tasks that are running simultaneously. </summary>
        private const int DegreeOfParallelism = 3;

        /// <summary> The protocol used in LUIS http requests. </summary>
        private const string Protocol = "https://";

        /// <summary> All the static domains/subdomains to construct LUIS host address. </summary>
        private const string Domain = ".api.cognitive.microsoft.com";

        /// <summary> Base path for LUIS API calls. </summary>
        private const string BasePath = "/luis/api/v2.0/apps/";

        /// <summary> Base path for LUIS queries. </summary>
        private const string QueryBasePath = "/luis/v2.0/apps/";

        /// <summary> The delay to use to throttle LUIS queries. </summary>
        private static readonly TimeSpan ThrottleQueryDelay = TimeSpan.FromMilliseconds(100);

        /// <summary> The delay to use when polling for LUIS training status. </summary>
        private static readonly TimeSpan TrainStatusDelay = TimeSpan.FromSeconds(2);

        /// <summary>
        /// Initializes a new instance of the <see cref="LuisLanguageUnderstandingService"/> class.
        /// </summary>
        /// <param name="appName">LUIS application name.</param>
        /// <param name="appId">LUIS application id.</param>
        /// <param name="appVersion">LUIS application version.</param>
        /// <param name="isStaging">Signals whether to use the staging endpoint.</param>
        /// <param name="authoringRegion">LUIS authoring region.</param>
        /// <param name="endpointRegion">LUIS endpoint region.</param>
        /// <param name="luisClient">LUIS client.</param>
        internal LuisLanguageUnderstandingService(string appName, string appId, string appVersion, bool isStaging, string authoringRegion, string endpointRegion, ILuisClient luisClient)
        {
            this.AppName = appName ?? throw new ArgumentNullException(nameof(appName));
            this.AppId = appId;
            this.AppVersion = appVersion ?? "0.2";
            this.IsStaging = isStaging;
            this.AuthoringRegion = authoringRegion;
            this.EndpointRegion = endpointRegion;
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

        /// <summary>
        /// Gets a value indicating whether the LUIS app is staging.
        /// </summary>
        public bool IsStaging { get; }

        /// <summary> Gets the LUIS authoring region. </summary>
        private string AuthoringRegion { get; }

        /// <summary> Gets the LUIS endpoint region. </summary>
        private string EndpointRegion { get; }

        /// <summary> Gets the client to make HTTP requests to LUIS. </summary>
        private ILuisClient LuisClient { get; }

        /// <summary> Gets host for LUIS API calls.</summary>
        private string Host => $"{Protocol}{this.AuthoringRegion}{Domain}";

        /// <summary> Gets full path for LUIS API calls. Contains appId.</summary>
        private string AppIdPath => $"{BasePath}{this.AppId}/";

        /// <summary> Gets path for LUIS API calls. Contains the appId and appVersion.</summary>
        private string AppVersionPath => $"{this.AppIdPath}versions/{this.AppVersion}/";

        /// <inheritdoc />
        public async Task TrainAsync(
            IEnumerable<LabeledUtterance> utterances,
            IEnumerable<EntityType> entityTypes,
            CancellationToken cancellationToken)
        {
            // Validate arguments
            ValidateTrainingArguments(utterances, entityTypes);

            this.EnsureAuthoringRegion();

            // Create application if not passed in.
            if (this.AppId == null)
            {
                this.AppId = await this.CreateAppAsync(cancellationToken).ConfigureAwait(false);
            }

            // Create LUIS import JSON
            var model = this.CreateImportJson(utterances, entityTypes);

            // Import the LUIS model
            await this.ImportVersionAsync(model, cancellationToken).ConfigureAwait(false);

            // Train the LUIS model
            var trainingUri = new Uri($"{this.Host}{this.AppVersionPath}train");
            var trainResponse = await this.LuisClient.PostAsync(trainingUri, null, cancellationToken).ConfigureAwait(false);
            trainResponse.EnsureSuccessStatusCode();

            // Wait for training to complete
            await this.PollTrainingStatusAsync(trainingUri, cancellationToken).ConfigureAwait(false);

            // Publishes the LUIS app version
            await this.PublishAppAsync(cancellationToken).ConfigureAwait(false);
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

            this.EnsureAuthoringRegion();

            async Task<LabeledUtterance> selector(string utterance)
            {
                if (utterance == null)
                {
                    throw new ArgumentException("Utterances must not be null.", nameof(utterances));
                }

                var staging = this.IsStaging ? "&staging=true" : string.Empty;
                var uri = new Uri($"{this.Host}{QueryBasePath}{this.AppId}?q={utterance}{staging}");
                while (true)
                {
                    var response = await this.LuisClient.GetAsync(uri, cancellationToken).ConfigureAwait(false);
                    if (response.StatusCode == (HttpStatusCode)429)
                    {
                        await Task.Delay(ThrottleQueryDelay, cancellationToken).ConfigureAwait(false);
                        continue;
                    }

                    response.EnsureSuccessStatusCode();
                    var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    return PredictionToLabeledUtterance(json, entityTypes);
                }
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

            this.EnsureEndpointRegion();

            async Task<LabeledUtterance> selector(string speechFile)
            {
                if (speechFile == null)
                {
                    throw new ArgumentException("Speech files must not be null.", nameof(speechFiles));
                }

                var jsonResult = await this.LuisClient.RecognizeSpeechAsync(this.AppId, speechFile, cancellationToken).ConfigureAwait(false);
                return PredictionToLabeledUtterance(jsonResult, entityTypes);
            }

            return SelectAsync(speechFiles, selector);
        }

        /// <inheritdoc />
        public async Task CleanupAsync(CancellationToken cancellationToken)
        {
            if (this.AppId == null)
            {
                throw new InvalidOperationException(
                    $"The '{nameof(this.AppId)}' must be set before calling '{nameof(LuisLanguageUnderstandingService.CleanupAsync)}'.");
            }

            this.EnsureAuthoringRegion();

            var uri = new Uri($"{this.Host}{this.AppIdPath}");
            var cleanupResponse = await this.LuisClient.DeleteAsync(uri, cancellationToken).ConfigureAwait(false);
            cleanupResponse.EnsureSuccessStatusCode();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            this.LuisClient.Dispose();
        }

        /// <summary>
        /// Validates the utterance and entity types arguments used to train the LUIS model.
        /// </summary>
        /// <param name="utterances">Utterances.</param>
        /// <param name="entityTypes">Entity types.</param>
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

        /// <summary>
        /// Converts a prediction request response from Luis into a <see cref="LabeledUtterance"/>.
        /// </summary>
        /// <returns>A <see cref="LabeledUtterance"/>.</returns>
        /// <param name="json">Prediction request response.</param>
        /// <param name="entityTypes">Entity types included in the model.</param>
        private static LabeledUtterance PredictionToLabeledUtterance(string json, IEnumerable<EntityType> entityTypes)
        {
            if (json == null)
            {
                return new LabeledUtterance(null, null, null);
            }

            var renamedEntityTypes = entityTypes
                .OfType<BuiltinEntityType>()
                .ToDictionary(entityType => $"builtin.{entityType.BuiltinId}", entityType => entityType.Name);

            var jsonObject = JObject.Parse(json);
            var text = jsonObject.Value<string>("query");
            var intent = jsonObject.SelectToken(".topScoringIntent.intent").Value<string>();

            var array = (JArray)jsonObject["entities"];
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

        /// <summary>
        /// Retrieves entity value from "resolution" field of LUIS entity.
        /// </summary>
        /// <param name="entityJson">LUIS entity.</param>
        /// <returns>Entity value if it exists in resolution field or <code>null</code>.</returns>
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

        /// <summary>
        /// Asynchronously maps items in a collection.
        /// </summary>
        /// <typeparam name="T">Type of items.</typeparam>
        /// <typeparam name="TResult">Type of mapped item.</typeparam>
        /// <param name="items">Collection of items.</param>
        /// <param name="selector">Asynchronous mapping function.</param>
        /// <returns>Task to await the mapped results.</returns>
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

        /// <summary>
        /// Ensures the authoring region is set.
        /// </summary>
        /// <param name="caller">Caller.</param>
        private void EnsureAuthoringRegion([CallerMemberName] string caller = null)
        {
            if (this.AuthoringRegion == null)
            {
                throw new InvalidOperationException(
                    $"Must specify '{nameof(this.AuthoringRegion)}' when using '{caller}'.");
            }
        }

        /// <summary>
        /// Ensures the endpoint region is set.
        /// </summary>
        /// <param name="caller">Caller.</param>
        private void EnsureEndpointRegion([CallerMemberName] string caller = null)
        {
            if (this.EndpointRegion == null)
            {
                throw new InvalidOperationException(
                    $"Must specify '{nameof(this.EndpointRegion)}' when using '{caller}'.");
            }
        }

        /// <summary>
        /// Creates a new app for LUIS.
        /// </summary>
        /// <returns>Task to await the creation of the LUIS app.</returns>
        /// <param name="cancellationToken">Cancellation token.</param>
        private async Task<string> CreateAppAsync(CancellationToken cancellationToken)
        {
            var requestJson = new JObject
            {
                { "name", this.AppName },
                { "culture", "en-us" },
            };

            var uri = new Uri($"{this.Host}{BasePath}");
            var requestBody = requestJson.ToString(Formatting.None);
            var httpResponse = await this.LuisClient.PostAsync(uri, requestBody, cancellationToken).ConfigureAwait(false);
            httpResponse.EnsureSuccessStatusCode();
            var jsonString = await httpResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
            var json = JToken.Parse(jsonString);
            return json.ToString();
        }

        /// <summary>
        /// Creates the import JSON for the LUIS app version.
        /// </summary>
        /// <returns>Import JSON.</returns>
        /// <param name="utterances">Utterances.</param>
        /// <param name="entityTypes">Entity types.</param>
        private JObject CreateImportJson(IEnumerable<LabeledUtterance> utterances, IEnumerable<EntityType> entityTypes)
        {
            // Get boilerplate JObject
            var model = this.GetModelStarter();

            // Add intents to model
            var intents = utterances
                .Select(utterance => utterance.Intent)
                .Append("None")
                .Distinct()
                .Select(intent => new JObject { { "name", intent } });
            var intentsArray = (JArray)model["intents"];
            intentsArray.AddRange(intents);

            // Add utterances to model
            var luisUtterances = utterances
                .Select(item => JObject.FromObject(LuisLabeledUtterance.FromLabeledUtterance(item, entityTypes)));
            var utteranceArray = (JArray)model["utterances"];
            utteranceArray.AddRange(luisUtterances);

            // Add entities to model
            var entitiesArray = (JArray)model["entities"];
            var prebuiltEntitiesArray = (JArray)model["prebuiltEntities"];
            var closedListsArray = (JArray)model["closedLists"];
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

            return model;
        }

        /// <summary>
        /// Create skeleton JSON for a LUIS model.
        /// </summary>
        /// <returns>JSON object with top-level properties for a LUIS model.</returns>
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

        /// <summary>
        /// Import a LUIS model as a new version.
        /// </summary>
        /// <returns>Task to await the import operation.</returns>
        /// <param name="model">LUIS model as a json object.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        private async Task ImportVersionAsync(JObject model, CancellationToken cancellationToken)
        {
            var uri = new Uri($"{this.Host}{this.AppIdPath}versions/import?versionId={this.AppVersion}");
            var requestBody = model.ToString(Formatting.None);
            var response = await this.LuisClient.PostAsync(uri, requestBody, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
        }

        /// <summary>
        /// Polls the training status for the LUIS app.
        /// </summary>
        /// <returns>Task to await the completion of the training operation.</returns>
        /// <param name="uri">URI to poll.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        private async Task PollTrainingStatusAsync(Uri uri, CancellationToken cancellationToken)
        {
            JArray trainStatusJson;
            while (true)
            {
                var trainStatusResponse = await this.LuisClient.GetAsync(uri, cancellationToken).ConfigureAwait(false);
                var trainStatusContent = await trainStatusResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
                trainStatusJson = JArray.Parse(trainStatusContent);
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

        /// <summary>
        /// Creates a new app for LUIS.
        /// </summary>
        /// <returns>Task to await the publishing of the LUIS app version.</returns>
        /// <param name="cancellationToken">Cancellation token.</param>
        private async Task PublishAppAsync(CancellationToken cancellationToken)
        {
            var requestJson = new JObject
            {
                { "versionId", this.AppVersion },
                { "isStaging", this.IsStaging },
                { "region", this.EndpointRegion },
            };

            var uri = new Uri($"{this.Host}{this.AppIdPath}publish");
            var requestBody = requestJson.ToString(Formatting.None);
            var httpResponse = await this.LuisClient.PostAsync(uri, requestBody, cancellationToken).ConfigureAwait(false);
            httpResponse.EnsureSuccessStatusCode();
        }
    }
}
