// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace NLU.DevOps.CommandLine.Train
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Models;
    using Newtonsoft.Json;
    using static Serializer;

    internal class TrainCommand : BaseCommand<TrainOptions>
    {
        public TrainCommand(TrainOptions options)
            : base(options)
        {
        }

        public override int Main()
        {
            this.RunAsync().GetAwaiter().GetResult();
            return 0;
        }

        protected override INLUTrainClient CreateNLUTrainClient()
        {
            return NLUClientFactory.CreateTrainInstance(this.Options, this.Configuration, this.Options.SettingsPath);
        }

        private async Task RunAsync()
        {
            try
            {
                this.Log("Training NLU model...");

                if (this.Options.UtterancesPath == null && this.Options.SettingsPath == null)
                {
                    throw new InvalidOperationException("Must specify either --utterances or --model-settings when using train.");
                }

                var trainingUtterances = this.Options.UtterancesPath != null
                    ? Read<IList<TrainLabeledUtterance>>(this.Options.UtterancesPath)
                    : Array.Empty<TrainLabeledUtterance>();
                await this.NLUTrainClient.TrainAsync(trainingUtterances).ConfigureAwait(false);
            }
            finally
            {
                if (this.Options.SaveAppSettings)
                {
                    Write($"appsettings.{this.Options.Service}.json", this.NLUTrainClient);
                }
            }
        }

        private class TrainLabeledUtterance : LabeledUtterance
        {
            public TrainLabeledUtterance(string text, string intent, IReadOnlyList<Entity> entities)
                : base(text, intent, entities)
            {
                this.AdditionalProperties = new Dictionary<string, object>();
            }

            [JsonExtensionData]
            public IDictionary<string, object> AdditionalProperties { get; }
        }
    }
}
