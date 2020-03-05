// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace NLU.DevOps.ModelPerformance
{
    using System;
    using Core;
    using Models;
    using Newtonsoft.Json.Linq;

    internal static class LabeledUtterancePropertyExtensions
    {
        private const string UtteranceIdPropertyName = "utteranceId";
        private const string StrictEntitiesPropertyName = "strictEntities";
        private const string SpeechFilePropertyName = "speechFile";

        public static string GetUtteranceId(this LabeledUtterance instance)
        {
            return instance.GetProperty<string>(UtteranceIdPropertyName);
        }

        public static string GetSpeechFile(this LabeledUtterance instance)
        {
            return instance.GetProperty<string>(SpeechFilePropertyName);
        }

        public static string[] GetStrictEntities(this LabeledUtterance instance)
        {
            return instance.GetProperty<JArray>(StrictEntitiesPropertyName)?.ToObject<string[]>() ?? Array.Empty<string>();
        }
    }
}
