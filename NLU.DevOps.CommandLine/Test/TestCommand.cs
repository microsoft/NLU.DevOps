// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace NLU.DevOps.CommandLine.Test
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Models;

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
            this.Log("Running tests against NLU service...");
            var testUtterances = Read<List<LabeledUtterance>>(this.Options.UtterancesPath);
            var utterances = testUtterances.Select(utterance => utterance.Text);
            var entityTypes = this.Options.EntityTypesPath != null
                ? Read<IList<EntityType>>(this.Options.EntityTypesPath)
                : Array.Empty<EntityType>();
            var testResults = await utterances.SelectAsync(utterance => this.LanguageUnderstandingService.TestAsync(utterance, entityTypes)).ConfigureAwait(false);

            var stream = this.Options.OutputPath != null
                ? File.OpenWrite(this.Options.OutputPath)
                : Console.OpenStandardOutput();

            using (stream)
            {
                Write(stream, testResults);
            }
        }
    }
}
