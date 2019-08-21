// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace NLU.DevOps.MockProvider
{
    using System.Composition;
    using Microsoft.Extensions.Configuration;
    using Models;

    [Export("mock", typeof(INLUClientFactory))]
    internal class MockNLUClientFactory : INLUClientFactory
    {
        public INLUTrainClient CreateTrainInstance(IConfiguration configuration, string settingsPath)
        {
            return new MockNLUClient(configuration["trainedUtterances"]);
        }

        public INLUTestClient CreateTestInstance(IConfiguration configuration, string settingsPath)
        {
            return new MockNLUClient(configuration["trainedUtterances"]);
        }
    }
}
