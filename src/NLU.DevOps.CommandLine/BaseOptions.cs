// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace NLU.DevOps.CommandLine
{
    using global::CommandLine;

    internal class BaseOptions
    {
        [Option('s', "service", HelpText = "NLU service to run against. One of 'lex' or 'luis'.", Required = true)]
        public string Service { get; set; }

        [Option('q', "quiet", HelpText = "Suppress log output.", Required = false)]
        public bool Quiet { get; set; }

        [Option('v', "verbose", HelpText = "Verbose log output.", Required = false)]
        public bool Verbose { get; set; }

        [Option('i', "include", HelpText = "Path to search for NLU.DevOps.CommandLine extensions.", Required = false)]
        public string IncludePath { get; set; }
    }
}
