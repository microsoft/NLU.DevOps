// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace NLU.DevOps.Luis
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Models;
    using Newtonsoft.Json;

    /// <summary>
    /// A <see cref="LabeledUtterance"/> for the LUIS service.
    /// </summary>
    internal class LuisLabeledUtterance
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LuisLabeledUtterance"/> class.
        /// </summary>
        /// <param name="text">Text of the utterance.</param>
        /// <param name="intent">Intent of the utterance.</param>
        /// <param name="entities">Entities referenced in the utterance.</param>
        [JsonConstructor]
        public LuisLabeledUtterance(string text, string intent, IReadOnlyList<LuisEntity> entities)
        {
            this.Text = text;
            this.Intent = intent;
            this.LuisEntities = entities ?? Array.Empty<LuisEntity>();
        }

        /// <summary>
        /// Gets the text of the utterance.
        /// </summary>
        [JsonProperty("text")]
        public string Text { get; }

        /// <summary>
        /// Gets the intent of the utterance.
        /// </summary>
        [JsonProperty("intent")]
        public string Intent { get; }

        /// <summary>
        /// Gets the LUIS entities referenced in the utterance.
        /// </summary>
        [JsonProperty("entities")]
        public IReadOnlyList<LuisEntity> LuisEntities { get; }

        /// <summary>
        /// Converts a <see cref="LabeledUtterance"/> to <see cref="LuisLabeledUtterance"/>.
        /// </summary>
        /// <returns>A <see cref="LuisLabeledUtterance"/>.</returns>
        /// <param name="utterance"><see cref="LabeledUtterance"/> being converted.</param>
        /// <param name="entityTypes">Entity type configuration for the utterances.</param>
        public static LuisLabeledUtterance FromLabeledUtterance(LabeledUtterance utterance, IEnumerable<EntityType> entityTypes)
        {
            var text = utterance.Text;

            var entities = from entity in utterance.Entities
                           let entityType = entityTypes.First(item => item.Name == entity.EntityType)
                           where entityType.Kind != "builtin"
                           select LuisEntity.FromEntity(entity, text, entityType);

            return new LuisLabeledUtterance(text, utterance.Intent, entities.ToList());
        }
    }
}
