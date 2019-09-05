// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace NLU.DevOps.Luis
{
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// LUIS configuration for testing.
    /// </summary>
    public sealed class TestLuisConfiguration : LuisConfiguration
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TestLuisConfiguration"/> class.
        /// </summary>
        /// <param name="configuration">Configuration.</param>
        public TestLuisConfiguration(IConfiguration configuration)
            : base(configuration)
        {
        }

        /// <inheritdoc />
        public override string AppId => this.EnsureConfigurationString(LuisAppIdConfigurationKey);
    }
}
