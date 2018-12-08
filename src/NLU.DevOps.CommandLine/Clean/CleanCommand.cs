// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace NLU.DevOps.CommandLine.Clean
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using Models;

    internal class CleanCommand : BaseCommand<CleanOptions>
    {
        public CleanCommand(CleanOptions options)
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
            this.Log("Cleaning NLU service...");
            await this.NLUService.CleanupAsync().ConfigureAwait(false);

            if (this.Options.DeleteConfig)
            {
                File.Delete($"appsettings.{this.Options.Service}.json");
            }
        }
    }
}
