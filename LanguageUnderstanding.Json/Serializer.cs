// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace LanguageUnderstanding.Json
{
    using System;
    using System.IO;
    using System.Text;
    using Models;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using Newtonsoft.Json.Serialization;

    /// <summary>
    /// Serializes and parses labeled utterances to JSON files.
    /// </summary>
    public static class Serializer
    {
        private static Lazy<JsonSerializerSettings> LazyJsonSerializerSettings =>
            new Lazy<JsonSerializerSettings>(() => new JsonSerializerSettings
            {
                Converters = { new EntityTypesJsonConverter() },
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
            });

        /// <summary>
        /// Reads JSON value from a file.
        /// </summary>
        /// <typeparam name="T">Type of value to read.</typeparam>
        /// <param name="filePath">File path.</param>
        /// <returns>JSON data parsed into object.</returns>
        public static T Read<T>(string filePath)
        {
            using (var stream = File.OpenRead(filePath))
            {
                return Read<T>(stream);
            }
        }

        /// <summary>
        /// Reads JSON value from a stream.
        /// </summary>
        /// <typeparam name="T">Type of value to read.</typeparam>
        /// <param name="stream">File path.</param>
        /// <returns>JSON data parsed into object.</returns>
        public static T Read<T>(Stream stream)
        {
            using (var streamReader = new StreamReader(stream, Encoding.UTF8, true, 4096, true))
            {
                var jsonEntities = streamReader.ReadToEnd();
                return JsonConvert.DeserializeObject<T>(jsonEntities, LazyJsonSerializerSettings.Value);
            }
        }

        /// <summary>
        /// Writes the value as JSON to a file.
        /// </summary>
        /// <typeparam name="T">Type of value.</typeparam>
        /// <param name="filePath">File path.</param>
        /// <param name="value">Value to write.</param>
        public static void Write<T>(string filePath, T value)
        {
            using (var stream = File.OpenWrite(filePath))
            {
                Write(stream, value);
            }
        }

        /// <summary>
        /// Writes the value as JSON to a file.
        /// </summary>
        /// <typeparam name="T">Type of value.</typeparam>
        /// <param name="stream">Stream.</param>
        /// <param name="value">Value to write.</param>
        public static void Write<T>(Stream stream, T value)
        {
            var jsonUtterances = JsonConvert.SerializeObject(value, Formatting.Indented, LazyJsonSerializerSettings.Value);
            using (var streamWriter = new StreamWriter(stream, Encoding.UTF8, 4096, true))
            {
                streamWriter.Write(jsonUtterances);
            }
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
