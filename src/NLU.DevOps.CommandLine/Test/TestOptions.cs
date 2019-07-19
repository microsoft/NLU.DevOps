// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace NLU.DevOps.CommandLine.Test
{
    using global::CommandLine;

    [Verb("test", HelpText = "Runs test cases against the NLU service.")]
    internal class TestOptions : BaseOptions
    {
        [Option('u', "utterances", HelpText = "Path to utterances.", Required = true)]
        public string UtterancesPath { get; set; }

        [Option('e', "service-settings", HelpText = "Path to NLU service settings.", Required = false)]
        public string SettingsPath { get; set; }

        [Option("speech", HelpText = "Test using speech files.", Required = false)]
        public bool Speech { get; set; }

        [Option('d', "speech-directory", HelpText = "Path to recordings directory.", Required = false)]
        public string SpeechFilesDirectory { get; set; }

        [Option('t', "transcriptions", HelpText = "Path to input transcriptions cache.", Required = false)]
        public string TranscriptionsFile { get; set; }

        [Option('o', "output", HelpText = "Path to labeled results output.", Required = false)]
        public string OutputPath { get; set; }

        [Option('p', "parallelism", HelpText = "Numeric value to determine the numer of parallel tests.  Default value is 3.", Required = false)]
        public int Parallelism { get; set; } = 3;
    }
}
