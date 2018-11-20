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
        /// Gets the entity label specifically needed for LUIS.
        /// </summary>
        [JsonProperty("entity")]
        public string EntityName { get; }

        /// <summary>
        /// Gets the occurrence index of matching token in the utterance.
        /// </summary>
        [JsonProperty("startPos")]
        public int StartCharIndex { get; }

        /// <summary>
        /// Gets the index of the end of the matching token in the utterance.
        /// </summary>
        [JsonProperty("endPos")]
        public int EndCharIndex { get; }

        /// <summary>
        /// Converts an <see cref="Entity"/> to <see cref="LuisEntity"/>.
        /// </summary>
        /// <returns>A <see cref="LuisEntity"/>.</returns>
        /// <param name="entity"><see cref="Entity"/> being converted.</param>
        /// <param name="utterance">Utterance in which the entity occurs.</param>
        /// <param name="entityType">Entity type.</param>
        public static LuisEntity FromEntity(Entity entity, string utterance, EntityType entityType)
        {
            var startCharIndex = entity.StartCharIndexInText(utterance);
            var endCharIndex = startCharIndex + entity.MatchText.Length - 1;

            // Builtin entities do not use a custom label
            var entityName = entityType is BuiltinEntityType builtinEntityType
                ? builtinEntityType.BuiltinId
                : entity.EntityType;

            return new LuisEntity(entityName, startCharIndex, endCharIndex);
        }
    }
}
