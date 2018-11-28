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

        private async Task RunAsync()
        {
            try
            {
                this.Log("Training NLU service...");
                var trainingUtterances = Read<List<LabeledUtterance>>(this.Options.UtterancesPath);
                var entityTypes = Read<List<EntityType>>(this.Options.EntityTypesPath);
                await this.NLUService.TrainAsync(trainingUtterances, entityTypes).ConfigureAwait(false);
            }
            finally
            {
                if (this.Options.WriteConfig)
                {
                    var serviceConfiguration = NLUServiceFactory.GetServiceConfiguration(
                        this.Options.Service,
                        this.NLUService);

                    var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"appsettings.{this.Options.Service}.json");
                    await File.WriteAllTextAsync(configPath, serviceConfiguration.ToString()).ConfigureAwait(false);
                }
            }
        }
    }
}
