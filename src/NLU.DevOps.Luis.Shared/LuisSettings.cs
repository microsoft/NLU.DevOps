// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace NLU.DevOps.Luis
{
    using System;
    using System.Collections.Generic;
    using Microsoft.Azure.CognitiveServices.Language.LUIS.Authoring.Models;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Luis settings.
    /// </summary>
    public class LuisSettings
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LuisSettings"/> class.
        /// </summary>
        public LuisSettings()
            : this(null, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LuisSettings"/> class.
        /// </summary>
        /// <param name="appTemplate">App template.</param>
        public LuisSettings(LuisApp appTemplate)
            : this(appTemplate, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LuisSettings"/> class.
        /// </summary>
        /// <param name="prebuiltEntityTypes">Prebuilt entity types.</param>
        public LuisSettings(IReadOnlyDictionary<string, string> prebuiltEntityTypes)
            : this(null, prebuiltEntityTypes)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LuisSettings"/> class.
        /// </summary>
        /// <param name="appTemplate">App template.</param>
        /// <param name="prebuiltEntityTypes">Prebuilt entity types.</param>
        [JsonConstructor]
        public LuisSettings(LuisApp appTemplate, IReadOnlyDictionary<string, string> prebuiltEntityTypes)
        {
            this.AppTemplate = appTemplate ?? new LuisApp();
            this.PrebuiltEntityTypes = prebuiltEntityTypes ?? new Dictionary<string, string>();
        }

        /// <summary>
        /// Gets the app template.
        /// </summary>
        public LuisApp AppTemplate { get; }

        /// <summary>
        /// Gets the prebuilt entity type mappings.
        /// </summary>
        public IReadOnlyDictionary<string, string> PrebuiltEntityTypes { get; }

        /// <summary>
        /// Converts a <see cref="JObject"/> to <see cref="LuisSettings"/>.
        /// </summary>
        /// <param name="settings">Settings JSON.</param>
        /// <returns>A <see cref="LuisSettings"/> instance.</returns>
        public static LuisSettings FromJson(JObject settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            if (settings.ContainsKey("appTemplate") || settings.ContainsKey("prebuiltEntityTypes"))
            {
                return settings.ToObject<LuisSettings>();
            }

            return new LuisSettings(settings.ToObject<LuisApp>());
        }
    }
}
