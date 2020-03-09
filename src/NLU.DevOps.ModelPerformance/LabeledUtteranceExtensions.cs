// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace NLU.DevOps.ModelPerformance
{
    using System;
    using System.Collections.Generic;
    using Core;
    using Models;
    using Newtonsoft.Json.Linq;

    internal static class LabeledUtteranceExtensions
    {
        private const string IgnoreEntitiesPropertyName = "ignoreEntities";
        private const string StrictEntitiesPropertyName = "strictEntities";
        private const string UtteranceIdPropertyName = "utteranceId";

        public static IReadOnlyList<string> GetIgnoreEntities(this LabeledUtterance utterance)
        {
            return utterance.GetProperty<JArray>(IgnoreEntitiesPropertyName)?.ToObject<string[]>() ?? Array.Empty<string>();
        }

        public static IReadOnlyList<string> GetStrictEntities(this LabeledUtterance utterance)
        {
            return utterance.GetProperty<JArray>(StrictEntitiesPropertyName)?.ToObject<string[]>() ?? Array.Empty<string>();
        }

        public static string GetUtteranceId(this LabeledUtterance utterance)
        {
            return utterance.GetProperty<string>(UtteranceIdPropertyName);
        }
    }
}
