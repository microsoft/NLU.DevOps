// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace NLU.DevOps.CommandLine.Benchmark
{
    using System.Collections.Generic;
    using System.IO;
    using Core;
    using Microsoft.Extensions.Configuration;
    using ModelPerformance;
    using Newtonsoft.Json.Linq;
    using static Serializer;

    internal static class BenchmarkCommand
    {
        private const string TestMetadataFileName = "metadata.json";
        private const string TestStatisticsFileName = "statistics.json";

        public static int Run(BenchmarkOptions options)
        {
            var expectedUtterances = Read<List<JsonLabeledUtterance>>(options.ExpectedUtterancesPath);
            var actualUtterances = Read<List<JsonLabeledUtterance>>(options.ActualUtterancesPath);
            var testSettings = CreateTestSettings(options.TestSettingsPath);
            var compareResults = TestCaseSource.GetNLUCompareResults(expectedUtterances, actualUtterances, testSettings);
            var metadataPath = options.OutputFolder != null ? Path.Combine(options.OutputFolder, TestMetadataFileName) : TestMetadataFileName;
            var statisticsPath = options.OutputFolder != null ? Path.Combine(options.OutputFolder, TestStatisticsFileName) : TestStatisticsFileName;

            Write(metadataPath, compareResults.TestCases);
            File.WriteAllText(statisticsPath, JObject.FromObject(compareResults.Statistics).ToString());
            compareResults.PrintResults();

            return 0;
        }

        private static TestSettings CreateTestSettings(string testSettingsPath)
        {
            IConfigurationBuilder configurationBuilder = new ConfigurationBuilder();

            if (testSettingsPath != null)
            {
                configurationBuilder = configurationBuilder
                    .AddJsonFile(Path.Combine(Directory.GetCurrentDirectory(), testSettingsPath));
            }

            return new TestSettings(configurationBuilder.Build())
            {
                Strict = true,
            };
        }
    }
}
