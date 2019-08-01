// Copyright (c) Microsoft Corporation. All rights reserved.
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

        [Option('l', "label", HelpText = "Label for differentiating comparison runs.", Required = false)]
        public string TestLabel { get; set; }

        [Option('m', "metadata", HelpText = "Return test case metadata as opposed to NUnit test results.", Required = false)]
        public bool Metadata { get; set; }

        [Option('t', "text", HelpText = "Run text comparison test cases.", Required = false)]
        public bool CompareText { get; set; }

        [Option('o', "output-folder", HelpText = "Output path for test results.", Required = false)]
        public string OutputFolder { get; set; }
    }
}
