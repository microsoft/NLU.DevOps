// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace LanguageUnderstanding.Luis
{
    using System.Collections.Generic;
    using Newtonsoft.Json.Linq;

    internal static class JArrayExtensions
    {
        public static void AddRange(this JArray array, IEnumerable<JToken> items)
        {
            foreach (var item in items)
            {
                array.Add(item);
            }
        }
    }
}
