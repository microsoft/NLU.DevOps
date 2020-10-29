// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace NLU.DevOps.Core
{
    using System.Collections.Generic;
    using Newtonsoft.Json;

    /// <summary>
    /// Entities and additional properties.
    /// </summary>
    public class JsonEntities
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="JsonEntities"/> class.
        /// </summary>
        /// <param name="entities">Entities referenced in the utterance.</param>
        public JsonEntities(IReadOnlyList<Entity> entities)
        {
            this.Entities = entities;
        }

        /// <summary>
        /// Gets the entities referenced in the utterance.
        /// </summary>
        public IReadOnlyList<Entity> Entities { get; }

        /// <summary>
        /// Gets the additional properties.
        /// </summary>
        [JsonExtensionData]
        public IDictionary<string, object> AdditionalProperties { get; } = new Dictionary<string, object>();
    }
}
