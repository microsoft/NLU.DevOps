// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace LanguageUnderstanding.CommandLine
{
    using System;
    using Newtonsoft.Json.Linq;

    internal abstract class ConfigCommand<TConfigOptions> : BaseCommand<TConfigOptions>
        where TConfigOptions : ConfigOptions
    {
        public ConfigCommand(TConfigOptions options)
            : base(options)
        {
        }

        protected override JToken ServiceConfiguration
        {
            get
            {
                if (!this.Options.ReadConfig)
                {
                    return base.ServiceConfiguration;
                }

                return JToken.Parse(Console.In.ReadToEnd());
            }
        }
    }
}
