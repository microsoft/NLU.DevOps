// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace NLU.DevOps.CommandLine
{
    using System.Collections.Generic;
    using System.Linq;
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
                    errors => IsVersionError(errors) ? 0 : 1);
        }

        private static int Run(ICommand command)
        {
            using (command)
            {
                return command.Main();
            }
        }

        private static bool IsVersionError(IEnumerable<Error> errors)
        {
            return errors.Count() == 1 && errors.Single().Tag == ErrorType.VersionRequestedError;
        }
    }
}
