﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace NLU.DevOps.CommandLine.Train
{
    using global::CommandLine;

    [Verb("train", HelpText = "Trains the NLU service.")]
    internal class TrainOptions : BaseOptions
    {
        [Option('u', "utterances", HelpText = "Path to utterances.", Required = true)]
        public string UtterancesPath { get; set; }

        [Option('e', "extra-settings", HelpText = "Path to NLU service settings.", Required = true)]
        public string SettingsPath { get; set; }

        [Option('c', "overwrite-config", HelpText = "Flag to (over)write NLU service configuration.", Required = false)]
        public bool WriteConfig { get; set; }
    }
}
