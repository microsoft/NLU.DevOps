// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace NLU.DevOps.Models
{
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// NLU service factory.
    /// </summary>
    public interface INLUServiceFactory
    {
        /// <summary>
        /// Create the NLU service instance.
        /// </summary>
        /// <returns>The instance.</returns>
        /// <param name="configuration">Configuration.</param>
        INLUService CreateInstance(IConfiguration configuration);
    }
}
