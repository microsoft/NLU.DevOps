// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace NLU.DevOps.Lex
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
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
        /// <param name="entityTypes">Entity types.</param>
        public LexSettings(IEnumerable<EntityType> entityTypes)
            : this(entityTypes, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LexSettings"/> class.
        /// </summary>
        /// <param name="entityTypes">Entity types.</param>
        /// <param name="importBotTemplate">Import bot template.</param>
        [JsonConstructor]
        public LexSettings(IEnumerable<EntityType> entityTypes, JObject importBotTemplate)
        {
            this.EntityTypes = entityTypes ?? Array.Empty<EntityType>();
            if (this.EntityTypes.Any(e => e == null))
            {
                throw new ArgumentException("Entity types must not be null.", nameof(entityTypes));
            }

            this.ImportBotTemplate = importBotTemplate ?? new JObject();
        }

        /// <summary>
        /// Gets the entity types.
        /// </summary>
        public IEnumerable<EntityType> EntityTypes { get; }

        /// <summary>
        /// Gets the import bot template.
        /// </summary>
        public JObject ImportBotTemplate { get; }
    }
}
