// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace NLU.DevOps.Core
{
    using System.Collections.Generic;
    using Models;
    using Newtonsoft.Json;

    /// <summary>
    /// Raw JSON labeled utterance.
    /// </summary>
    public sealed class JsonLabeledUtterance : ILabeledUtterance, IJsonExtension
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="JsonLabeledUtterance"/> class.
        /// </summary>
        /// <param name="jsonEntities">Utterance JSON with only entities parsed.</param>
        public JsonLabeledUtterance(JsonEntities jsonEntities)
        {
            this.JsonEntities = jsonEntities;
        }

        /// <inheritdoc />
        public string Text => this.Value<string>("text", "query");

        /// <inheritdoc />
        public string Intent => this.Value<string>("intent");

        /// <inheritdoc />
        public IReadOnlyList<IEntity> Entities => this.JsonEntities.Entities;

        /// <inheritdoc />
        [JsonExtensionData]
        public IDictionary<string, object> AdditionalProperties => this.JsonEntities.AdditionalProperties;

        private JsonEntities JsonEntities { get; }

        private T Value<T>(params string[] propertyNames)
        {
            foreach (var propertyName in propertyNames)
            {
                if (this.AdditionalProperties.TryGetValue(propertyName, out var value))
                {
                    return (T)value;
                }
            }

            return default;
        }
    }
}
