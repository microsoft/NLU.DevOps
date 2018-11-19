// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace LanguageUnderstanding.CommandLine
{
    using System;
    using System.IO;
    using Microsoft.Extensions.Configuration;
    using Models;

    internal abstract class BaseCommand<TOptions> : ICommand
        where TOptions : BaseOptions
    {
        public BaseCommand(TOptions options)
        {
            this.Options = options;
            this.LazyLanguageUnderstandingService =
                new Lazy<ILanguageUnderstandingService>(this.CreateLanguageUnderstandingService);
        }

        protected TOptions Options { get; }

        protected IConfiguration Configuration => this.LazyConfiguration.Value;

        protected ILanguageUnderstandingService LanguageUnderstandingService => this.LazyLanguageUnderstandingService.Value;

        private Lazy<IConfiguration> LazyConfiguration => new Lazy<IConfiguration>(this.CreateConfiguration);

        private Lazy<ILanguageUnderstandingService> LazyLanguageUnderstandingService { get; }

        public abstract int Main();

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected void Dispose(bool disposing)
        {
            if (disposing && this.LazyLanguageUnderstandingService.IsValueCreated)
            {
                this.LanguageUnderstandingService.Dispose();
            }
        }

        protected void Log(string message, bool newline = true)
        {
            if (!this.Options.Quiet)
            {
                if (newline)
                {
                    Console.WriteLine(message);
                }
                else
                {
                    Console.Write(message);
                }
            }
        }

        private IConfiguration CreateConfiguration()
        {
            var configurationBuilder = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory);

            if (File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.local.json")))
            {
                configurationBuilder.AddJsonFile("appsettings.local.json");
            }

            if (File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"appsettings.{this.Options.Service}.json")))
            {
                configurationBuilder.AddJsonFile($"appsettings.{this.Options.Service}.json");
            }

            return configurationBuilder
                .AddEnvironmentVariables()
                .Build();
        }

        private ILanguageUnderstandingService CreateLanguageUnderstandingService()
        {
            return LanguageUnderstandingServiceFactory.Create(this.Options.Service, this.Configuration);
        }
    }
}
