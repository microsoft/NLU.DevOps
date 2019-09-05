// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace NLU.DevOps.Models
{
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// NLU client factory.
    /// </summary>
    public interface INLUClientFactory
    {
        /// <summary>
        /// Create the NLU training client instance.
        /// </summary>
        /// <returns>The instance.</returns>
        /// <param name="configuration">Configuration.</param>
        /// <param name="settingsPath">Path to NLU provider settings.</param>
        INLUTrainClient CreateTrainInstance(IConfiguration configuration, string settingsPath);

        /// <summary>
        /// Create the NLU testing client instance.
        /// </summary>
        /// <returns>The instance.</returns>
        /// <param name="configuration">Configuration.</param>
        /// <param name="settingsPath">Path to NLU provider settings.</param>
        INLUTestClient CreateTestInstance(IConfiguration configuration, string settingsPath);
    }
}
