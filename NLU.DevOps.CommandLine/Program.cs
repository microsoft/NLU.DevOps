// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace NLU.DevOps.CommandLine
{
    using Clean;
    using Compare;
    using global::CommandLine;
    using Test;
    using Train;

    internal class Program
    {
        private static int Main(string[] args)
        {
            return Parser.Default.ParseArguments<
                CleanOptions,
                CompareOptions,
                TestOptions,
                TrainOptions
            >(args)
                .MapResult(
                    (CleanOptions options) => Run(new CleanCommand(options)),
                    (CompareOptions options) => CompareCommand.Run(options),
                    (TestOptions options) => Run(new TestCommand(options)),
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
