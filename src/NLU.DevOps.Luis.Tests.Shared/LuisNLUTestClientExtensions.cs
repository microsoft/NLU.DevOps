// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace NLU.DevOps.Luis.Tests
{
    using System.Threading.Tasks;
    using Models;
    using Newtonsoft.Json.Linq;

    internal static class LuisNLUTestClientExtensions
    {
        public static Task<LabeledUtterance> TestAsync(this LuisNLUTestClient service, string utterance)
        {
            return service.TestAsync(new JObject { { "text", utterance } });
        }
    }
}
