// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace NLU.DevOps.Lex.Tests
{
    using System.Threading.Tasks;
    using Core;
    using Models;

    internal static class LexNLUServiceExtensions
    {
        public static Task<LabeledUtterance> TestAsync(this LexNLUService service, string utterance)
        {
            return service.TestAsync(new NLUQuery(utterance));
        }
    }
}
