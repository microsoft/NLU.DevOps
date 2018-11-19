// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace LanguageUnderstanding.CommandLine.Test
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Json;
    using LanguageUnderstanding.Models;

    internal class TestCommand : BaseCommand<TestOptions>
    {
        public TestCommand(TestOptions options)
            : base(options)
        {
        }

        public override int Main()
        {
            this.RunAsync().Wait();
            return 0;
        }

        private async Task RunAsync()
        {
            this.Log("Running tests against NLU service... ", false);
            var testUtterances = Serializer.Read<List<LabeledUtterance>>(this.Options.UtterancesPath);
            var entityTypes = Serializer.Read<List<EntityType>>(this.Options.EntityTypesPath);
            var unlabeledTests = testUtterances.Select(utterance => utterance.Text);
            var testResults = await this.LanguageUnderstandingService.TestAsync(unlabeledTests, entityTypes).ConfigureAwait(false);
            this.Log("Done.");

            var stream = this.Options.OutputPath != null
                ? File.OpenWrite(this.Options.OutputPath)
                : Console.OpenStandardOutput();

            using (stream)
            {
                Serializer.Write(stream, testResults);
            }
        }
    }
}
