// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace NLU.DevOps.CommandLine.Compare
{
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using Core;
    using ModelPerformance;
    using Newtonsoft.Json.Linq;
    using NUnitLite;
    using static Serializer;

    internal static class CompareCommand
    {
        private const string TestMetadataFileName = "metadata.json";
        private const string TestStatisticsFileName = "statistics.json";

        public static int Run(CompareOptions options)
        {
            var unitTestResults = RunNUnit(options);
            var performanceResults = RunJson(options);
            return options.UnitTestMode ? unitTestResults : performanceResults;
        }

        private static int RunJson(CompareOptions options)
        {
            var expectedUtterances = Read<List<JsonLabeledUtterance>>(options.ExpectedUtterancesPath);
            var actualUtterances = Read<List<JsonLabeledUtterance>>(options.ActualUtterancesPath);
            var testSettings = new TestSettings(options.TestSettingsPath, options.UnitTestMode);
            var compareResults = TestCaseSource.GetNLUCompareResults(expectedUtterances, actualUtterances, testSettings);
            var metadataPath = options.OutputFolder != null ? Path.Combine(options.OutputFolder, TestMetadataFileName) : TestMetadataFileName;
            var statisticsPath = options.OutputFolder != null ? Path.Combine(options.OutputFolder, TestStatisticsFileName) : TestStatisticsFileName;

            Write(metadataPath, compareResults.TestCases);
            File.WriteAllText(statisticsPath, JObject.FromObject(compareResults.Statistics).ToString());

            var baseline = options.BaselinePath != null ? Read<NLUStatistics>(options.BaselinePath) : null;
            compareResults.PrintResults(baseline);

            return 0;
        }

        private static int RunNUnit(CompareOptions options)
        {
            var parameters = CreateParameters(
                (ConfigurationConstants.ExpectedUtterancesPathKey, options.ExpectedUtterancesPath),
                (ConfigurationConstants.ActualUtterancesPathKey, options.ActualUtterancesPath),
                (ConfigurationConstants.TestSettingsPathKey, options.TestSettingsPath),
                (ConfigurationConstants.UnitTestModeKey, options.UnitTestMode.ToString(CultureInfo.InvariantCulture)));

            var arguments = new List<string> { $"-p:{parameters}" };
            if (options.OutputFolder != null)
            {
                arguments.Add($"--work={options.OutputFolder}");
            }

            return new AutoRun(typeof(ConfigurationConstants).Assembly).Execute(arguments.ToArray());
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
