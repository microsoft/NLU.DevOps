// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace LanguageUnderstanding.CommandLine
{
    using global::CommandLine;

    internal class ConfigOptions : BaseOptions
    {
        [Option('i', "read-config", HelpText = "Flag to read configuration from standard input.", Required = false)]
        public bool ReadConfig { get; set; }
    }
}
