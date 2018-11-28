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

        [Option('e', "entity-types", HelpText = "Path to entity type configuration.", Required = true)]
        public string EntityTypesPath { get; set; }

        [Option('o', "output", HelpText = "Path to labeled results output.", Required = false)]
        public string OutputPath { get; set; }
    }
}
