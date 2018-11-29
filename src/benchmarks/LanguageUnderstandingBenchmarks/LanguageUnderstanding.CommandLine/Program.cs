// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace LanguageUnderstanding.CommandLine
{
    using Clean;
    using global::CommandLine;
    using Test;
    using TestSpeech;
    using Train;

    internal class Program
    {
        private static int Main(string[] args)
        {
            return Parser.Default.ParseArguments<
                CleanOptions,
                TestOptions,
                TestSpeechOptions,
                TrainOptions
            >(args)
                .MapResult(
                    (CleanOptions options) => Run(new CleanCommand(options)),
                    (TestOptions options) => Run(new TestCommand(options)),
                    (TestSpeechOptions options) => Run(new TestSpeechCommand(options)),
                    (TrainOptions options) => Run(new TrainCommand(options)),
                    errors => 1);
        }

        private static int Run(ICommand command)
        {
            using (command)
            {
                return command.Main();
            }
        }
    }
}
