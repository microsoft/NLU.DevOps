// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace LanguageUnderstanding.Lex
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using Amazon;
    using Amazon.Lex.Model;
    using Amazon.LexModelBuildingService;
    using Amazon.LexModelBuildingService.Model;
    using Amazon.Runtime;
    using Models;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Language understanding service for Amazon Lex.
    /// </summary>
    public class LexLanguageUnderstandingService : ILanguageUnderstandingService
    {
        private const int DegreeOfParallelism = 3;
        private const int RetryCount = 5;

        private static readonly TimeSpan GetImportDelay = TimeSpan.FromSeconds(2);
        private static readonly TimeSpan GetBotDelay = TimeSpan.FromSeconds(2);
        private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(10);

        /// <summary>
        /// Initializes a new instance of the <see cref="LexLanguageUnderstandingService"/> class.
        /// </summary>
        /// <param name="botName">Bot name.</param>
        /// <param name="botAlias">Bot alias.</param>
        /// <param name="templatesDirectory">Templates directory.</param>
        /// <param name="credentials">Credentials.</param>
        /// <param name="regionEndpoint">Region endpoint.</param>
        public LexLanguageUnderstandingService(
            string botName,
            string botAlias,
            string templatesDirectory,
            AWSCredentials credentials,
            RegionEndpoint regionEndpoint)
            : this(botName, botAlias, templatesDirectory, new DefaultLexService(credentials, regionEndpoint))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LexLanguageUnderstandingService"/> class.
        /// </summary>
        /// <param name="botName">Bot name.</param>
        /// <param name="botAlias">Bot alias.</param>
        /// <param name="templatesDirectory">Templates directory.</param>
        /// <param name="lexClient">Lex client.</param>
        public LexLanguageUnderstandingService(string botName, string botAlias, string templatesDirectory, ILexClient lexClient)
        {
            this.BotName = botName ?? throw new ArgumentNullException(nameof(botName));
            this.BotAlias = botAlias ?? throw new ArgumentNullException(nameof(botAlias));
            this.TemplatesDirectory = templatesDirectory ?? throw new ArgumentNullException(nameof(templatesDirectory));
            this.LexClient = lexClient ?? throw new ArgumentNullException(nameof(lexClient));
        }

        /// <summary>
        /// Gets the name of the bot.
        /// </summary>
        public string BotName { get; }

        /// <summary>
        /// Gets the bot alias.
        /// </summary>
        public string BotAlias { get; }

        private string TemplatesDirectory { get; }

        private ILexClient LexClient { get; }

        /// <inheritdoc />
        public async Task TrainAsync(IEnumerable<LabeledUtterance> utterances, IEnumerable<EntityType> entityTypes, CancellationToken cancellationToken)
        {
            // Validate arguments
            ValidateArguments(utterances, entityTypes);

            // Create the bot
            await this.CreateBotAsync(cancellationToken);

            // Generate the bot configuration
            var importJson = this.CreateImportJson(utterances, entityTypes);

            // Import the bot configuration
            await this.ImportBotAsync(importJson, cancellationToken);

            // Build the bot
            await this.BuildBotAsync(cancellationToken);

            // Publish the bot
            await this.PublishBotAsync(cancellationToken);
        }

        /// <inheritdoc />
        public Task<IEnumerable<LabeledUtterance>> TestAsync(IEnumerable<string> utterances, CancellationToken cancellationToken)
        {
            if (utterances == null)
            {
                throw new ArgumentNullException(nameof(utterances));
            }

            async Task<LabeledUtterance> selector(string utterance, int index)
            {
                if (utterance == null)
                {
                    throw new ArgumentException("Utterance must not be null.", nameof(utterance));
                }

                var postTextRequest = new PostTextRequest
                {
                    BotAlias = this.BotAlias,
                    BotName = this.BotName,
                    UserId = $"User{index}",
                    InputText = utterance,
                };

                var postTextResponse = await this.LexClient.PostTextAsync(postTextRequest, cancellationToken);
                var entities = postTextResponse.Slots?
                    .Where(slot => slot.Value != null)
                    .Select(slot => new Entity(slot.Key, slot.Value, null, 0))
                    .ToList();

                return new LabeledUtterance(
                    utterance,
                    postTextResponse.IntentName,
                    entities);
            }

            return SelectAsync(utterances, selector);
        }

        /// <inheritdoc />
        public Task<IEnumerable<LabeledUtterance>> TestSpeechAsync(IEnumerable<string> speechFiles, CancellationToken cancellationToken)
        {
            if (speechFiles == null)
            {
                throw new ArgumentNullException(nameof(speechFiles));
            }

            async Task<LabeledUtterance> selector(string speechFile, int index)
            {
                if (speechFile == null)
                {
                    throw new ArgumentException("Speech files must not be null.", nameof(speechFiles));
                }

                using (var stream = File.OpenRead(speechFile))
                {
                    var postContentRequest = new PostContentRequest
                    {
                        BotAlias = this.BotAlias,
                        BotName = this.BotName,
                        UserId = $"User{index}",
                        Accept = "text/plain; charset=utf-8",
                        ContentType = "audio/l16; rate=16000; channels=1",
                        InputStream = stream,
                    };

                    var postContentResponse = await this.LexClient.PostContentAsync(postContentRequest, cancellationToken);
                    var slots = postContentResponse.Slots != null
                        ? JsonConvert.DeserializeObject<Dictionary<string, string>>(postContentResponse.Slots)
                            .Select(slot => new Entity(slot.Key, slot.Value, null, 0))
                            .ToList()
                        : null;

                    return new LabeledUtterance(
                        postContentResponse.InputTranscript,
                        postContentResponse.IntentName,
                        slots);
                }
            }

            return SelectAsync(speechFiles, selector);
        }

        /// <inheritdoc />
        public async Task CleanupAsync(CancellationToken cancellationToken)
        {
            await RetryAsync<Amazon.LexModelBuildingService.Model.ConflictException>(this.DeleteBotAliasAsync, cancellationToken);
            await RetryAsync<Amazon.LexModelBuildingService.Model.ConflictException>(this.DeleteBotAsync, cancellationToken);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            this.LexClient.Dispose();
        }

        private static void ValidateArguments(IEnumerable<LabeledUtterance> utterances, IEnumerable<EntityType> entityTypes)
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

        private static string CreateSampleUtterance(LabeledUtterance utterance)
        {
            var text = utterance.Text;
            if (utterance.Entities != null)
            {
                foreach (var entity in utterance.Entities)
                {
                    // Match against original text, avoid matching partial contractions
                    var match = new Regex($"\\b{entity.MatchText}\\b(?!'\\w)")
                        .Match(utterance.Text);

                    // Iterate to the correct match
                    for (var i = 0; i < entity.MatchIndex; ++i)
                    {
                        match = match.NextMatch();
                    }

                    if (!match.Success)
                    {
                        throw new InvalidOperationException("Unable to find matching entity.");
                    }

                    // Replace the matching token with the slot indicator
                    text = new Regex(entity.MatchText)
                        .Replace(text, $"{{{entity.EntityType}}}", 1, match.Index);
                }
            }

            return text;
        }

        private static async Task WriteJsonZipAsync(Stream stream, JToken importJson, CancellationToken cancellationToken)
        {
            using (var zipArchive = new ZipArchive(stream, ZipArchiveMode.Create, true))
            {
                var zipEntry = zipArchive.CreateEntry("import.json");
                using (var entryStream = zipEntry.Open())
                {
                    var importJsonString = importJson.ToString();
                    var importJsonBytes = Encoding.UTF8.GetBytes(importJsonString);
                    await entryStream.WriteAsync(importJsonBytes, 0, importJsonBytes.Length, cancellationToken);
                }
            }

            // Reset stream to initial position
            stream.Position = 0;
        }

        private static async Task<IEnumerable<TResult>> SelectAsync<T, TResult>(IEnumerable<T> items, Func<T, int, Task<TResult>> selector)
        {
            var indexedItems = items.Select((item, i) => new { Item = item, Index = i });
            var results = new TResult[items.Count()];
            var tasks = new List<Task<Tuple<int, TResult>>>(DegreeOfParallelism);

            async Task<Tuple<int, TResult>> selectWithIndexAsync(T item, int i)
            {
                var result = await selector(item, i);
                return Tuple.Create(i, result);
            }

            foreach (var indexedItem in indexedItems)
            {
                if (tasks.Count == DegreeOfParallelism)
                {
                    var task = await Task.WhenAny(tasks);
                    tasks.Remove(task);
                    var result = await task;
                    results[/* (int) */ result.Item1] = /* (TResult) */ result.Item2;
                }

                tasks.Add(selectWithIndexAsync(indexedItem.Item, indexedItem.Index));
            }

            await Task.WhenAll(tasks);
            foreach (var task in tasks)
            {
                var result = await task;
                results[/* (int) */ result.Item1] = /* (TResult) */ result.Item2;
            }

            return results;
        }

        private static async Task RetryAsync<TException>(Func<CancellationToken, Task> actionAsync, CancellationToken cancellationToken)
            where TException : Exception
        {
            var count = 0;
            while (count++ < RetryCount)
            {
                try
                {
                    await actionAsync(cancellationToken);
                    return;
                }
                catch (TException)
                when (count < RetryCount)
                {
                    await Task.Delay(RetryDelay, cancellationToken);
                }
            }
        }

        private Task CreateBotAsync(CancellationToken cancellationToken)
        {
            // Create a new bot with the given name
            var botJson = File.ReadAllText(Path.Combine(this.TemplatesDirectory, "bot.json"));
            var putBotRequest = JsonConvert.DeserializeObject<PutBotRequest>(botJson);
            putBotRequest.Name = this.BotName;
            putBotRequest.CreateVersion = true;
            return this.LexClient.PutBotAsync(putBotRequest, cancellationToken);
        }

        private JToken CreateImportJson(IEnumerable<LabeledUtterance> utterances, IEnumerable<EntityType> entityTypes)
        {
            // Add name to imports JSON template
            var importJsonTemplate = File.ReadAllText(Path.Combine(this.TemplatesDirectory, "import.json"));
            var importJson = JObject.Parse(importJsonTemplate);
            importJson.SelectToken(".resource.name").Replace(this.BotName);

            // Add intents to imports JSON template
            var intents = utterances
                .GroupBy(utterance => utterance.Intent)
                .Select(group => this.CreateIntent(group.Key, group, entityTypes));
            Debug.Assert(importJson.SelectToken(".resource.intents") is JArray, "Import template includes intents JSON array.");
            var intentsArray = (JArray)importJson.SelectToken(".resource.intents");
            intentsArray.AddRange(intents);

            // Add slot types to imports JSON template
            var slotTypes = entityTypes
                .OfType<ListEntityType>()
                .Select(entityType => this.CreateSlotType(entityType));
            Debug.Assert(importJson.SelectToken(".resource.slotTypes") is JArray, "Import template includes slotTypes JSON array.");
            var slotTypesArray = (JArray)importJson.SelectToken(".resource.slotTypes");
            slotTypesArray.AddRange(slotTypes);

            return importJson;
        }

        private JToken CreateIntent(string intent, IEnumerable<LabeledUtterance> utterances, IEnumerable<EntityType> entityTypes)
        {
            // Create a new intent with the given name
            var intentJsonString = File.ReadAllText(Path.Combine(this.TemplatesDirectory, "intent.json"));
            var intentJson = JObject.Parse(intentJsonString);
            intentJson.SelectToken(".name").Replace(intent);

            // Create slots for the intent
            //
            // Currently, the algorithm only adds slots that
            // exist in the training set for the given intent.
            var slots = utterances
                .SelectMany(utterance => utterance.Entities ?? Array.Empty<Entity>())
                .Select(entity => entity.EntityType)
                .Distinct()
                .Select(slot => this.CreateSlot(slot, entityTypes));
            Debug.Assert(intentJson.SelectToken(".slots") is JArray, "Intent template includes slots JSON array.");
            var slotsArray = (JArray)intentJson.SelectToken(".slots");
            slotsArray.AddRange(slots);

            // Create Lex sample utterances
            var sampleUtterances = utterances
                .Select(utterance => CreateSampleUtterance(utterance));
            Debug.Assert(intentJson.SelectToken(".sampleUtterances") is JArray, "Intent template includes sampleUtterances JSON array.");
            var sampleUtterancesArray = (JArray)intentJson.SelectToken(".sampleUtterances");
            sampleUtterancesArray.AddRange(sampleUtterances);

            return intentJson;
        }

        private JToken CreateSlot(string slot, IEnumerable<EntityType> entityTypes)
        {
            // This will throw if a matching entity type is not provided
            var entityType = entityTypes.First(e => e.Name == slot);

            // Create a new intent with the given name
            var slotJsonString = File.ReadAllText(Path.Combine(this.TemplatesDirectory, "slot.json"));
            var slotJson = JObject.Parse(slotJsonString);
            slotJson.SelectToken(".name").Replace(slot);

            var slotTypeToken = slotJson.SelectToken(".slotType");
            if (entityType is BuiltinEntityType builtinEntityType)
            {
                slotTypeToken.Replace(builtinEntityType.BuiltinId);
            }
            else
            {
                slotTypeToken.Replace(slot);
            }

            return slotJson;
        }

        private JToken CreateSlotType(ListEntityType entityType)
        {
            // Create a new intent with the given name
            var slotTypeJsonString = File.ReadAllText(Path.Combine(this.TemplatesDirectory, "slotType.json"));
            var slotTypeJson = JObject.Parse(slotTypeJsonString);
            slotTypeJson.SelectToken(".name").Replace(entityType.Name);

            // If any values have synonyms, use TOP_RESOLUTION, otherwise use ORIGINAL_VALUE
            var valueSelectionStrategy = entityType.Values.Any(synonymSet => synonymSet.Synonyms?.Count > 0)
                ? SlotValueSelectionStrategy.TOP_RESOLUTION
                : SlotValueSelectionStrategy.ORIGINAL_VALUE;
            slotTypeJson.SelectToken(".valueSelectionStrategy").Replace(valueSelectionStrategy.Value);

            // Add enumeration values
            var enumerationValues = entityType.Values
                .Select(synonymSet => new JObject
                {
                    { "value", synonymSet.CanonicalForm },
                    { "synonyms", JArray.FromObject(synonymSet.Synonyms ?? Array.Empty<string>()) },
                });
            var slotTypesArray = (JArray)slotTypeJson.SelectToken(".enumerationValues");
            slotTypesArray.AddRange(enumerationValues);

            return slotTypeJson;
        }

        private async Task ImportBotAsync(JToken importJson, CancellationToken cancellationToken)
        {
            using (var stream = new MemoryStream())
            {
                // Generate zip archive with imports JSON
                await WriteJsonZipAsync(stream, importJson, cancellationToken);

                // Call StartImport action on Amazon Lex
                var startImportRequest = new StartImportRequest
                {
                    MergeStrategy = MergeStrategy.OVERWRITE_LATEST,
                    Payload = stream,
                    ResourceType = ResourceType.BOT,
                };

                var startImportResponse = await this.LexClient.StartImportAsync(startImportRequest, cancellationToken);

                // If the import is not complete, poll until import is complete
                if (startImportResponse.ImportStatus != ImportStatus.COMPLETE)
                {
                    await this.PollBotImportStatusAsync(startImportResponse.ImportId, cancellationToken);
                }
            }
        }

        private async Task PollBotImportStatusAsync(string importId, CancellationToken cancellationToken)
        {
            var getImportRequest = new GetImportRequest
            {
                ImportId = importId,
            };

            while (true)
            {
                // Check the status of the import operation
                var getImportResponse = await this.LexClient.GetImportAsync(getImportRequest, cancellationToken);

                // If complete, break from the loop
                if (getImportResponse.ImportStatus == ImportStatus.COMPLETE)
                {
                    break;
                }

                // If failed, throw an exception
                if (getImportResponse.ImportStatus == ImportStatus.FAILED)
                {
                    var exceptionMessage = string.Join(Environment.NewLine, getImportResponse.FailureReason);
                    throw new InvalidOperationException(exceptionMessage);
                }

                // If in progress, delay
                await Task.Delay(GetImportDelay, cancellationToken);
            }
        }

        private async Task BuildBotAsync(CancellationToken cancellationToken)
        {
            // Get the latest bot configuration
            var getBotRequest = new GetBotRequest
            {
                Name = this.BotName,
                VersionOrAlias = "$LATEST",
            };

            var getBotResponse = await this.LexClient.GetBotAsync(getBotRequest, cancellationToken);

            // Call PutBot with the latest GetBot response
            var putBotBuildRequest = JObject.FromObject(getBotResponse).ToObject<PutBotRequest>();
            putBotBuildRequest.CreateVersion = true;
            putBotBuildRequest.ProcessBehavior = ProcessBehavior.BUILD;

            // Workaround for error received from Lex client
            putBotBuildRequest.AbortStatement.Messages.First().GroupNumber = 1;
            putBotBuildRequest.ClarificationPrompt.Messages.First().GroupNumber = 1;

            await this.LexClient.PutBotAsync(putBotBuildRequest, cancellationToken);

            // Poll for until bot is ready
            await this.PollBotReadyStatusAsync(cancellationToken);
        }

        private async Task PollBotReadyStatusAsync(CancellationToken cancellationToken)
        {
            var getBotRequest = new GetBotRequest
            {
                Name = this.BotName,
                VersionOrAlias = "$LATEST",
            };

            // Wait for Ready status
            while (true)
            {
                var getBotResponse = await this.LexClient.GetBotAsync(getBotRequest, cancellationToken);
                if (getBotResponse.Status == Status.READY)
                {
                    break;
                }

                if (getBotResponse.Status == Status.FAILED)
                {
                    var exceptionMessage = string.Join(Environment.NewLine, getBotResponse.FailureReason);
                    throw new InvalidOperationException(exceptionMessage);
                }

                if (getBotResponse.Status != Status.BUILDING)
                {
                    throw new InvalidOperationException(
                        $"Expected bot to have '{Status.BUILDING}', instead found '{getBotResponse.Status}'.");
                }

                await Task.Delay(GetBotDelay, cancellationToken);
            }
        }

        private async Task DeleteBotAliasAsync(CancellationToken cancellationToken)
        {
            try
            {
                var deleteBotAliasRequest = new DeleteBotAliasRequest
                {
                    BotName = this.BotName,
                    Name = this.BotAlias,
                };

                await this.LexClient.DeleteBotAliasAsync(deleteBotAliasRequest, cancellationToken);
            }
            catch (Amazon.LexModelBuildingService.Model.NotFoundException)
            {
                // Likely that no bot alias was published
                // TODO: log that this exception occurred
            }
        }

        private async Task DeleteBotAsync(CancellationToken cancellationToken)
        {
            try
            {
                var deleteBotRequest = new DeleteBotRequest
                {
                    Name = this.BotName,
                };

                await this.LexClient.DeleteBotAsync(deleteBotRequest, cancellationToken);
            }
            catch (Amazon.LexModelBuildingService.Model.NotFoundException)
            {
                // Likely that bot was not created
                // TODO: log that this exception occurred
            }
        }

        private async Task PublishBotAsync(CancellationToken cancellationToken)
        {
            // Creates an alias that can be used for testing
            var putBotAliasRequest = new PutBotAliasRequest
            {
                BotName = this.BotName,
                BotVersion = "$LATEST",
                Name = this.BotAlias,
            };

            await this.LexClient.PutBotAliasAsync(putBotAliasRequest, cancellationToken);
        }
    }
}
