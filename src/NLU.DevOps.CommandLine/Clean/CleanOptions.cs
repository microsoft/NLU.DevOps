// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace NLU.DevOps.CommandLine.Clean
{
    using global::CommandLine;

    [Verb("clean", HelpText = "Cleans up the NLU model.")]
    internal class CleanOptions : BaseOptions
    {
        [Option('a', "delete-appsettings", HelpText = "Flag to delete NLU model instance appsettings.", Required = false)]
        public bool DeleteAppSettings { get; set; }
    }
}
