// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace NLU.DevOps.Lex
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
    using Logging;
    using Microsoft.Extensions.Logging;
    using Models;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// NLU service for Amazon Lex.
    /// </summary>
    public sealed class LexNLUService : INLUService
    {
        private const int GetBotDelaySeconds = 2;
        private const int GetImportDelaySeconds = 2;

        /// <summary>
        /// Initializes a new instance of the <see cref="LexNLUService"/> class.
        /// </summary>
        /// <param name="botName">Bot name.</param>
        /// <param name="botAlias">Bot alias.</param>
        /// <param name="credentials">Credentials.</param>
        /// <param name="regionEndpoint">Region endpoint.</param>
        public LexNLUService(
            string botName,
            string botAlias,
            AWSCredentials credentials,
            RegionEndpoint regionEndpoint)
            : this(botName, botAlias, new LexClient(credentials, regionEndpoint))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LexNLUService"/> class.
        /// </summary>
        /// <param name="botName">Bot name.</param>
        /// <param name="botAlias">Bot alias.</param>
        /// <param name="lexClient">Lex client.</param>
        public LexNLUService(string botName, string botAlias, ILexClient lexClient)
        {
            this.LexBotName = botName ?? throw new ArgumentNullException(nameof(botName));
            this.LexBotAlias = botAlias ?? throw new ArgumentNullException(nameof(botAlias));
            this.LexClient = lexClient ?? throw new ArgumentNullException(nameof(lexClient));
        }

        /// <summary>
        /// Gets the name of the bot.
        /// </summary>
        public string LexBotName { get; }

        /// <summary>
        /// Gets the bot alias.
        /// </summary>
        public string LexBotAlias { get; }

        private static ILogger Logger => LazyLogger.Value;

        private static Lazy<ILogger> LazyLogger { get; } = new Lazy<ILogger>(() => ApplicationLogger.LoggerFactory.CreateLogger<LexNLUService>());

        private ILexClient LexClient { get; }

        /// <inheritdoc />
        public async Task TrainAsync(IEnumerable<LabeledUtterance> utterances, IEnumerable<EntityType> entityTypes, CancellationToken cancellationToken)
        {
            // Validate arguments
            ValidateArguments(utterances, entityTypes);

            // Check if bot exists
            var botExists = await this.BotExistsAsync(cancellationToken).ConfigureAwait(false);

            // Create the bot if does not exist
            if (!botExists)
            {
                await this.CreateBotAsync(cancellationToken).ConfigureAwait(false);
            }

            // Generate the bot configuration
            var importJson = this.CreateImportJson(utterances, entityTypes);

            // Import the bot configuration
            await this.ImportBotAsync(importJson, cancellationToken).ConfigureAwait(false);

            // Build the bot
            await this.BuildBotAsync(cancellationToken).ConfigureAwait(false);

            // Publish the bot if not published
            var isPublished = await this.IsPublishedAsync(cancellationToken).ConfigureAwait(false);
            if (!isPublished)
            {
                await this.PublishBotAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        /// <inheritdoc />
        public async Task<LabeledUtterance> TestAsync(string utterance, IEnumerable<EntityType> entityTypes, CancellationToken cancellationToken)
        {
            if (utterance == null)
            {
                throw new ArgumentNullException(nameof(utterance));
            }

            if (entityTypes == null)
            {
                throw new ArgumentNullException(nameof(entityTypes));
            }

            var postTextRequest = new PostTextRequest
            {
                BotAlias = this.LexBotAlias,
                BotName = this.LexBotName,
                UserId = Guid.NewGuid().ToString(),
                InputText = utterance,
            };

            var postTextResponse = await this.LexClient.PostTextAsync(postTextRequest, cancellationToken).ConfigureAwait(false);
            var entities = postTextResponse.Slots?
                .Where(slot => slot.Value != null)
                .Select(slot => new Entity(slot.Key, slot.Value, null, 0))
                .ToArray();

            return new LabeledUtterance(
                utterance,
                postTextResponse.IntentName,
                entities);
        }

        /// <inheritdoc />
        public async Task<LabeledUtterance> TestSpeechAsync(string speechFile, IEnumerable<EntityType> entityTypes, CancellationToken cancellationToken)
        {
            if (speechFile == null)
            {
                throw new ArgumentNullException(nameof(speechFile));
            }

            if (entityTypes == null)
            {
                throw new ArgumentNullException(nameof(entityTypes));
            }

            using (var stream = File.OpenRead(speechFile))
            {
                var postContentRequest = new PostContentRequest
                {
                    BotAlias = this.LexBotAlias,
                    BotName = this.LexBotName,
                    UserId = Guid.NewGuid().ToString(),
                    Accept = "text/plain; charset=utf-8",
                    ContentType = "audio/l16; rate=16000; channels=1",
                    InputStream = stream,
                };

                var postContentResponse = await this.LexClient.PostContentAsync(postContentRequest, cancellationToken).ConfigureAwait(false);
                var slots = postContentResponse.Slots != null
                    ? JsonConvert.DeserializeObject<Dictionary<string, string>>(postContentResponse.Slots)
                        .Select(slot => new Entity(slot.Key, slot.Value, null, 0))
                        .ToArray()
                    : null;

                return new LabeledUtterance(
                    postContentResponse.InputTranscript,
                    postContentResponse.IntentName,
                    slots);
            }
        }

        /// <inheritdoc />
        public async Task CleanupAsync(CancellationToken cancellationToken)
        {
            await this.DeleteBotAliasAsync(cancellationToken).ConfigureAwait(false);
            await this.DeleteBotAsync(cancellationToken).ConfigureAwait(false);
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
                    // Match in original text
                    var index = entity.StartCharIndexInText(utterance.Text);

                    // Replace the matching token with the slot indicator
                    text = new Regex(entity.MatchText)
                        .Replace(text, $"{{{entity.EntityType}}}", 1, index);
                }
            }

            return text;
        }

        private static JToken CreateIntent(string intent, IEnumerable<LabeledUtterance> utterances, IEnumerable<EntityType> entityTypes)
        {
            // Create a new intent with the given name
            var intentJson = ImportBotTemplates.IntentJson;
            intentJson.SelectToken(".name").Replace(intent);

            // Create slots for the intent
            //
            // Currently, the algorithm only adds slots that
            // exist in the training set for the given intent.
            var slots = utterances
                .SelectMany(utterance => utterance.Entities ?? Array.Empty<Entity>())
                .Select(entity => entity.EntityType)
                .Distinct()
                .Select(slot => CreateSlot(slot, entityTypes));
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

        private static JToken CreateSlot(string slot, IEnumerable<EntityType> entityTypes)
        {
            // This will throw if a matching entity type is not provided
            var entityType = entityTypes.First(e => e.Name == slot);

            // Create a new intent with the given name
            var slotJson = ImportBotTemplates.SlotJson;
            slotJson.SelectToken(".name").Replace(slot);

            if (entityType.Kind == "builtin")
            {
                slotJson.Merge(entityType.Data);
            }
            else
            {
                slotJson.SelectToken(".slotType").Replace(slot);
            }

            return slotJson;
        }

        private static JToken CreateSlotType(EntityType entityType)
        {
            // Create a new intent with the given name
            var slotTypeJson = ImportBotTemplates.SlotTypeJson;
            slotTypeJson.SelectToken(".name").Replace(entityType.Name);

            // If any values have synonyms, use TOP_RESOLUTION, otherwise use ORIGINAL_VALUE
            var valueSelectionStrategy = entityType.Data.SelectTokens(".enumerationValues[*].synonyms")
                .Any(synonyms => synonyms.Any())
                ? SlotValueSelectionStrategy.TOP_RESOLUTION
                : SlotValueSelectionStrategy.ORIGINAL_VALUE;
            slotTypeJson.SelectToken(".valueSelectionStrategy").Replace(valueSelectionStrategy.Value);

            slotTypeJson.Merge(entityType.Data);

            return slotTypeJson;
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
                    await entryStream.WriteAsync(importJsonBytes, 0, importJsonBytes.Length, cancellationToken).ConfigureAwait(false);
                }
            }

            // Reset stream to initial position
            stream.Position = 0;
        }

        private async Task<bool> BotExistsAsync(CancellationToken cancellationToken)
        {
            // Get the latest bot configuration
            var getBotsRequest = new GetBotsRequest
            {
                NameContains = this.LexBotName,
            };

            var getBotsResponse = await this.LexClient.GetBotsAsync(getBotsRequest, cancellationToken).ConfigureAwait(false);
            return getBotsResponse.Bots.Any(bot => bot.Name == this.LexBotName);
        }

        private Task CreateBotAsync(CancellationToken cancellationToken)
        {
            // Create a new bot with the given name
            var putBotRequest = new PutBotRequest
            {
                Name = this.LexBotName,
                ChildDirected = false,
                CreateVersion = true,
                Locale = Locale.EnUS,
                ProcessBehavior = ProcessBehavior.BUILD,
                VoiceId = "0",
            };

            Logger.LogTrace($"Creating bot '{this.LexBotName}'.");
            return this.LexClient.PutBotAsync(putBotRequest, cancellationToken);
        }

        private JToken CreateImportJson(IEnumerable<LabeledUtterance> utterances, IEnumerable<EntityType> entityTypes)
        {
            // Add name to imports JSON template
            var importJson = ImportBotTemplates.ImportJson;
            importJson.SelectToken(".resource.name").Replace(this.LexBotName);

            // Add intents to imports JSON template
            var intents = utterances
                .GroupBy(utterance => utterance.Intent)
                .Select(group => CreateIntent(group.Key, group, entityTypes));
            Debug.Assert(importJson.SelectToken(".resource.intents") is JArray, "Import template includes intents JSON array.");
            var intentsArray = (JArray)importJson.SelectToken(".resource.intents");
            intentsArray.AddRange(intents);

            // Add slot types to imports JSON template
            var slotTypes = entityTypes
                .Where(entityType => entityType.Kind != "builtin")
                .Select(entityType => CreateSlotType(entityType));
            Debug.Assert(importJson.SelectToken(".resource.slotTypes") is JArray, "Import template includes slotTypes JSON array.");
            var slotTypesArray = (JArray)importJson.SelectToken(".resource.slotTypes");
            slotTypesArray.AddRange(slotTypes);

            return importJson;
        }

        private async Task ImportBotAsync(JToken importJson, CancellationToken cancellationToken)
        {
            using (var stream = new MemoryStream())
            {
                // Generate zip archive with imports JSON
                await WriteJsonZipAsync(stream, importJson, cancellationToken).ConfigureAwait(false);

                // Call StartImport action on Amazon Lex
                var startImportRequest = new StartImportRequest
                {
                    MergeStrategy = MergeStrategy.OVERWRITE_LATEST,
                    Payload = stream,
                    ResourceType = ResourceType.BOT,
                };

                Logger.LogTrace($"Importing bot '{this.LexBotName}'.");

                var startImportResponse = await this.LexClient.StartImportAsync(startImportRequest, cancellationToken).ConfigureAwait(false);

                // If the import is not complete, poll until import is complete
                if (startImportResponse.ImportStatus != ImportStatus.COMPLETE)
                {
                    await this.PollBotImportStatusAsync(startImportResponse.ImportId, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        private async Task PollBotImportStatusAsync(string importId, CancellationToken cancellationToken)
        {
            var getImportRequest = new GetImportRequest
            {
                ImportId = importId,
            };

            var count = 0;
            while (true)
            {
                // Check the status of the import operation
                var getImportResponse = await this.LexClient.GetImportAsync(getImportRequest, cancellationToken).ConfigureAwait(false);

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

                // If in progress, delay with linear backoff
                var delay = TimeSpan.FromSeconds(GetImportDelaySeconds * ++count);
                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task BuildBotAsync(CancellationToken cancellationToken)
        {
            // Get the latest bot configuration
            var getBotRequest = new GetBotRequest
            {
                Name = this.LexBotName,
                VersionOrAlias = "$LATEST",
            };

            var getBotResponse = await this.LexClient.GetBotAsync(getBotRequest, cancellationToken).ConfigureAwait(false);

            // Call PutBot with the latest GetBot response
            var putBotBuildRequest = JObject.FromObject(getBotResponse).ToObject<PutBotRequest>();
            putBotBuildRequest.CreateVersion = true;
            putBotBuildRequest.ProcessBehavior = ProcessBehavior.BUILD;

            // Workaround for error received from Lex client
            putBotBuildRequest.AbortStatement.Messages.First().GroupNumber = 1;
            putBotBuildRequest.ClarificationPrompt.Messages.First().GroupNumber = 1;

            Logger.LogTrace($"Building bot '{this.LexBotName}'.");

            await this.LexClient.PutBotAsync(putBotBuildRequest, cancellationToken).ConfigureAwait(false);

            // Poll for until bot is ready
            await this.PollBotReadyStatusAsync(cancellationToken).ConfigureAwait(false);
        }

        private async Task PollBotReadyStatusAsync(CancellationToken cancellationToken)
        {
            var getBotRequest = new GetBotRequest
            {
                Name = this.LexBotName,
                VersionOrAlias = "$LATEST",
            };

            // Wait for Ready status
            var count = 0;
            while (true)
            {
                var getBotResponse = await this.LexClient.GetBotAsync(getBotRequest, cancellationToken).ConfigureAwait(false);
                if (getBotResponse.Status == Status.READY)
                {
                    break;
                }

                // After importing an unchanged bot, a bot status will be "NOT_BUILT"
                if (getBotResponse.Status == Status.NOT_BUILT)
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

                // If building, delay with linear backoff
                var delay = TimeSpan.FromSeconds(GetBotDelaySeconds * ++count);
                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task DeleteBotAliasAsync(CancellationToken cancellationToken)
        {
            try
            {
                var deleteBotAliasRequest = new DeleteBotAliasRequest
                {
                    BotName = this.LexBotName,
                    Name = this.LexBotAlias,
                };

                Logger.LogTrace($"Deleting bot alias '{this.LexBotAlias}' for bot '{this.LexBotName}'.");

                await this.LexClient.DeleteBotAliasAsync(deleteBotAliasRequest, cancellationToken).ConfigureAwait(false);
            }
            catch (Amazon.LexModelBuildingService.Model.NotFoundException exception)
            {
                // Likely that no bot alias was published
                Logger.LogWarning(exception, $"Could not delete bot alias '{this.LexBotAlias}' for bot '{this.LexBotName}'.");
            }
        }

        private async Task DeleteBotAsync(CancellationToken cancellationToken)
        {
            try
            {
                var deleteBotRequest = new DeleteBotRequest
                {
                    Name = this.LexBotName,
                };

                Logger.LogTrace($"Deleting bot '{this.LexBotName}'.");

                await this.LexClient.DeleteBotAsync(deleteBotRequest, cancellationToken).ConfigureAwait(false);
            }
            catch (Amazon.LexModelBuildingService.Model.NotFoundException exception)
            {
                // Likely that bot was not created
                Logger.LogWarning(exception, $"Could not delete bot '{this.LexBotName}'.");
            }
        }

        private async Task<bool> IsPublishedAsync(CancellationToken cancellationToken)
        {
            var getBotAliasesRequest = new GetBotAliasesRequest
            {
                BotName = this.LexBotName,
                NameContains = this.LexBotAlias,
            };

            var getBotAliasesResponse = await this.LexClient.GetBotAliasesAsync(getBotAliasesRequest, cancellationToken).ConfigureAwait(false);
            return getBotAliasesResponse.BotAliases.Any(botAlias => botAlias.Name == this.LexBotAlias);
        }

        private async Task PublishBotAsync(CancellationToken cancellationToken)
        {
            // Creates an alias that can be used for testing
            var putBotAliasRequest = new PutBotAliasRequest
            {
                BotName = this.LexBotName,
                BotVersion = "$LATEST",
                Name = this.LexBotAlias,
            };

            Logger.LogTrace($"Publishing bot alias '{this.LexBotAlias}' for bot '{this.LexBotName}'.");

            await this.LexClient.PutBotAliasAsync(putBotAliasRequest, cancellationToken).ConfigureAwait(false);
        }
    }
}
