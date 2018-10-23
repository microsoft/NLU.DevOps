// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace LanguageUnderstanding.Models
{
    using System.Collections.Generic;

    /// <summary>
    /// Labeled utterance.
    /// </summary>
    public class LabeledUtterance
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LabeledUtterance"/> class.
        /// </summary>
        /// <param name="text">Text of the utterance.</param>
        /// <param name="intent">Intent of the utterance.</param>
        /// <param name="entities">Entities referenced in the utterance.</param>
        public LabeledUtterance(string text, string intent, IReadOnlyList<Entity> entities)
        {
            this.Text = text;
            this.Intent = intent;
            this.Entities = entities;
        }

        /// <summary>
        /// Gets the text of the utterance.
        /// </summary>
        public string Text { get; }

        /// <summary>
        /// Gets the intent of the utterance.
        /// </summary>
        public string Intent { get; }

        /// <summary>
        /// Gets the entities referenced in the utterance.
        /// </summary>
        public IReadOnlyList<Entity> Entities { get; }
    }
}
