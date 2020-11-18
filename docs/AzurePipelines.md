# Configuring LUIS CI/CD with Azure Pipelines

The motivating scenario for the NLU.DevOps CLI tool was to make it simple to compose continuous integration and deployment (CI/CD) scripts for NLU scenarios. This document focuses on how to set up CI/CD for your NLU model on Azure Pipelines. We'll focus on a CI/CD pipeline for LUIS, as it is should be easy to generalize this approach for other NLU providers. We've also included a section on [Generalizing the pipeline](#generalizing-the-pipeline), to demonstrate how you can structure your files so a single Azure Pipelines definition can be used for multiple NLU providers.

## Azure DevOps Extension

We have published an Azure DevOps extension that wraps the steps below into three pipeline tasks for training, testing and deleting your NLU model. To get started, install the [NLU.DevOps extension](https://marketplace.visualstudio.com/items?itemName=NLUDevOps.nlu-devops) to your Azure DevOps organization.

See the Azure DevOps extension [overview](../extensions/overview.md) for more details.

## Continuous integration

The motivating user story for this continuous integration (CI) guide for LUIS is as follows:

> As a LUIS model developer, I need to validate that changes I've made to my NLU model have not regressed performance on a given set of test utterances, so that I can ensure changes made to the model improve user experience.

This user story can be broken down into the following tasks:
- [Install CLI tool ](#install-cli-tool)
- [Retrieve ARM token](#retrieve-arm-token)
- [Train LUIS model](#train-luis-model)
- [Get LUIS predictions for test utterances](#get-luis-predictions-for-test-utterances)
- [Cleanup LUIS model](#cleanup-luis-model)
- [Download baseline performance results](#download-baseline-performance-results)
- [Compare performance against baseline results](#compare-performance-against-baseline-results)
- [Uninstall CLI tool](#uninstall-cli-tool)
- [Publish unit test results](#publish-unit-test-results)
- [Publish model performance results](#publish-model-performance-results)

### Source Control Files
We're going to be using the same music player scenario used in the [Training an NLU model](Train.md#getting-started) and [Testing an NLU model](Test.md#getting-started) getting started sections. We assume our source control already has the following files:

```bash
> ls -1R .
./models:
compare.yml
settings.json
tests.json
utterances.json
```

Where `utterances.json` contains training utterances, `tests.json` contains test utterances, `settings.json` contains the LUIS model configuration, and `compare.yml` contains the constraints used for performance regression testing.

### Install CLI tool

Add the following task to your Azure Pipeline:
```yaml
- task: DotNetCoreCLI@2
  displayName: Install dotnet-nlu
  inputs:
    command: custom
    custom: tool
    arguments: install dotnet-nlu --tool-path $(Agent.TempDirectory)/bin

- bash: echo "##vso[task.prependpath]$(toolPath)"
  displayName: Prepend .NET Core CLI tool path
```

The `--tool-path` flag will install the CLI tool to `$(Agent.TempDirectory)/bin`. To allow the .NET Core CLI to discover the extension in future calls, we added the `task.prependpath` task to add the tool folder to the path. We'll uninstall the tool when we are finished using it in [Uninstall the CLI tool on the host](#uninstall-the-cli-tool-on-the-host).

We also have a [reusable template](https://github.com/microsoft/NLU.DevOps/blob/master/.azdo/templates/steps/install-dotnet-nlu.yml) that installs `dotnet-nlu`, bear in mind though that the template installs from a local NuGet package built from source.

#### Installing NLU providers

Some NLU providers are not available by default to the NLU.DevOps CLI. For example, if you create a [custom NLU provider for your NLU service](CliExtensions.md), you will need to [install that extension](CliExtensions.md#installing-the-extension) in the same way the NLU.DevOps CLI is installed (or supply the path via the [--include](Test.md#-i---include) option). For example, we use a mock provider for some aspects of integration. Here's how you would install the `dotnet-nlu-mock` NLU provider:

```yaml
- task: DotNetCoreCLI@2
  displayName: Install dotnet-nlu-mock
  inputs:
    command: custom
    custom: tool
    arguments: install dotnet-nlu-mock --tool-path $(Agent.TempDirectory)/bin
```

#### Install CLI tool for local access

.NET Core CLI tools can be installed globally, to a specific tool path, or [locally from a tools manifest](https://docs.microsoft.com/en-us/dotnet/core/tools/local-tools-how-to-use#create-a-manifest-file). This latter approach of using a tools manifest is useful in CI environments, as you can configure the specific packages and versions you want to take a dependency on and commit it to your source control for others to use as well. Here's how you might use the tools manifest in your pipeline:

Given a tools manifest file at relative path `.config/dotnet-tools.json`: 
```json
{
  "version": 1,
  "isRoot": true,
  "tools": {
    "dotnet-nlu": {
      "version": "0.8.0",
      "commands": [
        "dotnet-nlu"
      ]
    },
    "dotnet-nlu-mock": {
      "version": "0.8.0",
      "commands": [
        "dotnet-nlu-mock"
      ]
    }
  }
}
```

You could replace the "Install dotnet-nlu" pipeline step above with:
```yaml
- task: DotNetCoreCLI@2
  displayName: Restore .NET tools
  inputs:
    command: custom
    custom: tool
    arguments: restore
```

### Retrieve ARM Token
One optional feature you may want to consider is the ability to assign an Azure LUIS prediction resource to the LUIS app you create with the CLI tool. The primary reason for assigning an Azure resource to the LUIS app is to avoid the quota encountered when testing with the [`luisAuthoringKey`](LuisEnd.md#luisauthoringkey).

To add an Azure resource to the LUIS app you create, a valid ARM token is required. ARM tokens are generally valid for a short period of time, so you will need to configure your pipeline to retrieve a fresh ARM token for each build.

Add the following task to your Azure Pipeline:
```yaml
  - task: AzurePowerShell@4
    displayName: Get ARM token for Azure
    inputs:
      azureSubscription: $(azureSubscription)
      azurePowerShellVersion: latestVersion
      scriptType: inlineScript
      inline: |
        $azProfile = [Microsoft.Azure.Commands.Common.Authentication.Abstractions.AzureRmProfileProvider]::Instance.Profile
        $currentAzureContext = Get-AzContext
        $profileClient = New-Object Microsoft.Azure.Commands.ResourceManager.Common.RMProfileClient($azProfile)
        $token = $profileClient.AcquireAccessToken($currentAzureContext.Tenant.TenantId)
        $setVariableMessage = "##vso[task.setvariable variable=arm_token]{0}" -f $token.AccessToken 
        echo $setVariableMessage
```

You'll need to configure an Azure service principal as a [service connection](https://docs.microsoft.com/en-us/azure/devops/pipelines/library/service-endpoints?view=vsts) and set the name of the service connection to the `azureSubscription` variable.

Also be sure to set the [`azureSubscriptionId`](LuisEndpointConfiguration.md#azuresubscriptionid), [`azureResourceGroup`](LuisEndpointConfiguration.md#azureresourcegroup), [`luisPredictionResourceName`](LuisEndpointConfiguration.md#luisPredictionResourceName), and [`luisEndpointKey`](LuisEndpointConfiguration.md#luisendpointkey).

### Train LUIS model

Add the following task to your Azure Pipeline:
```yaml
- task: DotNetCoreCLI@2
  displayName: Train the NLU model
  inputs:
    command: custom
    custom: nlu
    arguments: train
      --service luis
      --utterances models/utterances.json
      --model-settings models/settings.json
      --save-appsettings
```

Our file system now looks like the following:
```bash
> ls -1R .
appsettings.luis.json

./models:
compare.yml
settings.json
tests.json
utterances.json
```

The NLU.DevOps CLI tool will load configuration variables from `$"appsettings.{service}.json"`, so the output from using the `--save-appsettings` option will be picked up automatically by subsequent commands.

### Get LUIS predictions for test utterances

Add the following task to your Azure Pipeline:
```yaml
- task: DotNetCoreCLI@2
  displayName: Test the NLU model with text
  inputs:
    command: custom
    custom: nlu
    arguments: test
      --service luis
      --utterances models/tests.json
      --model-settings models/settings.json
      --output $(Build.ArtifactStagingDirectory)/results.json
```

Our file system now looks like the following:
```bash
> ls -1 $BUILD_ARTIFACTSTAGINGDIRECTORY
results.json
```

### Cleanup LUIS model

Add the following task to your Azure Pipeline:
```yaml
- task: DotNetCoreCLI@2
  displayName: Cleanup the NLU model
  condition: always()
  inputs:
    command: custom
    custom: nlu
    arguments: clean
      --service luis
      --delete-appsettings
```

We added a condition that ensures this task is always run, so we have stronger guarantees that any resources we create will be cleaned up, even if something fails in the train or test steps.

Our file system now looks like the following:
```bash
> ls -1R .
./models:
compare.yml
settings.json
tests.json
utterances.json
```

The `appsettings.luis.json` file has been removed, so subsequent calls to `train` for LUIS will not inadvertently use the app that was just deleted.

### Download baseline performance results

In order to compare the results against a baseline performance (e.g., from the latest run on the master branch), we need to download a previous build artifact.

Add the following task to your Azure Pipeline:
```yaml
- task: DownloadBuildArtifacts@0
  displayName: Download test results from master
  inputs:
    buildType: specific
    project: $(System.TeamProject)
    pipeline: $(Build.DefinitionName)
    buildVersionToDownload: latestFromBranch
    branchName: refs/heads/master
    artifactName: nluResults
```

Assuming there was a test result to pull from master, our file system now looks like the following:
```bash
> ls -1 $SYSTEM_ARTIFACTSDIRECTORY
metadata.json
results.json
statistics.json
TestResult.xml
```

### Compare performance against baseline results

The [`compare`](Analyze.md) command allows you to set regression thresholds for intents and entity types to ensure that a metric (e.g., precision, recall or f-measure) has not regressed more than a given value. In order for this to work, you need to supply the `--baseline` option to provide a path to confusion matrix results for a previous run and a `--test-settings` option to provide a path to a configuration file for the regression thresholds. You can find more details about these options [here](Analyze.md#performance-test-mode).

Here, we will configure the `compare` command with our expected utterances from `./models/tests.json`, the actual NLU predictions in the `results.json` file produced by the `test` command, the test settings containing the performance regression thresholds in `./models/compare.yml`, the baseline confusion matrix results downloaded from a previous build, and the output folder of the comparison results.

Add the following task to your Azure Pipeline:
```yaml
- task: DotNetCoreCLI@2
  displayName: Compare the NLU results
  inputs:
    command: custom
    custom: nlu
    arguments: compare
        --expected models/tests.json
        --actual $(Build.ArtifactStagingDirectory)/results.json
        --test-settings models/compare.yml
        --baseline $(System.ArtifactsDirectory)/statistics.json
        --output-folder $(Build.ArtifactStagingDirectory)
```

We write the comparison results to the `$(Build.ArtifactStagingDirectory)` for a future step that will publish the test results on the `master` branch. That folder now looks like the following:
```bash
> ls -1 $BUILD_ARTIFACTSTAGINGDIRECTORY
results.json
metadata.json
statistics.json
TestResult.xml
```

If any of the metrics drop below the thresholds specified in `./models/compare.yml`, a non-zero exit code will be returned and, in most CI environments (including Azure Pipelines), the build will fail.

### Uninstall CLI tool

Add the following task to your Azure Pipeline (optional):
```yaml
- task: DotNetCoreCLI@2
  displayName: Uninstall dotnet-nlu
  inputs:
    command: custom
    custom: tool
    arguments: uninstall dotnet-nlu --tool-path $(Agent.TempDirectory)/bin
```

### Publish unit test results

You may publish the NLU results in unit test format, where any false positive or false negative results are rendered as failing tests, and any true positive and true negative results are rendered as passing. This is useful for getting a quick overview of unexpected results in test cases.

Add the following task to your Azure Pipeline:
```yaml
- task: PublishTestResults@2
  displayName: Publish test results
  condition: succeededOrFailed()
  inputs:
    testResultsFormat: NUnit
    testResultsFiles: $(Build.ArtifactStagingDirectory)/TestResult.xml
```

### Publish model performance results

In order for results to be downloaded for comparison in a future CI run, we need to publish them.

Add the following task to your Azure Pipeline:
```yaml
- task: PublishBuildArtifacts@1
  displayName: Publish build artifacts
  condition: succeededOrFailed()
  inputs:
    artifactName: nluResults
    artifactType: container
```

## Continuous deployment

The motivating user story for this continuous deployment (CD) guide for LUIS is as follows:

> As a LUIS model developer, I need to deploy the latest changes to my NLU model, so that I can produce a LUIS staging endpoint that I can test out with users.

This user story can be broken down into the following tasks:
- Install the CLI tool to the host
- Train the LUIS model

We can use the same tasks for installing the CLI tool and training the LUIS model as found in [Install CLI tool](#install-cli-tool) and [Train LUIS model](#train-luis-model).

If you wish to use the same Azure Pipelines YAML for continuous integration and deployment, you can add an externally configured build variable to skip the steps that are irrelevant for deployment. E.g., you could add the following condition to tasks that are not relevant to continuous deployment:
```yaml
- task: <task>
  condition: and(succeeded(), ne(variables['nlu.ci'], 'false'))
  displayName: <displayName>
    inputs:
      ...
```

Then set the variable `$(nlu.ci)` to `true` any time you wish to run a continuous deployment build.

## Generalizing the pipeline

If you plan to compare or evaluate multiple NLU providers from your repository, you may use a single YAML build definition by parameterizing the Azure Pipeline on the NLU provider name. E.g., rather than calling the `--model-settings` CLI parameter `settings.json`, you can suffix it with the NLU provider identifier, e.g., `settings.luis.json`. The YAML for `train` and other tasks could then be configured as follows:
```yaml
- task: DotNetCoreCLI@2
  displayName: Train the NLU model
  inputs:
    command: custom
    custom: nlu
    arguments: train
      --service $(nlu.service)
      --utterances utterances.json
      --model-settings settings.$(nlu.service).json
      --save-appsettings
```

You will need to set the variable `$(nlu.service)` to `luis` or whatever NLU provider identifier you wish to use for the CI/CD builds.

For example, if you wish to train and test on both LUIS and Lex, the file system would look as follows:
```bash
> ls -1R .
./models:
compare.yml
settings.lex.json
settings.luis.json
tests.json
utterances.json
```

Keep in mind that the `settings.luis.json` and `settings.lex.json` must each by configured to support all entity types that occur in the `utterances.json` file.

## Putting it together

The generalized version of the tasks above have been incorporated into [a template used by this repository](https://github.com/microsoft/NLU.DevOps/blob/master/.azdo/templates/jobs/nlu/cli.yml) file we have checked into this repository.
