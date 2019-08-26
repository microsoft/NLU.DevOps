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

                var paths = new string[8];
                paths[0] = AppDomain.CurrentDomain.BaseDirectory;
                Array.Fill(paths, "..", 1, paths.Length - 1);
                var searchRoot = Path.GetFullPath(Path.Combine(paths));
                if (!Directory.Exists(searchRoot))
                {
                    return null;
                }

                try
                {
                    return Directory.GetFiles(searchRoot, assemblyName, SearchOption.AllDirectories).FirstOrDefault();
                }
                catch (UnauthorizedAccessException)
                {
                    return null;
                }
            }

            var includePath = options.IncludePath != null ? Path.GetFullPath(options.IncludePath) : null;
            var assemblyPath = includePath ?? getAssemblyPath();
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
