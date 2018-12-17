# Extending the CLI to new NLU services

By default, the NLU.DevOps CLI tool supports [LUIS](https://www.luis.ai) and [Lex](https://aws.amazon.com/lex/). You can extend CLI support to additional NLU services by implementing the `INLUService` and `INLUServiceFactory` interfaces. In this guide, we'll walk through the creation of a very simple NLU service that stores training utterances and returns them when there is an exact match on the utterance text.

## Building the extension

Before starting, decide what the service identifier will be for you NLU service implementation, as that will be used in a number of places. E.g., the service identifier for LUIS is `luis`. In this example, we'll use `demo`.

### 1. Create a new .NET Core class library:
```bash
dotnet new console dotnet-nlu-demo
```

Be sure to replace `demo` with the service identifier you plan to use for your NLU service implementation.

### 2. Add required dependencies:
```bash
cd dotnet-nlu-demo
dotnet add package NLU.DevOps.Models
dotnet add package System.Composition.AttributedModel
```

### 3. Implement `INLUService`
Open the project in [Visual Studio](https://visualstudio.microsoft.com/downloads/) or [Visual Studio Code](https://code.visualstudio.com/).

Add `DemoNLUService.cs` to your project:
```cs
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace NLU.DevOps.Demo
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Models;
    using Newtonsoft.Json;

    internal class DemoNLUService : INLUService
    {
        public DemoNLUService(string trainedUtterances)
        {
            this.Utterances = new List<LabeledUtterance>();
            if (trainedUtterances != null)
            {
                this.Utterances.AddRange(
                    JsonConvert.DeserializeObject<IEnumerable<LabeledUtterance>>(trainedUtterances));
            }
        }

        public string TrainedUtterances => JsonConvert.SerializeObject(this.Utterances);

        private List<LabeledUtterance> Utterances { get; }

        public Task TrainAsync(IEnumerable<LabeledUtterance> utterances, CancellationToken cancellationToken)
        {
            Utterances.AddRange(utterances);
            return Task.CompletedTask;
        }

        public Task<LabeledUtterance> TestAsync(string utterance, CancellationToken cancellationToken)
        {
            var matchedUtterance = Utterances.FirstOrDefault(u => u.Text == utterance);
            return Task.FromResult(matchedUtterance ?? new LabeledUtterance(null, null, null));
        }

        public Task<LabeledUtterance> TestSpeechAsync(string speechFile, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task CleanupAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public void Dispose()
        {
        }
    }
}
```

### 4. Implement `INLUServiceFactory`
Add `DemoNLUServiceFactory.cs` to you project:
```cs
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace NLU.DevOps.Demo
{
    using System.Composition;
    using Microsoft.Extensions.Configuration;
    using Models;

    [Export("demo", typeof(INLUServiceFactory))]
    public class DemoNLUServiceFactory : INLUServiceFactory
    {
        public INLUService CreateInstance(IConfiguration configuration, string settingsPath)
        {
            return new DemoNLUService(configuration["trainedUtterances"]);
        }
    }
}
```

Ensure that you set the `ExportAttribute` on the class with the service identifier you plan to use.

## Installing the extension

There are two ways to install the NLU service extension. You can specify a search root to the DLL for your service extension using the CLI tool, or you can install your project as a .NET Core tool extension on the same path that `dotnet-nlu` was installed.

### Specifying the include path

Build your .NET Core project:
```bash
dotnet build
```

When you want to run an NLU.DevOps CLI command, add the `--include` option to your build output folder. E.g.:
```bash
dotnet nlu train --service demo --utterances utterances --include ./bin
```

These commands assume you are currently in the `dotnet-nlu-demo` project folder.

### Installing via .NET Core tools

In order to install your NLU service implementation so it's always accessible to the NLU.DevOps CLI tool, you'll have to pack and install your project as a .NET Core tool extension.

To start, add configuration to your `dotnet-nlu-demo.csproj` file to instruct .NET Core to build your project as a .NET Core tool:
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>netcoreapp2.1</TargetFramework>
    <AssemblyName>dotnet-nlu-demo</AssemblyName>
    <PackAsTool>true</PackAsTool> <!-- Add this line -->
  </PropertyGroup>
  ...
</Project>
```

Create a NuGet package from your project:
```bash
dotnet pack
```

Install the NuGet package as a .NET Core tool:
```bash
dotnet tool install dotnet-nlu-demo --add-source ./bin/Debug [-g|--tool-path <path>]
```

With this option, you won't have to specify the `--include` option each time you call the NLU.DevOps CLI tool.

### Example

Try running the following commands to install and test the `demo` service we've created above:
```
dotnet tool install -g dotnet-nlu
dotnet tool install -g dotnet-nlu-demo
dotnet nlu train -s demo -u models/utterances.json -a
dotnet nlu test -s demo -u models/tests.json
dotnet nlu clean -s demo -a
```
