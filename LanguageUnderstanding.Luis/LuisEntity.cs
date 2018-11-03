// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace LanguageUnderstanding.Luis
{
    using System.Text.RegularExpressions;
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
            var startCharIndex = MatchIndexToStartCharIndex(utterance, entity.MatchText, entity.MatchIndex);
            var endCharIndex = startCharIndex + entity.MatchText.Length - 1;
            return new LuisEntity(entity.EntityType, startCharIndex, endCharIndex);
        }

        /// <summary>
        /// Converts <see cref="Entity.MatchIndex"/> used in <see cref="Entity"/> to the <see cref="StartCharIndex"/> used in <see cref="LuisEntity"/>
        /// </summary>
        /// <returns>Starting character index of the <paramref name="matchText"/>.</returns>
        /// <param name="utterance">Utterance where the entity occurs.</param>
        /// <param name="matchText">Matching text in the utterance.</param>
        /// <param name="matchIndex">Occurrence index of the <paramref name="matchText"/>.</param>
        private static int MatchIndexToStartCharIndex(string utterance, string matchText, int matchIndex)
        {
            var matches = Regex.Match(utterance, string.Format(@"\b{0}\b", matchText));
            for (var i = 0; i < matchIndex; ++i)
            {
                matches = matches.NextMatch();
            }

            return matches.Index;
        }
    }
}
