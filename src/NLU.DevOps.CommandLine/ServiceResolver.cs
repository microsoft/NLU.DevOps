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

    internal static class ServiceResolver
    {
        public static bool TryResolve<T>(BaseOptions options, out T instance)
        {
            var assemblies = new[]
            {
                typeof(LuisNLUServiceFactory).Assembly,
                typeof(LexNLUServiceFactory).Assembly,
            };

            var foundExport = new ContainerConfiguration()
                .WithAssemblies(assemblies)
                .CreateContainer()
                .TryGetExport(options.Service, out instance);

            if (foundExport)
            {
                return true;
            }

            return TryGetExportExternal(options, out instance);
        }

        private static bool TryGetExportExternal<T>(BaseOptions options, out T instance)
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

            var assemblyPath = options.IncludePath ?? getAssemblyPath();
            if (assemblyPath == null)
            {
                instance = default(T);
                return false;
            }

            return new ContainerConfiguration()
               .WithAssembly(Assembly.LoadFrom(assemblyPath))
               .CreateContainer()
               .TryGetExport(options.Service, out instance);
        }
    }
}
