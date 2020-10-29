// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace NLU.DevOps.Core
{
    using System.Collections.Generic;
    using Models;
    using Newtonsoft.Json;

    /// <summary>
    /// Labeled utterance.
    /// </summary>
    public class LabeledUtterance : ILabeledUtterance, IJsonExtension
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LabeledUtterance"/> class.
        /// </summary>
        /// <param name="text">Text of the utterance.</param>
        /// <param name="intent">Intent of the utterance.</param>
        /// <param name="entities">Entities referenced in the utterance.</param>
        public LabeledUtterance(string text, string intent, IReadOnlyList<IEntity> entities)
        {
            this.Text = text;
            this.Intent = intent;
            this.Entities = entities;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LabeledUtterance"/> class.
        /// </summary>
        /// <param name="text">Text of the utterance.</param>
        /// <param name="intent">Intent of the utterance.</param>
        /// <param name="entities">Entities referenced in the utterance.</param>
        [JsonConstructor]
        private LabeledUtterance(string text, string intent, IReadOnlyList<Entity> entities)
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
        public IReadOnlyList<IEntity> Entities { get; }

        /// <summary>
        /// Gets the additional properties for the labeled utterance.
        /// </summary>
        [JsonExtensionData]
        public IDictionary<string, object> AdditionalProperties { get; } = new Dictionary<string, object>();
    }
}
