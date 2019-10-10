// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace NLU.DevOps.Core
{
    using System.Collections.Generic;
    using Models;
    using Newtonsoft.Json;

    /// <summary>
    /// Labeled utterance with any additional JSON properties.
    /// </summary>
    public class JsonLabeledUtterance : LabeledUtterance, IJsonExtension
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="JsonLabeledUtterance"/> class.
        /// </summary>
        /// <param name="text">Text of the utterance.</param>
        /// <param name="intent">Intent of the utterance.</param>
        /// <param name="entities">Entities referenced in the utterance.</param>
        public JsonLabeledUtterance(string text, string intent, IReadOnlyList<Entity> entities)
            : base(text, intent, entities)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonLabeledUtterance"/> class.
        /// </summary>
        /// <param name="text">Text of the utterance.</param>
        /// <param name="intent">Intent of the utterance.</param>
        /// <param name="entities">Entities referenced in the utterance.</param>
        [JsonConstructor]
        private JsonLabeledUtterance(string text, string intent, IReadOnlyList<JsonEntity> entities)
            : base(text, intent, entities)
        {
        }

        /// <summary>
        /// Gets the additional properties for the labeled utterance.
        /// </summary>
        [JsonExtensionData]
        public IDictionary<string, object> AdditionalProperties { get; } = new Dictionary<string, object>();
    }
}
