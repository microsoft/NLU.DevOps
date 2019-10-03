// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace NLU.DevOps.ModelPerformance
{
    using System;
    using System.Diagnostics;
    using Newtonsoft.Json;

    /// <summary>
    /// JSON converter for <see cref="ConfusionMatrix"/>.
    /// </summary>
    public class ConfusionMatrixConverter : JsonConverter<ConfusionMatrix>
    {
        /// <inheritdoc />
        public override bool CanRead => false;

        /// <inheritdoc />
        public override ConfusionMatrix ReadJson(JsonReader reader, Type objectType, ConfusionMatrix existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
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
