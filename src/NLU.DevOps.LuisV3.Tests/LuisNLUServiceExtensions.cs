// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace NLU.DevOps.Luis.Tests
{
    using System.Threading.Tasks;
    using Models;

    internal static class LuisNLUServiceExtensions
    {
        public static Task<LabeledUtterance> TestAsync(this LuisNLUService service, string utterance)
        {
            return service.TestAsync(new LuisNLUQuery(utterance));
        }
    }
}
