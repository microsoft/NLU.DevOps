// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace NLU.DevOps.CommandLine.Clean
{
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
            this.Log("Cleaning NLU model...");
            await this.NLUTrainClient.CleanupAsync().ConfigureAwait(false);

            if (this.Options.DeleteAppSettings)
            {
                File.Delete($"appsettings.{this.Options.Service}.json");
            }
        }
    }
}
