// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace LanguageUnderstanding.Loaders
{
    using System;
    using Models;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Custom JSON converter for entities file
    /// </summary>
    public class EntityTypesJsonConverter : JsonConverter
    {
        /// <inheritdoc/>
        public override bool CanWrite => false;

        /// <inheritdoc/>
        public override bool CanConvert(Type objectType) => objectType == typeof(EntityType);

        /// <inheritdoc/>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var jsonObject = JObject.Load(reader);
            var kind = jsonObject["kind"].Value<string>();
            switch (kind)
            {
                case "builtin":
                    return jsonObject.ToObject<BuiltinEntityType>();
                case "simple":
                    return jsonObject.ToObject<SimpleEntityType>();
                case "list":
                    return jsonObject.ToObject<ListEntityType>();
                default:
                    throw new NotSupportedException($"Value '{kind}' is not supported for kind property.");
            }

            throw new NotImplementedException();
            }

        /// <inheritdoc/>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
