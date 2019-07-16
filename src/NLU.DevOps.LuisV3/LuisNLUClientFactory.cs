// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace NLU.DevOps.Luis
{
    using System;
    using System.Composition;
    using System.IO;
    using Microsoft.Extensions.Configuration;
    using Models;
    using Newtonsoft.Json;

    /// <summary>
    /// Factory for creating <see cref="LuisNLUTrainClient"/> and <see cref="LuisNLUTestClient"/> instances.
    /// </summary>
    [Export("luisV3", typeof(INLUClientFactory))]
    public class LuisNLUClientFactory : INLUClientFactory
    {
        /// <inheritdoc/>
        public INLUTrainClient CreateTrainInstance(IConfiguration configuration, string settingsPath)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            var luisConfiguration = new LuisConfiguration(configuration);
            var luisSettings = settingsPath != null
                ? JsonConvert.DeserializeObject<LuisSettings>(File.ReadAllText(settingsPath))
                : new LuisSettings();

            var luisClient = new LuisTrainClient(luisConfiguration);
            return new LuisNLUTrainClient(
                luisConfiguration,
                luisSettings,
                luisClient);
        }

        /// <inheritdoc/>
        public INLUTestClient CreateTestInstance(IConfiguration configuration, string settingsPath)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            var luisSettings = settingsPath != null
                ? JsonConvert.DeserializeObject<LuisSettings>(File.ReadAllText(settingsPath))
                : new LuisSettings();

            var luisConfiguration = new TestLuisConfiguration(configuration);
            var luisClient = new LuisTestClient(luisConfiguration);
            return new LuisNLUTestClient(luisSettings, luisClient);
        }
    }
}
