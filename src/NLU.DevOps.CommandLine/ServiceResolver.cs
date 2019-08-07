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

                var paths = new string[8];
                paths[0] = AppDomain.CurrentDomain.BaseDirectory;
                Array.Fill(paths, "..", 1, paths.Length - 1);
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

            var providerDirectory = Path.GetDirectoryName(assemblyPath);
            var assemblyLoadContext = new ProviderAssemblyLoadContext(providerDirectory);
            return new ContainerConfiguration()
               .WithAssembly(assemblyLoadContext.LoadFromAssemblyPath(assemblyPath))
               .CreateContainer()
               .TryGetExport(options.Service, out instance);
        }

        private class ProviderAssemblyLoadContext : AssemblyLoadContext
        {
            public ProviderAssemblyLoadContext(string providerDirectory)
            {
                this.ProviderDirectory = providerDirectory;
            }

            private string ProviderDirectory { get; }

            protected override Assembly Load(AssemblyName assemblyName)
            {
                if (HasDefaultAssembly(assemblyName))
                {
                    return null;
                }

                var assemblyPath = Path.Combine(this.ProviderDirectory, $"{assemblyName.Name}.dll");
                if (!File.Exists(assemblyPath))
                {
                    return null;
                }

                return this.LoadFromAssemblyPath(assemblyPath);
            }

            private static bool HasDefaultAssembly(AssemblyName assemblyName)
            {
                try
                {
                    return Default.LoadFromAssemblyName(assemblyName) != null;
                }
                catch (Exception)
                {
                    // Swallow exceptions from default context
                }

                return false;
            }
        }
    }
}
