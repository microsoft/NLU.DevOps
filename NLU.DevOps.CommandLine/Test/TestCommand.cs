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

            if (this.Options.Speech && this.Options.RecordingsDirectory == null)
            {
                throw new InvalidOperationException("Must specify --directory when using --speech.");
            }

            var testUtterances = Read<List<LabeledUtteranceWithRecordingId>>(this.Options.UtterancesPath);
            if (this.Options.Speech && testUtterances.Any(utterance => utterance.RecordingId == null))
            {
                throw new InvalidOperationException("Test utterances must have 'recordingID' when using --speech.");
            }

            var testCases = this.Options.Speech
                ? testUtterances.Select(utterance => $"{Path.Combine(this.Options.RecordingsDirectory, utterance.RecordingId)}.wav")
                : testUtterances.Select(utterance => utterance.Text);

            var entityTypes = this.Options.EntityTypesPath != null
                ? Read<IList<EntityType>>(this.Options.EntityTypesPath)
                : Array.Empty<EntityType>();

            var testResults = this.Options.Speech
                ? await testCases.SelectAsync(testCase => this.NLUService.TestSpeechAsync(testCase, entityTypes)).ConfigureAwait(false)
                : await testCases.SelectAsync(testCase => this.NLUService.TestAsync(testCase, entityTypes)).ConfigureAwait(false);

            var stream = this.Options.OutputPath != null
                ? File.OpenWrite(this.Options.OutputPath)
                : Console.OpenStandardOutput();

            using (stream)
            {
                Write(stream, testResults);
            }
        }

        private class LabeledUtteranceWithRecordingId : LabeledUtterance
        {
            public LabeledUtteranceWithRecordingId(string text, string intent, string recordingId, IReadOnlyList<Entity> entities)
                : base(text, intent, entities)
            {
                this.RecordingId = recordingId;
            }

            public string RecordingId { get; }
        }
    }
}
