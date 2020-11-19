// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace NLU.DevOps.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Models;
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
        public JsonEntities(IEnumerable<HierarchicalEntity> entities)
        {
            this.Entities = FlattenChildren(entities)?.ToArray();
        }

        /// <summary>
        /// Gets the entities referenced in the utterance.
        /// </summary>
        public IReadOnlyList<IEntity> Entities { get; }

        /// <summary>
        /// Gets the additional properties.
        /// </summary>
        [JsonExtensionData]
        public IDictionary<string, object> AdditionalProperties { get; } = new Dictionary<string, object>();

        private static IEnumerable<IEntity> FlattenChildren(IEnumerable<IHierarchicalEntity> entities, string prefix = "")
        {
            if (entities == null)
            {
                return null;
            }

            IEnumerable<IEntity> getChildren(IHierarchicalEntity entity)
            {
                yield return entity;

                var children = FlattenChildren(entity.Children, $"{prefix}{entity.EntityType}::");
                if (children != null)
                {
                    foreach (var child in children)
                    {
                        yield return child;
                    }
                }
            }

            return entities.SelectMany(getChildren);
        }
    }
}
