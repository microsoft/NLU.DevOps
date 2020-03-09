// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace NLU.DevOps.CommandLine.Benchmark
{
    using Compare;
    using global::CommandLine;

    [Verb("benchmark", HelpText = "Compute confusion matrix results.")]
    internal class BenchmarkOptions
    {
        [Option('e', "expected", HelpText = "Path to expected utterances.", Required = true)]
        public string ExpectedUtterancesPath { get; set; }

        [Option('a', "actual", HelpText = "Path to actual utterances.", Required = true)]
        public string ActualUtterancesPath { get; set; }

        [Option('b', "baseline", HelpText = "Path to baseline confusion matrix results.", Required = false)]
        public string BaselineResultsPath { get; set; }

        [Option('o', "output-folder", HelpText = "Output path for test results.", Required = false)]
        public string OutputFolder { get; set; }

        [Option('t', "test-settings", HelpText = "Path to test settings.", Required = false)]
        public string TestSettingsPath { get; set; }
    }
}
