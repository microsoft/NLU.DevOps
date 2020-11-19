// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace NLU.DevOps.Core
{
    using System.Collections.Generic;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Entity appearing in utterance.
    /// </summary>
    public sealed class HierarchicalEntity : Entity, IHierarchicalEntity
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HierarchicalEntity"/> class.
        /// </summary>
        /// <param name="entityType">Entity type name.</param>
        /// <param name="entityValue">Entity value, generally a canonical form of the entity.</param>
        /// <param name="matchText">Matching text in the utterance.</param>
        /// <param name="matchIndex">Occurrence index of matching token in the utterance.</param>
        /// <param name="children">Children entities.</param>
        public HierarchicalEntity(string entityType, JToken entityValue, string matchText, int matchIndex, IEnumerable<HierarchicalEntity> children)
            : base(entityType, entityValue, matchText, matchIndex)
        {
            this.Children = children;
        }

        /// <inheritdoc />
        public IEnumerable<IHierarchicalEntity> Children { get; }
    }
}
