// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace NLU.DevOps.CommandLine.Test
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Models;
    using Newtonsoft.Json.Linq;
    using static Serializer;

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

        protected override INLUTestClient CreateNLUTestClient()
        {
            return NLUClientFactory.CreateTestInstance(this.Options, this.Configuration, this.Options.SettingsPath);
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
            this.Log("Running tests against NLU model...");

            var testUtterances = this.LoadUtterances().ToList();
            var testResults = await testUtterances.SelectAsync(
                    this.TestAsync,
                    this.Options.Parallelism,
                    this.Options.QueriesPerSecond)
                .ConfigureAwait(false);

            Stream getFileStream(string filePath)
            {
                EnsureDirectory(filePath);
                return File.Open(filePath, FileMode.Create);
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

        private Task<LabeledUtterance> TestAsync((JToken Query, string SpeechFile) utterance)
        {
            return utterance.SpeechFile != null
                ? this.TestSpeechAsync(utterance)
                : this.NLUTestClient.TestAsync(utterance.Query);
        }

        private async Task<LabeledUtterance> TestSpeechAsync((JToken Query, string SpeechFile) utterance)
        {
            var text = default(string);
            if (this.Transcriptions?.TryGetValue(utterance.SpeechFile, out text) ?? false)
            {
                utterance.Query["text"] = text;
                return await this.NLUTestClient.TestAsync(utterance.Query).ConfigureAwait(false);
            }

            var speechFile = this.Options.SpeechFilesDirectory != null
                ? Path.Combine(this.Options.SpeechFilesDirectory, utterance.SpeechFile)
                : utterance.SpeechFile;

            var result = await this.NLUTestClient.TestSpeechAsync(speechFile).ConfigureAwait(false);
            if (!string.IsNullOrEmpty(result.Text))
            {
                this.Transcriptions?.Add(utterance.SpeechFile, result.Text);
            }

            return result;
        }

        private IDictionary<string, string> LoadTranscriptions()
        {
            var transcriptionsFile = this.Options.TranscriptionsFile;
            if (transcriptionsFile != null)
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
            if (transcriptionsFile != null)
            {
                EnsureDirectory(transcriptionsFile);
                Write(transcriptionsFile, this.Transcriptions);
            }
        }

        private IEnumerable<(JToken Query, string SpeechFile)> LoadUtterances()
        {
            var queryJson = Read<JArray>(this.Options.UtterancesPath);
            foreach (var query in queryJson)
            {
                var speechFile = query.Value<string>("speechFile");
                yield return (query, speechFile);
            }
        }
    }
}
