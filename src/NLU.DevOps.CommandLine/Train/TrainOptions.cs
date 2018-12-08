// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace NLU.DevOps.CommandLine.Train
{
    using global::CommandLine;

    [Verb("train", HelpText = "Trains the NLU service.")]
    internal class TrainOptions : BaseOptions
    {
        [Option('u', "utterances", HelpText = "Path to utterances.", Required = false)]
        public string UtterancesPath { get; set; }

        [Option('e', "service-settings", HelpText = "Path to NLU service settings.", Required = false)]
        public string SettingsPath { get; set; }

        [Option('a', "save-appsettings", HelpText = "Flag to save NLU service instance appsettings.", Required = false)]
        public bool SaveAppSettings { get; set; }
    }
}
