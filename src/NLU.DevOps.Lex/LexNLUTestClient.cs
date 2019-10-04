// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace NLU.DevOps.Lex
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Amazon;
    using Amazon.Lex.Model;
    using Amazon.Runtime;
    using Core;
    using Logging;
    using Microsoft.Extensions.Logging;
    using Models;
    using Newtonsoft.Json;

    /// <summary>
    /// NLU test client for Lex.
    /// </summary>
    public sealed class LexNLUTestClient : DefaultNLUTestClient
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LexNLUTestClient"/> class.
        /// </summary>
        /// <param name="botName">Bot name.</param>
        /// <param name="botAlias">Bot alias.</param>
        /// <param name="lexSettings">Lex settings.</param>
        /// <param name="credentials">Credentials.</param>
        /// <param name="regionEndpoint">Region endpoint.</param>
        public LexNLUTestClient(
            string botName,
            string botAlias,
            LexSettings lexSettings,
            AWSCredentials credentials,
            RegionEndpoint regionEndpoint)
            : this(botName, botAlias, lexSettings, new LexTestClient(credentials, regionEndpoint))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LexNLUTestClient"/> class.
        /// </summary>
        /// <param name="botName">Bot name.</param>
        /// <param name="botAlias">Bot alias.</param>
        /// <param name="lexSettings">Lex settings.</param>
        /// <param name="lexClient">Lex client.</param>
        public LexNLUTestClient(
            string botName,
            string botAlias,
            LexSettings lexSettings,
            ILexTestClient lexClient)
        {
            this.LexBotName = botName ?? throw new ArgumentNullException(nameof(botName));
            this.LexBotAlias = botAlias ?? throw new ArgumentNullException(nameof(botAlias));
            this.LexSettings = lexSettings ?? throw new ArgumentNullException(nameof(lexSettings));
            this.LexClient = lexClient ?? throw new ArgumentNullException(nameof(lexClient));
        }

        private static ILogger Logger => LazyLogger.Value;

        private static Lazy<ILogger> LazyLogger { get; } = new Lazy<ILogger>(() => ApplicationLogger.LoggerFactory.CreateLogger<LexNLUTrainClient>());

        private string LexBotName { get; }

        private string LexBotAlias { get; }

        private LexSettings LexSettings { get; }

        private ILexTestClient LexClient { get; }

        /// <inheritdoc />
        protected override async Task<LabeledUtterance> TestAsync(string utterance, CancellationToken cancellationToken)
        {
            if (utterance == null)
            {
                throw new ArgumentNullException(nameof(utterance));
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

            var context = LabeledUtteranceContext.CreateDefault();
            return new PredictedLabeledUtterance(
                utterance,
                postTextResponse.IntentName,
                0,
                0,
                entities,
                context);
        }

        /// <inheritdoc />
        protected override async Task<LabeledUtterance> TestSpeechAsync(string speechFile, CancellationToken cancellationToken)
        {
            if (speechFile == null)
            {
                throw new ArgumentNullException(nameof(speechFile));
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

                var context = LabeledUtteranceContext.CreateDefault();
                return new PredictedLabeledUtterance(
                    postContentResponse.InputTranscript,
                    postContentResponse.IntentName,
                    0,
                    0,
                    slots,
                    context);
            }
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            this.LexClient.Dispose();
        }
    }
}
