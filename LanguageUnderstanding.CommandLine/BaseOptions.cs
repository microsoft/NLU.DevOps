// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace LanguageUnderstanding.CommandLine
{
    using global::CommandLine;

    internal class BaseOptions
    {
        [Option('s', "service", HelpText = "NLU service to run against. One of 'lex' or 'luis'.", Required = true)]
        public string Service { get; set; }

        [Option('q', HelpText = "Suppress log output.", Required = false)]
        public bool Quiet { get; set; }
    }
}
