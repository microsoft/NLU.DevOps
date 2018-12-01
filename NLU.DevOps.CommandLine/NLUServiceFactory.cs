// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace NLU.DevOps.CommandLine
{
    using System;
    using System.Composition.Hosting;
    using Microsoft.Extensions.Configuration;
    using Models;
    using NLU.DevOps.Lex;
    using NLU.DevOps.Luis;

    internal class NLUServiceFactory
    {
        public static INLUService Create(BaseOptions options, IConfiguration configuration)
        {
            var assemblies = new[]
            {
                typeof(LuisNLUServiceFactory).Assembly,
                typeof(LexNLUServiceFactory).Assembly,
            };

            INLUServiceFactory serviceFactory;
            var foundExport = new ContainerConfiguration()
                .WithAssemblies(assemblies)
                .CreateContainer()
                .TryGetExport(options.Service, out serviceFactory);

            if (!foundExport)
            {
                throw new ArgumentException($"Invalid service type '{options.Service}'.");
            }

            return serviceFactory.CreateInstance(configuration);
        }
    }
}
