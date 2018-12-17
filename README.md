# NLU.DevOps &middot; [![Build Status](https://dev.azure.com/NLUDevOps/NLU.DevOps/_apis/build/status/Microsoft.NLU.DevOps)](https://dev.azure.com/NLUDevOps/NLU.DevOps/_build/latest?definitionId=1) [![PRs Welcome](https://img.shields.io/badge/PRs-welcome-brightgreen.svg)](CONTRIBUTING.md#pull-requests)

Continuous integration and deployment of NLU models.

- [Getting Started](#getting-started)
- [Contributing](#contributing)

## Getting Started

To install the NLU.DevOps CLI tool, run:

```bash
dotnet tool install -g dotnet-nlu
```

This will install the CLI tool to your default .NET Core tools path. See the [documentation](https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet-tool-install) on the `dotnet tool install` command for more information on how to customize the installation directory or package source.

The service by default supports training and testing NLU models against [LUIS](https://www.luis.ai) and [Lex](https://aws.amazon.com/lex/).

Detailed information on the CLI tool sub-commands and arguments can be found in the [docs](docs) folder:
- [Training an NLU service](docs/Train.md)
- [Testing an NLU service](docs/Test.md)
- [Tearing down an NLU service](docs/Clean.md)
- [Analyzing NLU service results](docs/Compare.md)
- [Generic utterances model](docs/GenericUtterances.md)
- [LUIS app configuration](docs/LuisSettings.md)
- [Configuring LUIS secrets](docs/LuisSecrets.md)
- [Lex bot configuration](docs/LexSettings.md)
- [Configuring Lex secrets](docs/LexSecrets.md)
- [Configuring NLU CI/CD with Azure Pipelines](docs/AzurePipelines.md)

## Contributing

This project welcomes contributions and suggestions.  Most contributions require you to agree to a
Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us
the rights to use your contribution. For details, visit https://cla.microsoft.com.

When you submit a pull request, a CLA-bot will automatically determine whether you need to provide
a CLA and decorate the PR appropriately (e.g., label, comment). Simply follow the instructions
provided by the bot. You will only need to do this once across all repos using our CLA.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).
For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or
contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.
