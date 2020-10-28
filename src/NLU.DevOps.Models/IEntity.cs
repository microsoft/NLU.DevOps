// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace NLU.DevOps.Models
{
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Entity appearing in utterance.
    /// </summary>
    public interface IEntity
    {
        /// <summary>
        /// Gets the entity type name.
        /// </summary>
        string EntityType { get; }

        /// <summary>
        /// Gets the entity value, generally a canonical form of the entity.
        /// </summary>
        JToken EntityValue { get; }

        /// <summary>
        /// Gets the matching text in the utterance.
        /// </summary>
        string MatchText { get; }

        /// <summary>
        /// Gets the occurrence index of matching token in the utterance.
        /// </summary>
        int MatchIndex { get; }
    }
}
