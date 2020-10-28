// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace NLU.DevOps.Core
{
    using System.Collections.Generic;
    using Models;
    using Newtonsoft.Json;

    /// <summary>
    /// Labeled utterance with any additional JSON properties.
    /// </summary>
    public class JsonLabeledUtterance : ILabeledUtterance, IJsonExtension
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="JsonLabeledUtterance"/> class.
        /// </summary>
        /// <param name="entities">Entities referenced in the utterance.</param>
        public JsonLabeledUtterance(IReadOnlyList<Entity> entities)
        {
            this.Entities = entities;
        }

        /// <inheritdoc />
        public string Text => this.Value<string>("text", "query");

        /// <inheritdoc />
        public string Intent => this.Value<string>("intent");

        /// <inheritdoc />
        public IReadOnlyList<IEntity> Entities { get; }

        /// <inheritdoc />
        [JsonExtensionData]
        public IDictionary<string, object> AdditionalProperties { get; } = new Dictionary<string, object>();

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
