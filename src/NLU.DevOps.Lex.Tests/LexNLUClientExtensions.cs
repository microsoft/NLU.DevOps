// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace NLU.DevOps.Lex.Tests
{
    using System.Threading.Tasks;
    using Core;
    using Models;
    using Newtonsoft.Json.Linq;

    internal static class LexNLUClientExtensions
    {
        public static Task<ILabeledUtterance> TestAsync(this LexNLUTestClient client, string utterance)
        {
            return client.TestAsync(new JObject { { "text", utterance } });
        }
    }
}
