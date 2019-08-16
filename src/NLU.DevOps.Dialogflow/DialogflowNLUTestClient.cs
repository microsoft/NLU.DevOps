// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace NLU.DevOps.Dialogflow
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Core;
    using Google.Apis.Auth.OAuth2;
    using Google.Cloud.Dialogflow.V2;
    using Google.Protobuf;
    using Google.Protobuf.WellKnownTypes;
    using Grpc.Auth;
    using Microsoft.Extensions.Configuration;
    using Models;
    using Newtonsoft.Json.Linq;

    internal sealed class DialogflowNLUTestClient : DefaultNLUTestClient
    {
        private const string DialogflowClientKeyJsonConfigurationKey = "dialogflowClientKeyJson";
        private const string DialogflowClientKeyPathConfigurationKey = "dialogflowClientKeyPath";
        private const string DialogflowProjectIdConfigurationKey = "dialogflowProjectId";
        private const string DialogflowSessionIdConfigurationKey = "dialogflowSessionId";

        private SessionsClient sessionsClient;

        public DialogflowNLUTestClient(SessionsClient sessionsClient, IConfiguration configuration)
            : this(configuration)
        {
            this.sessionsClient = sessionsClient;
        }

        public DialogflowNLUTestClient(IConfiguration configuration)
        {
            this.Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        private IConfiguration Configuration { get; }

        private string ProjectId => this.GetConfigurationValue(DialogflowProjectIdConfigurationKey);

        private string SessionId => this.GetConfigurationValue(DialogflowSessionIdConfigurationKey, true);

        protected override async Task<LabeledUtterance> TestAsync(string utterance, CancellationToken cancellationToken)
        {
            var sessionId = this.SessionId ?? Guid.NewGuid().ToString();
            var sessionName = new SessionName(this.ProjectId, sessionId);
            var queryInput = new QueryInput
            {
                Text = new TextInput
                {
                    Text = utterance,
                    LanguageCode = "en",
                }
            };

            var client = await this.GetSessionClientAsync(cancellationToken).ConfigureAwait(false);
            var result = await client.DetectIntentAsync(sessionName, queryInput, cancellationToken).ConfigureAwait(false);
            return new LabeledUtterance(
                result.QueryResult.QueryText,
                result.QueryResult.Intent.DisplayName,
                result.QueryResult.Parameters?.Fields.Select(GetEntity).OfType<Entity>().ToList());
        }

        protected override async Task<LabeledUtterance> TestSpeechAsync(string speechFile, CancellationToken cancellationToken)
        {
            var sessionId = this.SessionId ?? Guid.NewGuid().ToString();
            var sessionName = new SessionName(this.ProjectId, sessionId);
            var byteString = await ByteString.FromStreamAsync(File.OpenRead(speechFile), cancellationToken).ConfigureAwait(false);
            var request = new DetectIntentRequest
            {
                InputAudio = byteString,
                SessionAsSessionName = sessionName,
                QueryInput = new QueryInput
                {
                    AudioConfig = new InputAudioConfig
                    {
                        AudioEncoding = AudioEncoding.Unspecified,
                        LanguageCode = "en",
                        SampleRateHertz = 16000,
                    },
                },
            };

            var client = await this.GetSessionClientAsync(cancellationToken).ConfigureAwait(false);
            var result = await client.DetectIntentAsync(request, cancellationToken).ConfigureAwait(false);
            return new LabeledUtterance(
                result.QueryResult.QueryText,
                result.QueryResult.Intent.DisplayName,
                result.QueryResult.Parameters?.Fields.Select(GetEntity).OfType<Entity>().ToList());
        }

        protected override void Dispose(bool disposing)
        {
            // no-op
        }

        private static Entity GetEntity(KeyValuePair<string, Value> pair)
        {
            JToken toJson(Value value)
            {
                switch (value.KindCase)
                {
                    case Value.KindOneofCase.BoolValue:
                        return value.BoolValue;
                    case Value.KindOneofCase.NumberValue:
                        return value.NumberValue;
                    case Value.KindOneofCase.StringValue:
                        return string.IsNullOrEmpty(value.StringValue)
                            ? default(JToken)
                            : value.StringValue;
                    case Value.KindOneofCase.ListValue:
                        return new JArray(value.ListValue.Values.Select(toJson).ToArray());
                    case Value.KindOneofCase.StructValue:
                        var jsonObject = new JObject();
                        foreach (var field in value.StructValue.Fields)
                        {
                            jsonObject.Add(field.Key, toJson(field.Value));
                        }

                        return jsonObject;
                    default:
                        return null;
                }
            }

            var jsonValue = toJson(pair.Value);
            if (jsonValue == null)
            {
                return null;
            }

            return new Entity(pair.Key, jsonValue, null, null, 0);
        }

        private async Task<SessionsClient> GetSessionClientAsync(CancellationToken cancellationToken)
        {
            if (this.sessionsClient != null)
            {
                return this.sessionsClient;
            }

            var keyPath = this.GetConfigurationValue(DialogflowClientKeyPathConfigurationKey, true);
            var googleCredential = keyPath != null
                ? GoogleCredential.FromFile(keyPath)
                : GoogleCredential.FromJson(
                    this.GetConfigurationValue(DialogflowClientKeyJsonConfigurationKey));

            var builder = new SessionsClientBuilder
            {
                ChannelCredentials = googleCredential.ToChannelCredentials(),
            };

            return this.sessionsClient = await builder.BuildAsync(cancellationToken).ConfigureAwait(false);
        }

        private string GetConfigurationValue(string key, bool optional = false)
        {
            var value = this.Configuration[key];
            if (value == null && !optional)
            {
                throw new InvalidOperationException($"Configuration value for '{key}' must be set.");
            }

            return value;
        }
    }
}
