// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace NLU.DevOps.CommandLine
{
    using System;
    using System.Composition.Hosting;
    using System.IO;
    using System.Linq;
    using System.Reflection;

    internal static class ServiceResolver
    {
        public static bool TryResolve<T>(BaseOptions options, out T instance)
        {
            string getAssemblyPath()
            {
                var assemblyName = $"dotnet-nlu-{options.Service}.dll";
                var defaultSearchPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "providers", options.Service, assemblyName);
                if (File.Exists(defaultSearchPath))
                {
                    return defaultSearchPath;
                }

                var paths = new string[9];
                paths[0] = AppDomain.CurrentDomain.BaseDirectory;
                Array.Fill(paths, "..", 1, paths.Length - 2);
                paths[paths.Length - 1] = assemblyName;
                var searchRoot = Path.GetFullPath(Path.Combine(paths));
                if (!Directory.Exists(searchRoot))
                {
                    return null;
                }

                return Directory.GetFiles(searchRoot, assemblyName, SearchOption.AllDirectories).FirstOrDefault();
            }

            var assemblyPath = options.IncludePath ?? getAssemblyPath();
            if (assemblyPath == null)
            {
                instance = default(T);
                return false;
            }

            ResolveEventHandler assemblyResolveHandler = (sender, args) =>
            {
                var assemblyDirectory = Path.GetDirectoryName(assemblyPath);
                var resolvedAssemblyName = args.Name.Split(",").First();
                var resolvedAssemblyPath = Path.Combine(assemblyDirectory, $"{resolvedAssemblyName}.dll");
                return Assembly.LoadFrom(resolvedAssemblyPath);
            };

            AppDomain.CurrentDomain.AssemblyResolve += assemblyResolveHandler;
            try
            {
                return new ContainerConfiguration()
                   .WithAssembly(Assembly.LoadFrom(assemblyPath))
                   .CreateContainer()
                   .TryGetExport(options.Service, out instance);
            }
            finally
            {
                AppDomain.CurrentDomain.AssemblyResolve -= assemblyResolveHandler;
            }
        }
    }
}
