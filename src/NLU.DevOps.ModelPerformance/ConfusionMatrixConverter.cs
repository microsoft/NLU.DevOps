// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace NLU.DevOps.ModelPerformance
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// JSON converter for <see cref="ConfusionMatrix"/>.
    /// </summary>
    public class ConfusionMatrixConverter : JsonConverter<ConfusionMatrix>
    {
        /// <inheritdoc />
        public override ConfusionMatrix ReadJson(JsonReader reader, Type objectType, ConfusionMatrix existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var json = JToken.Load(reader);
            if (json.Type == JTokenType.Null || json.Type == JTokenType.Undefined)
            {
                return null;
            }

            var jsonArray = json as JArray;
            if (jsonArray == null || jsonArray.Count != 4 || jsonArray.Any(t => t.Type != JTokenType.Integer))
            {
                throw new InvalidOperationException("Expected JSON array of confusion matrix integers.");
            }

            return new ConfusionMatrix(
                jsonArray[0].Value<int>(),
                jsonArray[1].Value<int>(),
                jsonArray[2].Value<int>(),
                jsonArray[3].Value<int>());
        }

        /// <inheritdoc />
        public override void WriteJson(JsonWriter writer, ConfusionMatrix value, JsonSerializer serializer)
        {
            Debug.Assert(value != null, "Newtonsoft.Json will not call this method if the value is null.");
            writer.WriteStartArray();
            writer.WriteValue(value.TruePositive);
            writer.WriteValue(value.TrueNegative);
            writer.WriteValue(value.FalsePositive);
            writer.WriteValue(value.FalseNegative);
            writer.WriteEndArray();
        }
    }
}
