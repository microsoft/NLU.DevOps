// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace NLU.DevOps.CommandLine
{
    using System;
    using System.Composition.Hosting;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using Lex;
    using Luis;
    using Microsoft.Extensions.Configuration;
    using Models;

    internal class NLUServiceFactory
    {
        public static INLUService Create(BaseOptions options, IConfiguration configuration, string settingsPath = null)
        {
            var assemblies = new[]
            {
                typeof(LuisNLUServiceFactory).Assembly,
                typeof(LexNLUServiceFactory).Assembly,
            };

            var foundExport = new ContainerConfiguration()
                .WithAssemblies(assemblies)
                .CreateContainer()
                .TryGetExport<INLUServiceFactory>(options.Service, out var serviceFactory);

            if (!foundExport && !TryGetExportExternal(options, out serviceFactory))
            {
                throw new ArgumentException($"Invalid service type '{options.Service}'.");
            }

            return serviceFactory.CreateInstance(configuration, settingsPath);
        }

        private static bool TryGetExportExternal(BaseOptions options, out INLUServiceFactory serviceFactory)
        {
            var assemblyName = $"dotnet-nlu-{options.Service}";

            string getAssemblyPath()
            {
                var paths = new string[9];
                paths[0] = AppDomain.CurrentDomain.BaseDirectory;
                Array.Fill(paths, "..", 1, paths.Length - 2);
                paths[paths.Length - 1] = assemblyName;
                var searchRoot = Path.GetFullPath(Path.Combine(paths));
                if (!Directory.Exists(searchRoot))
                {
                    return null;
                }

                return Directory.GetFiles(searchRoot, $"{assemblyName}.dll", SearchOption.AllDirectories).FirstOrDefault();
            }

            var assemblyPath = options.ServiceAssemblyPath ?? getAssemblyPath();
            if (assemblyPath == null)
            {
                serviceFactory = null;
                return false;
            }

            return new ContainerConfiguration()
               .WithAssembly(Assembly.LoadFrom(assemblyPath))
               .CreateContainer()
               .TryGetExport(options.Service, out serviceFactory);
        }
    }
}
