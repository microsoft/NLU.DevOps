// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace NLU.DevOps.Core
{
    using System.Collections.Generic;
    using Models;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Entity appearing in utterance.
    /// </summary>
    public class Entity : IEntity, IJsonExtension
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Entity"/> class.
        /// </summary>
        /// <param name="entityType">Entity type name.</param>
        /// <param name="entityValue">Entity value, generally a canonical form of the entity.</param>
        /// <param name="matchText">Matching text in the utterance.</param>
        /// <param name="matchIndex">Occurrence index of matching token in the utterance.</param>
        public Entity(string entityType, JToken entityValue, string matchText, int matchIndex)
        {
            this.EntityType = entityType;
            this.EntityValue = entityValue;
            this.MatchText = matchText;
            this.MatchIndex = matchIndex;
        }

        /// <inheritdoc />
        public string EntityType { get; }

        /// <inheritdoc />
        public JToken EntityValue { get; }

        /// <inheritdoc />
        public string MatchText { get; }

        /// <inheritdoc />
        public int MatchIndex { get; }

        /// <inheritdoc />
        [JsonExtensionData]
        public IDictionary<string, object> AdditionalProperties { get; } = new Dictionary<string, object>();
    }
}
