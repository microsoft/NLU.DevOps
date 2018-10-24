// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace LanguageUnderstanding.Loaders
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Models;
    using Newtonsoft.Json;

    /// <summary>
    /// Helper class for loading utterances and entities from json files.
    /// </summary>
    public static class Loader
    {
        /// <summary>
        /// Returns list of entities from file
        /// </summary>
        /// <param name="filePath">file path to entities.json</param>
        /// <returns>List of entities</returns>
        public static List<EntityType> LoadEntities(string filePath)
        {
            var jsonEntities = File.ReadAllText(filePath);
            return JsonConvert.DeserializeObject<List<EntityType>>(jsonEntities, new EntityTypesJsonConverter());
        }

        /// <summary>
        /// Returns list of utterances from file
        /// </summary>
        /// <param name="filePath">file path to utterances.json</param>
        /// <returns>List of utterances</returns>
        public static List<LabeledUtterance> LoadUtterances(string filePath)
        {
            var jsonUtterences = File.ReadAllText(filePath);
            var utterances = JsonConvert.DeserializeObject<List<LabeledUtterance>>(jsonUtterences);
            return utterances;
        }
    }
}
