// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace NLU.DevOps.CommandLine.Train
{
    using global::CommandLine;

    [Verb("train", HelpText = "Trains the NLU model.")]
    internal class TrainOptions : BaseOptions
    {
        [Option('u', "utterances", HelpText = "Path to utterances.", Required = false)]
        public string UtterancesPath { get; set; }

        [Option('m', "model-settings", HelpText = "Path to NLU model settings.", Required = false)]
        public string SettingsPath { get; set; }

        [Option('a', "save-appsettings", HelpText = "Flag to save NLU model instance appsettings.", Required = false)]
        public bool SaveAppSettings { get; set; }
    }
}
