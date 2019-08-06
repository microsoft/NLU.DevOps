// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace NLU.DevOps.CommandLine
{
    using System;
    using Microsoft.Extensions.Configuration;
    using Models;

    internal class NLUClientFactory
    {
        public static INLUTrainClient CreateTrainInstance(BaseOptions options, IConfiguration configuration, string settingsPath = null)
        {
            if (!ServiceResolver.TryResolve<INLUClientFactory>(options, out var serviceFactory))
            {
                throw new InvalidOperationException($"Invalid NLU provider type '{options.Service}'.");
            }

            return serviceFactory.CreateTrainInstance(configuration, settingsPath);
        }

        public static INLUTestClient CreateTestInstance(BaseOptions options, IConfiguration configuration, string settingsPath = null)
        {
            if (!ServiceResolver.TryResolve<INLUClientFactory>(options, out var serviceFactory))
            {
                throw new InvalidOperationException($"Invalid NLU provider type '{options.Service}'.");
            }

            return serviceFactory.CreateTestInstance(configuration, settingsPath);
        }
    }
}
