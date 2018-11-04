// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace LanguageUnderstanding.CommandLine.Clean
{
    using Models;

    internal class CleanCommand : ConfigCommand<CleanOptions>
    {
        public CleanCommand(CleanOptions options)
            : base(options)
        {
        }

        public override int Main()
        {
            this.Log("Cleaning NLU service... ", false);
            this.LanguageUnderstandingService.CleanupAsync().Wait();
            this.Log("Done.");
            return 0;
        }
    }
}
