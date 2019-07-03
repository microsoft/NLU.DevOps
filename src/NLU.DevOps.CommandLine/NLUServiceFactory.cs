// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace NLU.DevOps.CommandLine
{
    using System;
    using Microsoft.Extensions.Configuration;
    using Models;

    internal class NLUServiceFactory
    {
        public static INLUService Create(BaseOptions options, IConfiguration configuration, string settingsPath = null)
        {
            if (!ServiceResolver.TryResolve<INLUServiceFactory>(options, out var serviceFactory))
            {
                throw new InvalidOperationException($"Invalid service type '{options.Service}'.");
            }

            return serviceFactory.CreateInstance(configuration, settingsPath);
        }
    }
}
