// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace NLU.DevOps.CommandLine
{
    using System;
    using System.Composition.Hosting;
    using System.IO;
    using System.Linq;
    using McMaster.NETCore.Plugins;
    using Newtonsoft.Json.Linq;

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

                var toolVersion = FindToolsManifestVersion(options.Service);
                var paths = new string[8];
                if (toolVersion != null)
                {
                    // CLI tool is configured in .NET tool manifest.
                    paths[0] = AppDomain.CurrentDomain.BaseDirectory;
                    Array.Fill(paths, "..", 1, paths.Length - 3);
                    paths[paths.Length - 2] = $"dotnet-nlu-{options.Service}";
                    paths[paths.Length - 1] = toolVersion;
                }
                else
                {
                    // Assume CLI tool is installed globally or at specific tool path
                    paths[0] = AppDomain.CurrentDomain.BaseDirectory;
                    Array.Fill(paths, "..", 1, paths.Length - 1);
                }

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
                instance = default;
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

        private static string FindToolsManifestVersion(string service)
        {
            string getToolVersion(string toolsManifestPath)
            {
                if (File.Exists(toolsManifestPath))
                {
                    var json = JObject.Parse(File.ReadAllText(toolsManifestPath));
                    return json.SelectToken($"$.tools.dotnet-nlu-{service}.version")?.Value<string>();
                }

                return null;
            }

            var currentDirectory = Directory.GetCurrentDirectory();
            while (currentDirectory != null)
            {
                var toolsManifestPath = Path.Combine(currentDirectory, ".config", "dotnet-tools.json");
                var toolVersion = getToolVersion(toolsManifestPath);
                if (toolVersion != null)
                {
                    return toolVersion;
                }

                currentDirectory = Directory.GetParent(currentDirectory)?.FullName;
            }

            return null;
        }
    }
}
