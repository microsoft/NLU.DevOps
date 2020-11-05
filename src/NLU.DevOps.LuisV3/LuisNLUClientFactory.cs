// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace NLU.DevOps.Luis
{
    using System;
    using System.Composition;
    using System.IO;
    using Microsoft.Azure.CognitiveServices.Language.LUIS.Authoring.Models;
    using Microsoft.Extensions.Configuration;
    using Models;
    using Newtonsoft.Json.Linq;

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
            var luisTemplate = settingsPath != null
                ? JObject.Parse(File.ReadAllText(settingsPath)).ToObject<LuisApp>()
                : new LuisApp();

            var luisClient = new LuisTrainClient(luisConfiguration);
            return new LuisNLUTrainClient(
                luisConfiguration,
                luisTemplate,
                luisClient);
        }

        /// <inheritdoc/>
        public INLUTestClient CreateTestInstance(IConfiguration configuration, string settingsPath)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            var luisConfiguration = new TestLuisConfiguration(configuration);
            var luisClient = new LuisTestClient(luisConfiguration);
            var luisBatchTestClient = new LuisBatchTestClient(luisConfiguration);
            return new LuisNLUTestClient(luisConfiguration, luisClient, luisBatchTestClient);
        }
    }
}
