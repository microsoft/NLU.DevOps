// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace NLU.DevOps.CommandLine.Test
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Core;
    using Models;
    using Newtonsoft.Json.Linq;

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
            this.Log("Running tests against NLU service...");

            var queryFactory = this.CreateNLUQueryFactory();
            var testUtterances = this.LoadUtterances(queryFactory);
            if (this.Options.Speech && testUtterances.Any(utterance => utterance.SpeechFile == null))
            {
                throw new InvalidOperationException("Test utterances must have 'speechFile' when using --speech.");
            }

            var testResults = await (this.Options.Speech
                    ? testUtterances.SelectAsync(utterance =>
                        this.TestSpeechAsync(utterance, queryFactory))
                    : testUtterances.SelectAsync(utterance =>
                        this.NLUTestClient.TestAsync(utterance.Query)))
                .ConfigureAwait(false);

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

        private async Task<LabeledUtterance> TestSpeechAsync((INLUQuery Query, string SpeechFile) utterance, INLUQueryFactory queryFactory)
        {
            var text = default(string);
            if (this.Transcriptions?.TryGetValue(utterance.SpeechFile, out text) ?? false)
            {
                var updatedQuery = queryFactory.Update(utterance.Query, text);
                return await this.NLUTestClient.TestAsync(updatedQuery).ConfigureAwait(false);
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

        private IEnumerable<(INLUQuery Query, string SpeechFile)> LoadUtterances(INLUQueryFactory queryFactory)
        {
            var queryJson = Read<JArray>(this.Options.UtterancesPath);
            foreach (var item in queryJson)
            {
                var speechFile = item.Value<string>("speechFile");
                var query = queryFactory.Create(item);
                yield return (query, speechFile);
            }
        }

        private INLUQueryFactory CreateNLUQueryFactory()
        {
            if (ServiceResolver.TryResolve<INLUQueryFactory>(this.Options, out var queryFactory))
            {
                return queryFactory;
            }

            return DefaultNLUQueryFactory.Instance;
        }

        private class LabeledUtteranceWithSpeechFile : LabeledUtterance
        {
            public LabeledUtteranceWithSpeechFile(string text, string intent, string speechFile, IReadOnlyList<Entity> entities)
                : base(text, intent, entities)
            {
                this.SpeechFile = speechFile;
            }

            public string SpeechFile { get; }
        }
    }
}
