// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace LanguageUnderstanding.Json
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Models;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using Newtonsoft.Json.Serialization;

    /// <summary>
    /// Serializes and parses labeled utterances to JSON files.
    /// </summary>
    public static class Serialization
    {
        /// <summary>
        /// Returns list of entities from file.
        /// </summary>
        /// <param name="filePath">File path.</param>
        /// <returns>List of entities.</returns>
        public static IReadOnlyList<EntityType> ReadEntities(string filePath)
        {
            var jsonEntities = File.ReadAllText(filePath);
            return JsonConvert.DeserializeObject<List<EntityType>>(jsonEntities, new EntityTypesJsonConverter());
        }

        /// <summary>
        /// Returns list of utterances from file.
        /// </summary>
        /// <param name="filePath">File path.</param>
        /// <returns>List of utterances.</returns>
        public static IReadOnlyList<LabeledUtterance> ReadUtterances(string filePath)
        {
            var jsonUtterences = File.ReadAllText(filePath);
            var utterances = JsonConvert.DeserializeObject<List<LabeledUtterance>>(jsonUtterences);
            return utterances;
        }

        /// <summary>
        /// Writes the list of utterances to a file.
        /// </summary>
        /// <param name="filePath">File path.</param>
        /// <param name="utterances">List of utterances.</param>
        public static void WriteUtterances(string filePath, IReadOnlyList<LabeledUtterance> utterances)
        {
            var settings = new JsonSerializerSettings();
            settings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            var jsonUtterances = JsonConvert.SerializeObject(utterances, settings);
            File.WriteAllText(filePath, jsonUtterances);
        }

        private class EntityTypesJsonConverter : JsonConverter
        {
            public override bool CanWrite => false;

            public override bool CanConvert(Type objectType) => objectType == typeof(EntityType);

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
            }

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                throw new NotImplementedException();
            }
        }
    }
}
