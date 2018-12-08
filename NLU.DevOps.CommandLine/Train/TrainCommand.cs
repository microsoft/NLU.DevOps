// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace NLU.DevOps.CommandLine.Train
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;
    using Models;

    internal class TrainCommand : BaseCommand<TrainOptions>
    {
        public TrainCommand(TrainOptions options)
            : base(options)
        {
        }

        public override int Main()
        {
            this.RunAsync().Wait();
            return 0;
        }

        protected override INLUService CreateNLUService()
        {
            return NLUServiceFactory.Create(this.Options, this.Configuration, this.Options.SettingsPath);
        }

        private async Task RunAsync()
        {
            try
            {
                this.Log("Training NLU service...");

                if (this.Options.UtterancesPath == null && this.Options.SettingsPath == null)
                {
                    throw new InvalidOperationException("Must specify either --utterances or --extra-settings when using train.");
                }

                var trainingUtterances = this.Options.UtterancesPath != null
                    ? Read<IList<LabeledUtterance>>(this.Options.UtterancesPath)
                    : Array.Empty<LabeledUtterance>();
                await this.NLUService.TrainAsync(trainingUtterances).ConfigureAwait(false);
            }
            finally
            {
                if (this.Options.WriteConfig)
                {
                    Write($"appsettings.{this.Options.Service}.json", this.NLUService);
                }
            }
        }
    }
}
