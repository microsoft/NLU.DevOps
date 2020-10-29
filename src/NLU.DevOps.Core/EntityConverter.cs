// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace NLU.DevOps.Core
{
    using System;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    internal class EntityConverter : JsonConverter<Entity>
    {
        public EntityConverter(string utterance)
        {
            this.Utterance = utterance;
        }

        private string Utterance { get; }

        public override Entity ReadJson(JsonReader reader, Type objectType, Entity existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var jsonObject = JObject.Load(reader);
            var matchText = jsonObject.Value<string>("matchText");
            var startPosOrNull = jsonObject.Value<int?>("startPos");
            var endPosOrNull = jsonObject.Value<int?>("endPos");
            if (matchText == null && startPosOrNull != null && endPosOrNull != null)
            {
                var startPos = startPosOrNull.Value;
                var endPos = endPosOrNull.Value;
                var length = endPos - startPos + 1;
                if (!this.IsValid(startPos, endPos))
                {
                    throw new InvalidOperationException(
                        $"Invalid start position '{startPos}' or end position '{endPos}' for utterance '{this.Utterance}'.");
                }

                matchText = this.Utterance.Substring(startPos, length);
                jsonObject.Add("matchText", matchText);
                var matchIndex = 0;
                var currentPos = 0;
                while (true)
                {
                    currentPos = this.Utterance.IndexOf(matchText, currentPos, StringComparison.InvariantCulture);

                    // Because 'matchText' is derived from the utterance from 'startPos' and 'endPos',
                    // we are guaranteed to find a match at with index 'startPos'.
                    if (currentPos == startPos)
                    {
                        break;
                    }

                    currentPos += length;
                    matchIndex++;
                }

                jsonObject.Add("matchIndex", matchIndex);
                jsonObject.Remove("startPos");
                jsonObject.Remove("endPos");
            }

            var entity = jsonObject.Value<string>("entity");
            var entityType = jsonObject.Value<string>("entityType");
            if (entityType == null && entity != null)
            {
                jsonObject.Add("entityType", entity);
                jsonObject.Remove("entity");
            }

            serializer.Converters.Remove(this);
            try
            {
                return (Entity)jsonObject.ToObject(objectType, serializer);
            }
            finally
            {
                serializer.Converters.Add(this);
            }
        }

        public override void WriteJson(JsonWriter writer, Entity value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        private bool IsValid(int startPos, int endPos)
        {
            return startPos <= endPos
                && startPos < this.Utterance.Length
                && endPos < this.Utterance.Length;
        }
    }
}
