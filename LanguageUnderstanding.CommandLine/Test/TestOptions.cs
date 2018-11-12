// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace LanguageUnderstanding.CommandLine.Test
{
    using global::CommandLine;

    [Verb("test", HelpText = "Runs test cases against the NLU service.")]
    internal class TestOptions : BaseOptions
    {
        [Option('u', "utterances", HelpText = "Path to utterances.", Required = true)]
        public string UtterancesPath { get; set; }

        [Option('o', "output", HelpText = "Path to labeled results output.", Required = false)]
        public string OutputPath { get; set; }
    }
}
