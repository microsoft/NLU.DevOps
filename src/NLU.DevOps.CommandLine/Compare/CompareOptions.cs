﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace NLU.DevOps.CommandLine.Compare
{
    using global::CommandLine;

    [Verb("compare", HelpText = "Compare test results.")]
    internal class CompareOptions
    {
        [Option('e', "expected", HelpText = "Path to expected utterances.", Required = true)]
        public string ExpectedUtterancesPath { get; set; }

        [Option('a', "actual", HelpText = "Path to actual utterances.", Required = true)]
        public string ActualUtterancesPath { get; set; }

        [Option('o', "output-folder", HelpText = "Output path for test results.", Required = false)]
        public string OutputFolder { get; set; }

        [Option('t', "test-settings", HelpText = "Path to test settings.", Required = false)]
        public string TestSettingsPath { get; set; }
    }
}
