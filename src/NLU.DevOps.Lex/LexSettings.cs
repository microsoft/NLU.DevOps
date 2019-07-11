// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace NLU.DevOps.Lex
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Lex settings.
    /// </summary>
    public class LexSettings
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LexSettings"/> class.
        /// </summary>
        public LexSettings()
            : this(null, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LexSettings"/> class.
        /// </summary>
        /// <param name="importBotTemplate">Import bot template.</param>
        public LexSettings(JObject importBotTemplate)
            : this(null, importBotTemplate)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LexSettings"/> class.
        /// </summary>
        /// <param name="slots">Entity slot configurations.</param>
        public LexSettings(JArray slots)
            : this(slots, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LexSettings"/> class.
        /// </summary>
        /// <param name="slots">Entity slot configurations.</param>
        /// <param name="importBotTemplate">Import bot template.</param>
        [JsonConstructor]
        public LexSettings(JArray slots, JObject importBotTemplate)
        {
            this.Slots = slots ?? new JArray();
            this.ImportBotTemplate = importBotTemplate ?? new JObject();
        }

        /// <summary>
        /// Gets the entity slots.
        /// </summary>
        public JArray Slots { get; }

        /// <summary>
        /// Gets the import bot template.
        /// </summary>
        public JObject ImportBotTemplate { get; }
    }
}
