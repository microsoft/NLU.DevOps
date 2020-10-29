// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace NLU.DevOps.Core
{
    using System;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// JSON converter for <see cref="LabeledUtterance"/> to recognize LUIS batch test format.
    /// </summary>
    public class JsonLabeledUtteranceConverter : JsonConverter<JsonLabeledUtterance>
    {
        /// <inheritdoc />
        public override bool CanWrite => false;

        /// <inheritdoc />
        public override JsonLabeledUtterance ReadJson(JsonReader reader, Type objectType, JsonLabeledUtterance existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var jsonObject = JObject.Load(reader);
            var utterance = jsonObject.Value<string>("text") ?? jsonObject.Value<string>("query");
            var entityConverter = new EntityConverter(utterance);
            serializer.Converters.Add(entityConverter);
            try
            {
                var jsonEntities = jsonObject.ToObject<JsonEntities>(serializer);
                return new JsonLabeledUtterance(jsonEntities);
            }
            finally
            {
                serializer.Converters.Remove(entityConverter);
            }
        }

        /// <inheritdoc />
        public override void WriteJson(JsonWriter writer, JsonLabeledUtterance value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
