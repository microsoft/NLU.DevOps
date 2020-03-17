// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace NLU.DevOps.ModelPerformance
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Microsoft.Extensions.Configuration;

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
        /// <param name="configurationPath">Test configuration path.</param>
        /// <param name="unitTestMode">Unit test mode.</param>
        public TestSettings(string configurationPath, bool unitTestMode)
            : this(CreateConfiguration(configurationPath), unitTestMode)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TestSettings"/> class.
        /// </summary>
        /// <param name="configuration">Test configuration.</param>
        /// <param name="unitTestMode">Test mode.</param>
        public TestSettings(IConfiguration configuration, bool unitTestMode)
        {
            this.Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            this.UnitTestMode = unitTestMode;
        }

        /// <summary>
        /// Gets the default set of entities that should never return false positive results.
        /// </summary>
        public IReadOnlyList<string> IgnoreEntities => this.Configuration.GetSection(IgnoreEntitiesConfigurationKey).Get<string[]>() ?? Array.Empty<string>();

        /// <summary>
        /// Gets a value indicating whether comparison should run in "unit test
        /// mode", which primarily signals that false positive entity results
        /// are not generated unless explicitly declared.
        /// </summary>
        public bool UnitTestMode { get; }

        /// <summary>
        /// Gets the default set of entities that should always return false positive results.
        /// </summary>
        public IReadOnlyList<string> StrictEntities => this.Configuration.GetSection(StrictEntitiesConfigurationKey).Get<string[]>() ?? Array.Empty<string>();

        /// <summary>
        /// Gets the name of the intent used for true negatives.
        /// </summary>
        public string TrueNegativeIntent => this.Configuration.GetValue(TrueNegativeIntentConfigurationKey, default(string));

        private IConfiguration Configuration { get; }

        private static IConfiguration CreateConfiguration(string path)
        {
            IConfigurationBuilder configurationBuilder = new ConfigurationBuilder();

            if (path != null && (path.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".yml", StringComparison.OrdinalIgnoreCase)))
            {
                configurationBuilder = configurationBuilder
                    .AddYamlFile(Path.Combine(Directory.GetCurrentDirectory(), path));
            }
            else if (path != null)
            {
                configurationBuilder = configurationBuilder
                    .AddJsonFile(Path.Combine(Directory.GetCurrentDirectory(), path));
            }

            return configurationBuilder.Build();
        }
    }
}
