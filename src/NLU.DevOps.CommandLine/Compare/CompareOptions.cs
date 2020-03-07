// Copyright (c) Microsoft Corporation.
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

        /// <summary>
        /// Gets or sets a value indicating whether confusion matrix metadata should be included in the output.
        /// </summary>
        /// <remarks>
        /// This option will be deprecated in a future release.
        /// </remarks>
        [Option('m', "metadata", HelpText = "Return test case metadata in addition to NUnit test results.", Required = false, Hidden = true)]
        public bool Metadata { get; set; }
    }
}
