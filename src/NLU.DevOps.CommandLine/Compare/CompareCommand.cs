// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace NLU.DevOps.CommandLine.Compare
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using ModelPerformance;
    using Models;
    using NUnitLite;
    using static Serializer;

    internal static class CompareCommand
    {
        private const string TestMetadataFileName = "metadata.json";

        public static int Run(CompareOptions options)
        {
            var parameters = CreateParameters(
                (ConfigurationConstants.ExpectedUtterancesPathKey, options.ExpectedUtterancesPath),
                (ConfigurationConstants.ActualUtterancesPathKey, options.ActualUtterancesPath),
                (ConfigurationConstants.TestLabelKey, options.TestLabel));

            var arguments = new List<string> { $"-p:{parameters}" };
            if (options.OutputFolder != null)
            {
                arguments.Add($"--work={options.OutputFolder}");
            }

            if (options.Metadata)
            {
                var expectedUtterances = Read<List<LabeledUtterance>>(options.ExpectedUtterancesPath);
                var actualUtterances = Read<List<LabeledUtterance>>(options.ExpectedUtterancesPath);
                var testCases = TestCaseSource.GenerateTestCases(expectedUtterances, actualUtterances);
                var outputFile = options.OutputFolder != null ? Path.Combine(options.OutputFolder, TestMetadataFileName) : TestMetadataFileName;
                Write(outputFile, testCases);
            }
            else
            {
                new AutoRun(typeof(ConfigurationConstants).Assembly).Execute(arguments.ToArray());
            }

            // We don't care if there are any failing NUnit tests
            return 0;
        }

        private static string CreateParameters(params (string, string)[] parameters)
        {
            var filteredParameters = parameters
                .Where(p => p.Item2 != null)
                .Select(p => $"{p.Item1}={p.Item2}");

            return string.Join(';', filteredParameters);
        }
    }
}
