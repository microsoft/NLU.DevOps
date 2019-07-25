// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace NLU.DevOps.CommandLine
{
    using System;
    using System.Composition.Hosting;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.Loader;
    using McMaster.NETCore.Plugins;

    internal static class ServiceResolver
    {
        public static bool TryResolve<T>(BaseOptions options, out T instance)
        {
            string getAssemblyPath()
            {
                var assemblyName = $"dotnet-nlu-{options.Service}.dll";
                var defaultSearchPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "providers");
                var defaultAssemblyPath = Directory.GetFiles(defaultSearchPath, assemblyName, SearchOption.AllDirectories).FirstOrDefault();
                if (defaultAssemblyPath != null)
                {
                    return defaultAssemblyPath;
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

            var assembly = PluginLoader.CreateFromAssemblyFile(
                    assemblyPath,
                    PluginLoaderOptions.PreferSharedTypes)
                .LoadDefaultAssembly();
            return new ContainerConfiguration()
               .WithAssembly(assembly)
               .CreateContainer()
               .TryGetExport(options.Service, out instance);
        }
    }
}
