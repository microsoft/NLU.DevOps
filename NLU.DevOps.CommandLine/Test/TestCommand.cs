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
            this.LazyTranscriptions = new Lazy<IDictionary<string, string>>(this.LoadTranscriptions);
        }

        private Lazy<IDictionary<string, string>> LazyTranscriptions { get; }

        private IDictionary<string, string> Transcriptions => this.LazyTranscriptions.Value;

        public override int Main()
        {
            this.RunAsync().Wait();
            return 0;
        }

        protected override INLUService CreateNLUService()
        {
            return NLUServiceFactory.Create(this.Options, this.Configuration, this.Options.SettingsPath);
        }

        private static void EnsureDirectory(string filePath)
        {
            var baseDirectory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(baseDirectory) && !Directory.Exists(baseDirectory))
            {
                Directory.CreateDirectory(baseDirectory);
            }
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

            var testResults = this.Options.Speech
                ? await testUtterances.SelectAsync(this.TestSpeechAsync).ConfigureAwait(false)
                : await testUtterances.SelectAsync(this.TestAsync).ConfigureAwait(false);

            Stream getFileStream(string filePath)
            {
                EnsureDirectory(filePath);
                return File.OpenWrite(filePath);
            }

            var stream = this.Options.OutputPath != null
                ? getFileStream(this.Options.OutputPath)
                : Console.OpenStandardOutput();

            using (stream)
            {
                Write(stream, testResults);
            }

            this.SaveTranscriptions();
        }

        private Task<LabeledUtterance> TestAsync(LabeledUtterance utterance)
        {
            return this.NLUService.TestAsync(utterance.Text);
        }

        private async Task<LabeledUtterance> TestSpeechAsync(LabeledUtteranceWithRecordingId utterance)
        {
            var text = default(string);
            if (this.Transcriptions?.TryGetValue(utterance.RecordingId, out text) ?? false)
            {
                return await this.NLUService.TestAsync(text).ConfigureAwait(false);
            }

            var speechFile = Path.Combine(this.Options.RecordingsDirectory, $"{utterance.RecordingId}.wav");
            var result = await this.NLUService.TestSpeechAsync(speechFile).ConfigureAwait(false);
            this.Transcriptions?.Add(utterance.RecordingId, result.Text);
            return result;
        }

        private IDictionary<string, string> LoadTranscriptions()
        {
            var transcriptionsFile = this.Options.TranscriptionsFile;
            if (this.Options.Speech && transcriptionsFile != null)
            {
                return File.Exists(transcriptionsFile)
                    ? Read<Dictionary<string, string>>(transcriptionsFile)
                    : new Dictionary<string, string>();
            }

            return null;
        }

        private void SaveTranscriptions()
        {
            var transcriptionsFile = this.Options.TranscriptionsFile;
            if (this.Options.Speech && transcriptionsFile != null)
            {
                EnsureDirectory(transcriptionsFile);
                Write(transcriptionsFile, this.Transcriptions);
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
