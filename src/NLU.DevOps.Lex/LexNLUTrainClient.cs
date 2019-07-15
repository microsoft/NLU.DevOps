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
    using Amazon.LexModelBuildingService;
    using Amazon.LexModelBuildingService.Model;
    using Amazon.Runtime;
    using Logging;
    using Microsoft.Extensions.Logging;
    using Models;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// NLU train client for Lex.
    /// </summary>
    public sealed class LexNLUTrainClient : INLUTrainClient
    {
        private const int GetBotDelaySeconds = 2;
        private const int GetImportDelaySeconds = 2;

        /// <summary>
        /// Initializes a new instance of the <see cref="LexNLUTrainClient"/> class.
        /// </summary>
        /// <param name="botName">Bot name.</param>
        /// <param name="botAlias">Bot alias.</param>
        /// <param name="lexSettings">Lex settings.</param>
        /// <param name="credentials">Credentials.</param>
        /// <param name="regionEndpoint">Region endpoint.</param>
        public LexNLUTrainClient(
            string botName,
            string botAlias,
            LexSettings lexSettings,
            AWSCredentials credentials,
            RegionEndpoint regionEndpoint)
            : this(botName, botAlias, lexSettings, new LexTrainClient(credentials, regionEndpoint))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LexNLUTrainClient"/> class.
        /// </summary>
        /// <param name="botName">Bot name.</param>
        /// <param name="botAlias">Bot alias.</param>
        /// <param name="lexSettings">Lex settings.</param>
        /// <param name="lexClient">Lex client.</param>
        public LexNLUTrainClient(
            string botName,
            string botAlias,
            LexSettings lexSettings,
            ILexTrainClient lexClient)
        {
            this.LexBotName = botName ?? throw new ArgumentNullException(nameof(botName));
            this.LexBotAlias = botAlias ?? throw new ArgumentNullException(nameof(botAlias));
            this.LexSettings = lexSettings ?? throw new ArgumentNullException(nameof(lexSettings));
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

        private static Lazy<ILogger> LazyLogger { get; } = new Lazy<ILogger>(() => ApplicationLogger.LoggerFactory.CreateLogger<LexNLUTrainClient>());

        private LexSettings LexSettings { get; }

        private ILexTrainClient LexClient { get; }

        /// <inheritdoc />
        public async Task TrainAsync(IEnumerable<LabeledUtterance> utterances, CancellationToken cancellationToken)
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

            // Check if bot exists
            var botExists = await this.BotExistsAsync(cancellationToken).ConfigureAwait(false);

            // Create the bot if does not exist
            if (!botExists)
            {
                await this.CreateBotAsync(cancellationToken).ConfigureAwait(false);
            }

            // Generate the bot configuration
            var importJson = this.CreateImportJson(utterances);

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

        private static void AddOrMergeIntents(JArray intents, IEnumerable<JToken> additionalIntents)
        {
            foreach (JObject intent in additionalIntents)
            {
                var intentName = intent.Value<string>("name");
                var existingIntent = intents.SelectToken($"[?(@.name == '{intentName}')]") as JObject;
                if (existingIntent != null)
                {
                    intent.Merge(existingIntent);
                    var slots = (JArray)intent["slots"];
                    var coalescedSlots = slots.Cast<JObject>()
                        .GroupBy(slot => slot.Value<string>("name"))
                        .Select(group => group.Aggregate(new JObject(), JTokenExtensions.MergeInto));
                    slots.Replace(JArray.FromObject(coalescedSlots));
                    existingIntent.Replace(intent);
                }
                else
                {
                    intents.Add(intent);
                }
            }
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

        private JToken CreateImportJson(IEnumerable<LabeledUtterance> utterances)
        {
            // Add name to imports JSON template
            var importJson = ImportBotTemplates.ImportJson;
            importJson.SelectToken(".resource.name").Replace(this.LexBotName);
            importJson.Merge(this.LexSettings.ImportBotTemplate);

            // Add intents to imports JSON template
            var intents = utterances
                .GroupBy(utterance => utterance.Intent)
                .Select(group => this.CreateIntent(group.Key, group));
            Debug.Assert(importJson.SelectToken(".resource.intents") is JArray, "Import template includes intents JSON array.");
            var intentsArray = (JArray)importJson.SelectToken(".resource.intents");
            AddOrMergeIntents(intentsArray, intents);

            return importJson;
        }

        private JToken CreateIntent(string intent, IEnumerable<LabeledUtterance> utterances)
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
                .Select(slot => this.CreateSlot(slot));
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

        private JToken CreateSlot(string slot)
        {
            var slotJson = ImportBotTemplates.SlotJson;
            var existingSlot = this.LexSettings.Slots.SelectToken($"[?(@.name == '{slot}')]");
            if (existingSlot != null)
            {
                slotJson.Merge(existingSlot);
            }
            else
            {
                slotJson.SelectToken(".name").Replace(slot);
                slotJson.SelectToken(".slotType").Replace(slot);
            }

            return slotJson;
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
                    Debug.Assert(getImportResponse.FailureReason != null, "Default behavior is an empty list for 'FailureReason'.");
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
