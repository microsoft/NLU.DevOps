// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace NLU.DevOps.DialogFlow
{
    using System;
    using System.Composition;
    using Microsoft.Extensions.Configuration;
    using Models;

    /// <summary>
    /// Dialogflow NLU client factory.
    /// </summary>
    [Export("dialogflow", typeof(INLUClientFactory))]
    public class DialogFlowNLUClientFactory : INLUClientFactory
    {
        /// <inheritdoc />
        public INLUTestClient CreateTestInstance(IConfiguration configuration, string settingsPath)
        {
            return new DialogFlowNLUTestClient(configuration ?? throw new ArgumentNullException(nameof(configuration)));
        }

        /// <inheritdoc />
        public INLUTrainClient CreateTrainInstance(IConfiguration configuration, string settingsPath)
        {
            throw new NotSupportedException("The Dialogflow NLU.DevOps package does not currently support training.");
        }
    }
}
