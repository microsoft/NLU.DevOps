// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace NLU.DevOps.ModelPerformance
{
    using System;
    using System.Collections.Generic;
    using Microsoft.Extensions.Configuration;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// A wrapper class to expose test configuration values.
    /// </summary>
    public class TestSettings
    {
        private const string IgnoreEntitiesConfigurationKey = "ignoreEntities";
        private const string StrictEntitiesConfigurationKey = "strictEntities";
        private const string TrueNegativeIntentConfigurationKey = "trueNegativeIntent";

        /// <summary>
        /// Initializes a new instance of the <see cref="TestSettings"/> class.
        /// </summary>
        /// <param name="configuration">Test cconfiguration.</param>
        public TestSettings(IConfiguration configuration)
        {
            this.Configuration = configuration;
        }

        /// <summary>
        /// Gets the default set of entities that should never return false positive results.
        /// </summary>
        /// <remarks>
        /// This is only relevant when used with the benchmark command, which runs in strict mode.
        /// </remarks>
        public IReadOnlyList<string> IgnoreEntities => this.GetArrayValue(IgnoreEntitiesConfigurationKey);

        /// <summary>
        /// Gets or sets a value indicating whether unexpected
        /// utterances should always return false positive results.
        /// </summary>
        public bool Strict { get; set; }

        /// <summary>
        /// Gets the default set of entities that should always return false positive results.
        /// </summary>
        /// <remarks>
        /// This is only relevant when used with the compare command, which is not run in strict mode.
        /// </remarks>
        public IReadOnlyList<string> StrictEntities => this.GetArrayValue(StrictEntitiesConfigurationKey);

        /// <summary>
        /// Gets the name of the intent used for true negatives.
        /// </summary>
        public string TrueNegativeIntent => this.Configuration.GetValue(TrueNegativeIntentConfigurationKey, default(string));

        private IConfiguration Configuration { get; }

        private IReadOnlyList<string> GetArrayValue(string key)
        {
            var value = this.Configuration.GetValue(key, default(string));
            try
            {
                if (value != null)
                {
                    return JToken.Parse(value).ToObject<string[]>();
                }
            }
            catch (JsonException)
            {
            }

            return Array.Empty<string>();
        }
    }
}
