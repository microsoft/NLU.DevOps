// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace NLU.DevOps.CommandLine
{
    using System;
    using System.IO;
    using System.Text;
    using Logging;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;
    using Models;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Serialization;

    internal abstract class BaseCommand<TOptions> : ICommand
        where TOptions : BaseOptions
    {
        public BaseCommand(TOptions options)
        {
            this.Options = options;
            this.LazyConfiguration = new Lazy<IConfiguration>(this.CreateConfiguration);
            this.LazyNLUTrainClient = new Lazy<INLUTrainClient>(this.CreateNLUTrainClient);
            this.LazyNLUTestClient = new Lazy<INLUTestClient>(this.CreateNLUTestClient);
            this.LazyLogger = new Lazy<ILogger>(this.CreateLogger);
        }

        protected TOptions Options { get; }

        protected IConfiguration Configuration => this.LazyConfiguration.Value;

        protected INLUTrainClient NLUTrainClient => this.LazyNLUTrainClient.Value;

        protected INLUTestClient NLUTestClient => this.LazyNLUTestClient.Value;

        protected ILogger Logger => this.LazyLogger.Value;

        private Lazy<IConfiguration> LazyConfiguration { get; }

        private Lazy<INLUTrainClient> LazyNLUTrainClient { get; }

        private Lazy<INLUTestClient> LazyNLUTestClient { get; }

        private Lazy<ILogger> LazyLogger { get; }

        public abstract int Main();

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected static T Read<T>(string path)
        {
            var serializer = JsonSerializer.CreateDefault();
            using (var jsonReader = new JsonTextReader(File.OpenText(path)))
            {
                return serializer.Deserialize<T>(jsonReader);
            }
        }

        protected static void Write(string path, object value)
        {
            using (var stream = File.Open(path, FileMode.Create))
            {
                Write(stream, value);
            }
        }

        protected static void Write(Stream stream, object value)
        {
            var serializer = JsonSerializer.CreateDefault();
            serializer.ContractResolver = new CamelCasePropertyNamesContractResolver();
            serializer.DefaultValueHandling = DefaultValueHandling.Ignore;
            serializer.Formatting = Formatting.Indented;
            using (var textWriter = new StreamWriter(stream, Encoding.UTF8, 4096, true))
            {
                serializer.Serialize(textWriter, value);
            }
        }

        protected virtual INLUTrainClient CreateNLUTrainClient()
        {
            return NLUClientFactory.CreateTrainInstance(this.Options, this.Configuration);
        }

        protected virtual INLUTestClient CreateNLUTestClient()
        {
            return NLUClientFactory.CreateTestInstance(this.Options, this.Configuration);
        }

        protected void Dispose(bool disposing)
        {
            if (disposing)
            {
                var trainClientException = default(Exception);
                if (this.LazyNLUTrainClient.IsValueCreated)
                {
                    try
                    {
                        this.NLUTrainClient.Dispose();
                    }
                    catch (Exception ex)
                    {
                        trainClientException = ex;
                    }
                }

                if (this.LazyNLUTestClient.IsValueCreated)
                {
                    try
                    {
                        this.NLUTestClient.Dispose();
                    }
                    catch (Exception ex)
                    when (trainClientException != null)
                    {
                        /* NLUTestClient exception will not be caught if no exception occurred when disposing NLUTrainClient */
                        throw new AggregateException(trainClientException, ex);
                    }
                }

                if (trainClientException != null)
                {
                    throw trainClientException;
                }
            }
        }

        protected void Log(string message)
        {
            this.LazyLogger.Value.LogInformation(message);
        }

        private IConfiguration CreateConfiguration()
        {
            return new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile($"appsettings.{this.Options.Service}.json", true)
                .AddJsonFile("appsettings.local.json", true)
                .AddEnvironmentVariables()
                .Build();
        }

        private ILogger CreateLogger()
        {
            var logLevel = this.Options.Verbose ? LogLevel.Trace : LogLevel.Information;

            if (this.Options.Quiet)
            {
                logLevel = LogLevel.Warning;
            }

            return ApplicationLogger.LoggerFactory.AddConsole(logLevel).CreateLogger(this.GetType());
        }
    }
}
