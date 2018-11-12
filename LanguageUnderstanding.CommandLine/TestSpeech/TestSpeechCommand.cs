// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace LanguageUnderstanding.CommandLine.TestSpeech
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Json;
    using LanguageUnderstanding.Models;

    internal class TestSpeechCommand : BaseCommand<TestSpeechOptions>
    {
        public TestSpeechCommand(TestSpeechOptions options)
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
            this.Log("Running speech tests against NLU service... ", false);

            var testUtterances = Serialization.Read<List<LabeledUtteranceWithRecordingId>>(this.Options.UtterancesPath);
            if (testUtterances.Any(utterance => utterance.RecordingId == null))
            {
                throw new InvalidOperationException("Test utterances must have 'recordingID'.");
            }

            var speechFiles = testUtterances
                .Select(utterance => $"{Path.Combine(this.Options.RecordingsDirectory, utterance.RecordingId)}.wav");
            var testResults = await this.LanguageUnderstandingService.TestSpeechAsync(speechFiles);

            this.Log("Done.");

            var stream = this.Options.OutputPath != null
                ? File.OpenWrite(this.Options.OutputPath)
                : Console.OpenStandardOutput();

            using (stream)
            {
                Serialization.Write(stream, testResults);
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
