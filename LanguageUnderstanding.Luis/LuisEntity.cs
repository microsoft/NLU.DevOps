// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace LanguageUnderstanding.Luis
{
    using Models;
    using Newtonsoft.Json;

    /// <summary>
    /// An <see cref="Entity"/> for LUIS.
    /// </summary>
    internal class LuisEntity
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LuisEntity"/> class.
        /// </summary>
        /// <param name="entityName">Entity name.</param>
        /// <param name="startCharIndex">Starting character index of the entity in the utterance.</param>
        /// <param name="endCharIndex">Ending character index of the entity in the utterance.</param>
        [JsonConstructor]
        public LuisEntity(string entityName, int startCharIndex, int endCharIndex)
        {
            this.EntityName = entityName;
            this.StartCharIndex = startCharIndex;
            this.EndCharIndex = endCharIndex;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LuisEntity"/> class.
        /// </summary>
        /// <param name="entityType">Entity type name.</param>
        /// <param name="entityValue">Entity value, generally a canonical form of the entity.</param>
        /// <param name="startCharIndex">Starting index of matching token in the utterance.</param>
        /// <param name="endCharIndex">Ending index of matching token in the utterance.</param>
        public LuisEntity(string entityType, string entityValue, int startCharIndex, int endCharIndex)
        {
            this.EntityName = entityType + "::" + entityValue;
            this.StartCharIndex = startCharIndex;
            this.EndCharIndex = endCharIndex;
        }

        /// <summary>
        /// Gets the entity label specifically needed for LUIS.
        /// </summary>
        [JsonProperty("entity")]
        public string EntityName { get; }

        /// <summary>
        /// Gets the occurrence index of matching token in the utterance.
        /// </summary>
        [JsonProperty("startCharIndex")]
        public int StartCharIndex { get; }

        /// <summary>
        /// Gets the index of the end of the matching token in the utterance.
        /// </summary>
        [JsonProperty("endCharIndex")]
        public int EndCharIndex { get; }

        /// <summary>
        /// Converts an <see cref="Entity"/> to <see cref="LuisEntity"/>.
        /// </summary>
        /// <returns>A <see cref="LuisEntity"/>.</returns>
        /// <param name="entity"><see cref="Entity"/> being converted.</param>
        /// <param name="utterance">Utterance in which the entity occurs.</param>
        public static LuisEntity FromEntity(Entity entity, string utterance)
        {
            var startCharIndex = entity.StartCharIndexInText(utterance);
            var endCharIndex = startCharIndex + entity.MatchText.Length - 1;
            return new LuisEntity(entity.EntityType, startCharIndex, endCharIndex);
        }
    }
}
