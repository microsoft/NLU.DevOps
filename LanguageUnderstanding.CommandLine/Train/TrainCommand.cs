// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace LanguageUnderstanding.CommandLine.Train
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;
    using Json;
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
                this.Log("Training NLU service... ", false);
                var trainingUtterances = Serializer.Read<List<LabeledUtterance>>(this.Options.UtterancesPath);
                var entityTypes = Serializer.Read<List<EntityType>>(this.Options.EntityTypesPath);
                await this.LanguageUnderstandingService.TrainAsync(trainingUtterances, entityTypes).ConfigureAwait(false);
                this.Log("Done.");
            }
            finally
            {
                if (this.Options.WriteConfig)
                {
                    var serviceConfiguration = LanguageUnderstandingServiceFactory.GetServiceConfiguration(
                        this.Options.Service,
                        this.LanguageUnderstandingService);

                    var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"appsettings.{this.Options.Service}.json");
                    await File.WriteAllTextAsync(configPath, serviceConfiguration.ToString()).ConfigureAwait(false);
                }
            }
        }
    }
}
