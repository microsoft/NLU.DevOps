// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace NLU.DevOps.CommandLine
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Clean;
    using Compare;
    using global::CommandLine;
    using Test;
    using Train;

    internal class Program
    {
        private static int Main(string[] args)
        {
            var asyncCommand = Parser.Default.ParseArguments<
                CleanOptions,
                CompareOptions,
                TestOptions,
                TrainOptions
            >(args)
                .MapResult(
                    (CleanOptions options) => RunAsync(new CleanCommand(options)),
                    (CompareOptions options) => Task.FromResult(CompareCommand.Run(options)),
                    (TestOptions options) => RunAsync(new TestCommand(options)),
                    (TrainOptions options) => RunAsync(new TrainCommand(options)),
                    errors => IsVersionError(errors) ? Task.FromResult(0) : Task.FromResult(1));

            return asyncCommand.GetAwaiter().GetResult();
        }

        private static async Task<int> RunAsync(ICommand command)
        {
            using (command)
            {
                return await command.RunAsync().ConfigureAwait(false);
            }
        }

        private static bool IsVersionError(IEnumerable<Error> errors)
        {
            return errors.SingleOrDefault()?.Tag == ErrorType.VersionRequestedError;
        }
    }
}
