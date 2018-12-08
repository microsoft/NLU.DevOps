// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace NLU.DevOps.Luis
{
    using System.Collections.Generic;
    using Microsoft.Azure.CognitiveServices.Language.LUIS.Authoring.Models;
    using Newtonsoft.Json;

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
        /// <param name="builtinEntityTypes">Builtin entity types.</param>
        public LuisSettings(IReadOnlyDictionary<string, string> builtinEntityTypes)
            : this(null, builtinEntityTypes)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LuisSettings"/> class.
        /// </summary>
        /// <param name="appTemplate">App template.</param>
        /// <param name="builtinEntityTypes">Builtin entity types.</param>
        [JsonConstructor]
        public LuisSettings(LuisApp appTemplate, IReadOnlyDictionary<string, string> builtinEntityTypes)
        {
            this.AppTemplate = appTemplate ?? new LuisApp();
            this.BuiltinEntityTypes = builtinEntityTypes ?? new Dictionary<string, string>();
        }

        /// <summary>
        /// Gets the app template.
        /// </summary>
        public LuisApp AppTemplate { get; }

        /// <summary>
        /// Gets the builtin entity types.
        /// </summary>
        public IReadOnlyDictionary<string, string> BuiltinEntityTypes { get; }
    }
}
