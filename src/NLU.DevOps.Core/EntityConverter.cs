// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace NLU.DevOps.Core
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    internal class EntityConverter : JsonConverter<Entity>
    {
        public EntityConverter(string utterance)
        {
            this.Utterance = utterance;
        }

        private string Utterance { get; }

        private string Prefix { get; set; } = string.Empty;

        public override Entity ReadJson(JsonReader reader, Type objectType, Entity existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            Debug.Assert(!hasExistingValue, "Entity instance can only be constructor initialized.");

            var jsonObject = JObject.Load(reader);
            return typeof(HierarchicalEntity).IsAssignableFrom(objectType)
                ? this.ReadHierarchicalEntity(jsonObject, serializer)
                : this.ReadEntity(jsonObject, objectType, serializer);
        }

        public override void WriteJson(JsonWriter writer, Entity value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        private Entity ReadEntity(JObject jsonObject, Type objectType, JsonSerializer serializer)
        {
            var matchText = jsonObject.Value<string>("matchText");
            var matchIndex = jsonObject.Value<int>("matchIndex");
            var startPosOrNull = jsonObject.Value<int?>("startPos");
            var endPosOrNull = jsonObject.Value<int?>("endPos");
            if (matchText == null && startPosOrNull.HasValue && endPosOrNull.HasValue)
            {
                (matchText, matchIndex) = this.GetMatchInfo(startPosOrNull.Value, endPosOrNull.Value);
                jsonObject.Add("matchText", matchText);
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

        private HierarchicalEntity ReadHierarchicalEntity(JObject jsonObject, JsonSerializer serializer)
        {
            var matchText = jsonObject.Value<string>("matchText");
            var matchIndex = jsonObject.Value<int>("matchIndex");
            var startPosOrNull = jsonObject.Value<int?>("startPos");
            var endPosOrNull = jsonObject.Value<int?>("endPos");
            if (matchText == null && startPosOrNull.HasValue && endPosOrNull.HasValue)
            {
                (matchText, matchIndex) = this.GetMatchInfo(startPosOrNull.Value, endPosOrNull.Value);
            }

            var entityType = jsonObject.Value<string>("entityType") ?? jsonObject.Value<string>("entity");
            var childrenJson = jsonObject["children"];
            var children = default(IEnumerable<HierarchicalEntity>);
            if (childrenJson != null)
            {
                var prefix = $"{entityType}::";
                this.Prefix += prefix;
                try
                {
                    children = childrenJson.ToObject<IEnumerable<HierarchicalEntity>>(serializer);
                }
                finally
                {
                    this.Prefix = this.Prefix.Substring(0, this.Prefix.Length - prefix.Length);
                }
            }

            var entity = new HierarchicalEntity($"{this.Prefix}{entityType}", jsonObject["entityValue"], matchText, matchIndex, children);
            foreach (var property in jsonObject)
            {
                switch (property.Key)
                {
                    case "children":
                    case "endPos":
                    case "entity":
                    case "entityType":
                    case "entityValue":
                    case "matchText":
                    case "matchIndex":
                    case "startPos":
                        break;
                    default:
                        var value = property.Value is JValue jsonValue ? jsonValue.Value : property.Value;
                        entity.AdditionalProperties.Add(property.Key, value);
                        break;
                }
            }

            return entity;
        }

        private Tuple<string, int> GetMatchInfo(int startPos, int endPos)
        {
            if (!this.IsValid(startPos, endPos))
            {
                throw new InvalidOperationException(
                    $"Invalid start position '{startPos}' or end position '{endPos}' for utterance '{this.Utterance}'.");
            }

            var length = endPos - startPos + 1;
            var matchText = this.Utterance.Substring(startPos, length);
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

            return Tuple.Create(matchText, matchIndex);
        }

        private bool IsValid(int startPos, int endPos)
        {
            return startPos <= endPos
                && startPos < this.Utterance.Length
                && endPos < this.Utterance.Length;
        }
    }
}
