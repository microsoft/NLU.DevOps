// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace LanguageUnderstanding.CommandLine.Train
{
    using System;
    using System.Collections.Generic;
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
            this.Log("Training NLU service... ", false);
            var trainingUtterances = Serialization.Read<List<LabeledUtterance>>(this.Options.UtterancesPath);
            var entityTypes = Serialization.Read<List<EntityType>>(this.Options.EntityTypesPath);
            this.LanguageUnderstandingService.TrainAsync(trainingUtterances, entityTypes).Wait();
            this.Log("Done.");

            if (this.Options.WriteConfig)
            {
                var serviceConfiguration = LanguageUnderstandingServiceFactory.GetServiceConfiguration(
                    this.Options.Service,
                    this.LanguageUnderstandingService);

                Console.Write(serviceConfiguration);
            }

            return 0;
        }
    }
}
