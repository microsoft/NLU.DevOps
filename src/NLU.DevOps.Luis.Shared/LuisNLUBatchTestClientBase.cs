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
    using Models;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Base class for using the LUIS batch testing API.
    /// </summary>
    public abstract class LuisNLUBatchTestClientBase : INLUTestClient, INLUBatchTestClient
    {
        private static readonly TimeSpan OperationStatusDelay = TimeSpan.FromSeconds(2);

        /// <summary>
        /// Initializes a new instance of the <see cref="LuisNLUBatchTestClientBase"/> class.
        /// </summary>
        /// <param name="luisConfiguration">LUIS configuration.</param>
        /// <param name="luisBatchTestClient">LUIS batch test client.</param>
        public LuisNLUBatchTestClientBase(ILuisConfiguration luisConfiguration, ILuisBatchTestClient luisBatchTestClient)
        {
            this.IsBatchEnabled = luisConfiguration?.IsBatchEnabled ?? throw new ArgumentNullException(nameof(luisConfiguration));
            this.LuisBatchTestClient = luisBatchTestClient ?? throw new ArgumentNullException(nameof(luisBatchTestClient));
        }

        /// <inheritdoc />
        public bool IsBatchEnabled { get; }

        private ILuisBatchTestClient LuisBatchTestClient { get; }

        /// <inheritdoc />
        public abstract Task<ILabeledUtterance> TestAsync(JToken query, CancellationToken cancellationToken);

        /// <inheritdoc />
        public abstract Task<ILabeledUtterance> TestSpeechAsync(string speechFile, JToken query, CancellationToken cancellationToken);

        /// <inheritdoc />
        public async Task<IEnumerable<ILabeledUtterance>> TestAsync(IEnumerable<JToken> queries, CancellationToken cancellationToken)
        {
            if (queries == null)
            {
                throw new ArgumentNullException(nameof(queries));
            }

            // Chunk query set into batch sizes suitable for the LUIS batch evaluation endpoint
            var queryBatches = queries
                .Select(ToLuisBatchInput)
                .Batch(Luis.LuisBatchTestClient.BatchSize);

            // Evaluate each batch and add to results set
            // We could consider running the batches in parallel
            var batchResults = new List<ILabeledUtterance>();
            foreach (var queryBatch in queryBatches)
            {
                batchResults.AddRange(await this.EvaluateAsync(queryBatch, cancellationToken).ConfigureAwait(false));
            }

            return batchResults;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes the LUIS batch test client.
        /// </summary>
        /// <param name="disposing">
        /// <code>true</code> if disposing, otherwise <code>false</code>.
        /// </param>
        protected abstract void Dispose(bool disposing);

        /// <summary>
        /// Converts the input utterance JSON to a format suitable for LUIS batch evaluation.
        /// </summary>
        /// <param name="inputJson">Input utterance JSON.</param>
        /// <returns>The modified JSON object.</returns>
        private static JToken ToLuisBatchInput(JToken inputJson)
        {
            // Function to convert input entity JSON to LUIS batch format in place
            void toLuisBatchEntity(string text, JToken json)
            {
                // Checks if the JSON is formatted in generic utterance format
                if (json is JObject jsonObject && jsonObject.ContainsKey("matchText") && !jsonObject.ContainsKey("startPos"))
                {
                    // Extract the generic entity properties
                    var entityType = jsonObject["entityType"];
                    var matchText = jsonObject.Value<string>("matchText");
                    var matchIndex = jsonObject.Value<int>("matchIndex");

                    // Remove the generic entity properties
                    jsonObject.Remove("entityType");
                    jsonObject.Remove("matchText");
                    jsonObject.Remove("matchIndex");

                    // Find the start index of the entity in the utterance 
                    var count = 0;
                    var startPos = 0;
                    while (count++ <= matchIndex)
                    {
                        startPos = text.IndexOf(matchText, startPos + 1, StringComparison.Ordinal);
                    }

                    // Ensure that the start position was found
                    if (startPos == -1)
                    {
                        throw new InvalidOperationException($"Could not find '{matchText}' in '{text}'.");
                    }

                    // Add the relevent LUIS batch entity properties
                    jsonObject.Add("startPos", startPos);
                    jsonObject.Add("endPos", startPos + matchText.Length - 1);
                    jsonObject["entity"] = jsonObject["entity"] ?? entityType;
                }
            }

            // Clone the input JSON so we can modify it in place
            var outputJson = inputJson.DeepClone();

            // Ensure each entity is in LUIS batch format
            if (outputJson is JObject jsonObject)
            {
                // Ensure the 'entities' property is not null or undefined
                // TODO: replace with existence check if the bug is resolved in LUIS batch evaluation API
                jsonObject["entities"] = jsonObject["entities"] ?? new JArray();

                var text = jsonObject.Value<string>("text");
                foreach (var entity in jsonObject["entities"])
                {
                    // Modifies in place as we've already cloned the input JSON
                    toLuisBatchEntity(text, entity);
                }
            }

            return outputJson;
        }

        /// <summary>
        /// Convert LUIS batch output to LUIS entity format.
        /// </summary>
        /// <param name="entity">LUIS batch output entity JSON.</param>
        /// <returns>JSON object, formatted in place.</returns>
        /// <remarks>
        /// Since the output JSON from the API call is self-contained in this class,
        /// it is safe to modify the JSON object in place.
        /// </remarks>
        private static JToken ToLuisEntity(JToken entity)
        {
            entity["entity"] = entity["entityName"];
            entity["startPos"] = entity["startCharIndex"];
            entity["endPos"] = entity["endCharIndex"];

            var entityObject = (JObject)entity;
            entityObject.Remove("entityName");
            entityObject.Remove("startCharIndex");
            entityObject.Remove("endCharIndex");

            return entity;
        }

        private static IEnumerable<ILabeledUtterance> BatchResultsToLabeledUtterances(IEnumerable<JToken> queries, JToken batchResults)
        {
            // Get the utterance output from the batch results
            var utterancesStatistics = batchResults["utterancesStats"];

            // Get the set of entity types that have been evaluated by the service
            var entityTypes = new HashSet<string>(GetEntityTypes(batchResults["entityModelsStats"]));

            // Pair each expected utterance with actual utterance result
            var utterancePairs = queries.Zip(utterancesStatistics, (query, utterance) => Tuple.Create(query, utterance));
            foreach (var utterancePair in utterancePairs)
            {
                var expectedUtterance = utterancePair.Item1;
                var actualUtterance = utterancePair.Item2;

                // Return entities from the expected utterance that are tested
                // by the service (as determined from the 'entityModelStats'
                // model names, remove any false negative entities and add any
                // false positive entities.
                var entities = expectedUtterance["entities"]
                    .Where(entity => entityTypes.Contains(entity.Value<string>("entity")))
                    .Except(actualUtterance["falseNegativeEntities"], EntityEqualityComparer.Instance)
                    .Concat(actualUtterance["falsePositiveEntities"].Select(ToLuisEntity));

                // Create the utterance JSON
                var utteranceJson = new JObject
                {
                    { "text", actualUtterance.Value<string>("text") },
                    { "intent", actualUtterance.Value<string>("predictedIntentName") },
                    { "entities", new JArray(entities) },
                };

                // Convert the utterance JSON to the utterance DTO
                var serializer = JsonSerializer.CreateDefault();
                serializer.Converters.Add(new LabeledUtteranceConverter());
                yield return utteranceJson.ToObject<LabeledUtterance>(serializer);
            }
        }

        private static IEnumerable<string> GetEntityTypes(JToken entityStatistics)
        {
            foreach (var entityStatistic in entityStatistics)
            {
                var modelName = entityStatistic.Value<string>("modelName");
                var nestedIndex = modelName.LastIndexOf('\\');
                yield return nestedIndex >= 0 ? modelName.Substring(nestedIndex + 1) : modelName;
            }
        }

        private async Task<IEnumerable<ILabeledUtterance>> EvaluateAsync(IEnumerable<JToken> queries, CancellationToken cancellationToken)
        {
            // Create the batch input JSON structure
            var batchInput = new JObject { { "LabeledTestSetUtterances", new JArray(queries) } };

            // Start the evaluation and await the operation ID
            var operationInfo = await this.LuisBatchTestClient.CreateEvaluationsOperationAsync(batchInput, cancellationToken).ConfigureAwait(false);

            // Poll until the evaluation operation reaches a terminal state
            while (true)
            {
                // Check the status of the evaluation operation
                var statusInfo = await this.LuisBatchTestClient.GetEvaluationsStatusAsync(operationInfo.Value, cancellationToken).ConfigureAwait(false);

                // Break if the operation was succcessful, otherwise throw an exception with the error details
                if (statusInfo.Value.Status == "succeeded")
                {
                    break;
                }
                else if (statusInfo.Value.Status == "failed")
                {
                    throw new InvalidOperationException(statusInfo.Value.ErrorDetails);
                }

                // Yield for the HTTP 'Retry-After' delay from the status operation response, or use a default delay
                var retryAfter = Retry.GetRetryAfterDelay(statusInfo.RetryAfter, OperationStatusDelay);
                await Task.Delay(retryAfter, cancellationToken).ConfigureAwait(false);
            }

            // Get the batch results and convert them to the utterance DTO.
            var batchResults = await this.LuisBatchTestClient.GetEvaluationsResultAsync(operationInfo.Value, cancellationToken).ConfigureAwait(false);
            return BatchResultsToLabeledUtterances(queries, batchResults);
        }

        /// <summary>
        /// Equality comparer for LUIS batch input and output entities.
        /// </summary>
        private class EntityEqualityComparer : IEqualityComparer<JToken>
        {
            private const uint Prime = 0xa5555529; // See CompilationPass.cpp in C# compiler codebase.

            private EntityEqualityComparer()
            {
            }

            public static EntityEqualityComparer Instance { get; } = new EntityEqualityComparer();

            public bool Equals(JToken x, JToken y)
            {
                // We are only comparing input entities to the set of false negative output entities.
                // Thus we know that one of the arguments is an input entity and the other is output.
                var input = x["entity"] != null ? x : y;
                var output = x["entity"] != null ? y : x;
                return input["entity"] == output["entityName"]
                    && input["startPos"] == output["startCharIndex"]
                    && input["endPos"] == output["endCharIndex"];
            }

            public int GetHashCode(JToken obj)
            {
                // Compute hash for LUIS input entity
                if (obj["entity"] != null)
                {
                    var inputHash = obj.Value<string>("entity").GetHashCode(StringComparison.Ordinal);
                    inputHash = (int)(inputHash * Prime) + obj.Value<int>("startPos").GetHashCode();
                    return (int)(inputHash * Prime) + obj.Value<int>("endPos").GetHashCode();
                }

                // Compute hash for LUIS output entity
                var outputHash = obj.Value<string>("entityName").GetHashCode(StringComparison.Ordinal);
                outputHash = (int)(outputHash * Prime) + obj.Value<int>("startCharIndex").GetHashCode();
                return (int)(outputHash * Prime) + obj.Value<int>("endCharIndex").GetHashCode();
            }
        }
    }
}
