// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace NLU.DevOps.CommandLine.Compare
{
    using global::CommandLine;
    using Newtonsoft.Json;

    [Verb("compare", HelpText = "Compare test results.")]
    internal class CompareOptions
    {
        [Option('e', "expected", HelpText = "Path to expected utterances.", Required = true)]
        public string ExpectedUtterancesPath { get; set; }

        [Option('a', "actual", HelpText = "Path to actual utterances.", Required = true)]
        public string ActualUtterancesPath { get; set; }

        [Option('l', "label", HelpText = "Label for differentiating comparison runs.", Required = false)]
        public string TestLabel { get; set; }

        [Option('o', "outputFolder", HelpText = "Output path for test results.", Required = false)]
        public string OutputFolder { get; set; }
    }
}
