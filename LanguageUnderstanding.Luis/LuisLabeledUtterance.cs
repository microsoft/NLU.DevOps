// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace LanguageUnderstanding.Luis
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
        /// <param name="entityLabels">Entities referenced in the utterance.</param>
        [JsonConstructor]
        public LuisLabeledUtterance(string text, string intent, IReadOnlyList<LuisEntity> entityLabels)
        {
            this.Text = text;
            this.Intent = intent;
            this.LuisEntities = entityLabels ?? Array.Empty<LuisEntity>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LuisLabeledUtterance"/> class.
        /// </summary>
        /// <param name="that">A <see cref="LabeledUtterance"/>.</param>
        public LuisLabeledUtterance(LabeledUtterance that)
        {
            this.Text = that.Text;
            this.Intent = that.Intent;
            this.LuisEntities = that.Entities
                .Select(entity => LuisEntity.FromEntity(entity, that.Text))
                .ToList();
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
    }
}
