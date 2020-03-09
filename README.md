# NLU.DevOps &middot; [![Build Status](https://dev.azure.com/NLUDevOps/NLU.DevOps/_apis/build/status/Microsoft.NLU.DevOps)](https://dev.azure.com/NLUDevOps/NLU.DevOps/_build/latest?definitionId=1) [![PRs Welcome](https://img.shields.io/badge/PRs-welcome-brightgreen.svg)](CONTRIBUTING.md#pull-requests)

Continuous integration and deployment of NLU models.

- [Getting Started](#getting-started)
- [Contributing](#contributing)

## Getting Started

### Getting Started with the NLU.DevOps Azure DevOps extension

We have published an Azure DevOps extension that wraps the steps below into three pipeline tasks for training, testing and deleting your NLU model. To get started, install the [NLU.DevOps extension](https://marketplace.visualstudio.com/items?itemName=NLUDevOps.nlu-devops) to your Azure DevOps organization.

See the Azure DevOps extension [overview](extensions/overview.md) for more details.

Detailed information for each Azure Pipelines task can be found in the `docs` folder:

- [NLUTrain](docs/NLUTrainTask.md)
- [NLUTest](docs/NLUTestTask.md)
- [NLUClean](docs/NLUCleanTask.md)

### Getting Started with the NLU.DevOps CLI

To install the NLU.DevOps CLI tool, run:

```bash
dotnet tool install -g dotnet-nlu
```

This will install the CLI tool to your default .NET Core tools path. See the [documentation](https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet-tool-install) on the `dotnet tool install` command for more information on how to customize the installation directory or package source.

The CLI tool by default supports training and testing NLU models against [LUIS](https://www.luis.ai) and [Lex](https://aws.amazon.com/lex/).

Detailed information on the CLI tool sub-commands and arguments can be found in the `docs` folder:

- [Training an NLU model](docs/Train.md)
- [Testing an NLU model](docs/Test.md)
- [Tearing down an NLU model](docs/Clean.md)
- [Analyzing NLU model results](docs/Analyze.md)
- [Generic utterances model](docs/GenericUtterances.md)
- [Extending the generic utterance model](docs/UtteranceExtensions.md)
- [LUIS model configuration](docs/LuisModelConfiguration.md)
- [LUIS endpoint configuration](docs/LuisEndpointConfiguration.md)
- [Lex bot configuration](docs/LexModelConfiguration.md)
- [Lex endpoint configuration](docs/LexEndpointConfiguration.md)
- [Dialogflow endpoint configuration](docs/DialogflowEndpointConfiguration.md)
- [Configuring LUIS CI/CD with Azure Pipelines](docs/AzurePipelines.md)
- [Extending the CLI to new NLU providers](docs/CliExtensions.md)

## Contributing

This project welcomes contributions and suggestions.  Most contributions require you to agree to a
Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us
the rights to use your contribution. For details, visit <https://cla.microsoft.com.>

When you submit a pull request, a CLA-bot will automatically determine whether you need to provide
a CLA and decorate the PR appropriately (e.g., label, comment). Simply follow the instructions
provided by the bot. You will only need to do this once across all repos using our CLA.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).
For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or
contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.

For more information on reporting potential security vulnerabilities, see the [Security](SECURITY.md) overview.
