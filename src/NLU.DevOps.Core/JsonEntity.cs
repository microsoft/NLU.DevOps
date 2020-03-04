// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace NLU.DevOps.Core
{
    using System.Collections.Generic;
    using Models;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Entity appearing in utterance with any additional JSON properties.
    /// </summary>
    public class JsonEntity : Entity, IJsonExtension
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="JsonEntity"/> class.
        /// </summary>
        /// <param name="entityType">Entity type name.</param>
        /// <param name="entityValue">Entity value, generally a canonical form of the entity.</param>
        /// <param name="matchText">Matching text in the utterance.</param>
        /// <param name="matchIndex">Occurrence index of matching token in the utterance.</param>
        public JsonEntity(string entityType, JToken entityValue, string matchText, int matchIndex)
            : base(entityType, entityValue, matchText, matchIndex)
        {
        }

        /// <summary>
        /// Gets the additional properties for the labeled utterance.
        /// </summary>
        [JsonExtensionData]
        public IDictionary<string, object> AdditionalProperties { get; } = new Dictionary<string, object>();
    }
}
