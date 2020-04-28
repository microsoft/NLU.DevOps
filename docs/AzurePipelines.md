# Configuring LUIS CI/CD with Azure Pipelines

The motivating scenario for the NLU.DevOps CLI tool was to make it simple to compose continuous integration and deployment (CI/CD) scripts for NLU scenarios. This document focuses on how to set up CI/CD for your NLU model on Azure Pipelines. We'll focus on a CI/CD pipeline for LUIS, as it is should be easy to generalize this approach for other NLU providers. We've also included a section on [Generalizing the pipeline](#generalizing-the-pipeline), to demonstrate how you can structure your files so a single Azure Pipelines definition can be used for multiple NLU providers.

## Azure DevOps Extension

We have published an Azure DevOps extension that wraps the steps below into three pipeline tasks for training, testing and deleting your NLU model. To get started, install the [NLU.DevOps extension](https://marketplace.visualstudio.com/items?itemName=NLUDevOps.nlu-devops) to your Azure DevOps organization.

See the Azure DevOps extension [overview](../extensions/overview.md) for more details.

## Continuous integration

The motivating user story for this continuous integration (CI) guide for LUIS is as follows:

> As a LUIS model developer, I need to validate that changes I've made to my NLU model have not regressed performance on a given set of test cases, so that I can ensure changes made to the model improve user experience.

This user story can be broken down into the following tasks:
- [Install the CLI tool on the host](#install-the-cli-tool-on-the-host)
- [Retrieve an ARM token](#retrieve-an-arm-token)
- [Train the LUIS model](#train-the-luis-model)
- [Query LUIS for results from test utterances](#query-luis-for-results-from-test-utterances)
- [Cleanup the LUIS model](#cleanup-the-luis-model)
- [Compare the LUIS results against the test utterances](#compare-the-luis-results-against-the-test-utterances)
- [Uninstall the CLI tool on the host](#uninstall-the-cli-tool-on-the-host)
- [Publish the test results for build failure analyis](#publish-the-test-results-for-build-failure-analysis)
- [Publish a baseline for LUIS model performance](#publish-a-baseline-for-luis-model-performance)
- [Compare the current test results with the results from master](#compare-the-current-test-results-with-the-results-from-master)

### Source Control Files
We're going to be using the same music player scenario used in the [Training an NLU model](Train.md#getting-started) and [Testing an NLU model](Test.md#getting-started) getting started sections. We assume our source control already has the following files:

```bash
> ls -1R .
./models:
settings.json
tests.json
utterances.json

./scripts:
compare.py
```

Where `utterances.json` contains training utterances, `tests.json` contains test utterances, `settings.json` contains the LUIS model configuration, and `compare.py` contains the Python script used to determine whether NLU model performance has improved in changes from a pull request.

### Install the CLI tool on the host

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

### Retrieve an ARM Token
One optional feature you may want to consider is the ability to assign an Azure LUIS resource to the LUIS app you create with the CLI tool. The primary reason for assigning an Azure resource to the LUIS app is to avoid the quota encountered when testing with the [`luisAuthoringKey`](LuisEnd.md#luisauthoringkey).

To add an Azure resource to the LUIS app you create, a valid ARM token is required. ARM tokens are generally valid for a short period of time, so you will need to configure your pipeline to retrieve a fresh ARM token for each build.

Add the following task to your Azure Pipeline:
```yaml
- task: AzureCLI@1
  displayName: 'Get ARM token for Azure'
  inputs:
    azureSubscription: $(azureSubscription)
    scriptLocation: inlineScript
    inlineScript: |
     ACCESS_TOKEN="$(az account get-access-token --query accessToken -o tsv)";
     echo "##vso[task.setvariable variable=arm_token]${ACCESS_TOKEN}"
```

You'll need to configure an Azure service principal as a [service connection](https://docs.microsoft.com/en-us/azure/devops/pipelines/library/service-endpoints?view=vsts) and set the name of the service connection to the `azureSubscription` variable.

Also be sure to set the [`azureSubscriptionId`](LuisEndpointConfiguration.md#azuresubscriptionid), [`azureResourceGroup`](LuisEndpointConfiguration.md#azureresourcegroup), [`luisPredictionResourceName`](LuisEndpointConfiguration.md#luisPredictionResourceName), and [`luisEndpointKey`](LuisEndpointConfiguration.md#luisendpointkey).

### Train the LUIS model

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
settings.json
tests.json
utterances.json

./scripts:
compare.py
```

The NLU.DevOps CLI tool will load configuration variables from `$"appsettings.{service}.json"`, so the output from using the `--save-appsettings` option will be picked up automatically by subsequent commands.

### Query LUIS for results from test utterances

Add the following task to your Azure Pipeline:
```yaml
- task: DotNetCoreCLI@2
  displayName: Test the NLU model with text
  inputs:
    command: custom
    custom: nlu
    arguments: test
      --service luis
      --utterances mdoels/tests.json
      --model-settings models/settings.json
      --output $(Agent.TempDirectory)/results.json
```

Our file system now looks like the following:
```bash
> ls -1 $AGENT_TEMPDIRECTORY
results.json
```

### Cleanup the LUIS model

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
settings.json
tests.json
utterances.json

./scripts:
compare.py
```

The `appsettings.luis.json` file has been removed, so subsequent calls to `train` for LUIS will not inadvertently use the app that was just deleted.

### Compare the LUIS results against the test utterances

Add the following task to your Azure Pipeline:
```yaml
- task: DotNetCoreCLI@2
  displayName: Compare the NLU results
  inputs:
    command: custom
    custom: nlu
    arguments: compare
      --expected models/tests.json
      --actual $(Agent.TempDirectory)/results.json
      --output-folder $(Build.ArtifactStagingDirectory)
```

We write the test results to the `$(Build.ArtifactStagingDirectory)` for a future step that will publish the test results on the `master` branch. That folder now looks like the following:
```bash
> ls -1 $BUILD_ARTIFACTSTAGINGDIRECTORY
TestResult.xml
```

The `TestResult.xml` file that is created contains the sensitivity and specifity results in NUnit format, where true positives and true negatives are passing tests and false positives and false negatives are failing tests. See [Analyzing NLU model results](Analyze.md) for more details.

### Uninstall the CLI tool on the host

Add the following task to your Azure Pipeline:
```yaml
- task: DotNetCoreCLI@2
  displayName: Uninstall dotnet-nlu
  inputs:
    command: custom
    custom: tool
    arguments: uninstall dotnet-nlu --tool-path .
```

### Publish the test results for build failure analysis

Add the following task to your Azure Pipeline:
```yaml
- task: PublishTestResults@2
  displayName: Publish test results
  inputs:
    testResultsFormat: NUnit
    testResultsFiles: $(Build.ArtifactStagingDirectory)/TestResult.xml
```

### Publish a baseline for LUIS model performance

When we start iterating on the model, we need to have results to compare against from what is currently checked into master. We can publish the NUnit test results generated from the [Compare the LUIS results against the supplied test utterances](#compare-the-luis-results-against-the-supplied-test-utterances) section for this comparison.

Add the following task to your Azure Pipeline:
```yaml
- task: PublishBuildArtifacts@1
  condition: and(succeeded(), eq(variables['Build.SourceBranch'], 'refs/heads/master'))
  displayName: Publish build artifacts
  inputs:
    pathToPublish: $(Build.ArtifactStagingDirectory)
    artifactName: drop
    artifactType: container
```

We only need to publish the test results as a build artifact for the `master` branch, so we added `condition: and(succeeded(), eq(variables['Build.SourceBranch'], 'refs/heads/master'))` to only run this step for `master` builds.

### Compare the current test results with the results from master

To ensure that we're making a net improvement in terms of NLU model performance, we want to compare the test results generated from pull requests with the latest results in master. The implementation will require multiple Azure Pipelines steps:
- Download the latest test results from master
- Use a domain-specific tool to establish whether performance has improved

#### Download the latest test results from master

In [Publish a baseline for LUIS model performance](#publish-a-baseline-for-luis-model-performance), we published the test results as a build artifact. We'll now need to download this build artifact to use for model performance comparisons in pull request builds.

Add the following task to your Azure Pipeline:
```yaml
- task: DownloadBuildArtifacts@0
  condition: and(succeeded(), eq(variables['Build.Reason'], 'PullRequest'))
  displayName: Download test results from master
  inputs:
    buildType: specific
    project: $(System.TeamProject)
    pipeline: $(Build.DefinitionName)
    buildVersionToDownload: latestFromBranch
    branchName: refs/heads/master
    downloadType: single
    artifactName: drop
    downloadPath: $(Agent.TempDirectory)
```

Our file system now looks like the following:
```bash
> ls -1 $AGENT_TEMPDIRECTORY/drop
TestResult.xml
```

#### Use a domain-specific tool to establish whether performance has improved

Whether model performance has improved is likely a domain-specific calculation. You may want to weight false negative intents more highly than false negative entities, or you may want to use an F-score to compute some harmonic mean over precision and recall. We've provided a [sample Python script](../scripts/compare.py) which takes the most naïve approach - comparing the percentage of failing tests in the pull request against the percentage of failing tests in master. The Python script will fail, and thus fail the CI build, if the percentage of failing tests is higher in the pull request than in master.

Add the following task to your Azure Pipeline:
```yaml
- task: UsePythonVersion@0
  condition: and(succeeded(), eq(variables['Build.Reason'], 'PullRequest'))
  displayName: Set correct Python version
  inputs:
    versionSpec: '>= 3.5'   

- task: PythonScript@0
  condition: and(succeeded(), eq(variables['Build.Reason'], 'PullRequest'))
  displayName: Check for performance regression
  inputs:
    scriptPath: compare.py
    arguments: $(Agent.TempDirectory)/drop/TestResult.xml $(Build.ArtifactStagingDirectory)/TestResult.xml
```

## Continuous deployment

The motivating user story for this continuous deployment (CD) guide for LUIS is as follows:

> As a LUIS model developer, I need to deploy the latest changes to my NLU model, so that I can produce a LUIS staging endpoint that I can test out with users.

This user story can be broken down into the following tasks:
- Install the CLI tool to the host
- Train the LUIS model

We can use the same tasks for installing the CLI tool and training the LUIS model as found in [Install the CLI tool on the host](#install-the-cli-tool-on-the-host) and [Train the LUIS model](#train-the-luis-model).

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
settings.lex.json
settings.luis.json
tests.json
utterances.json

./scripts:
compare.py
```

Keep in mind that the `settings.luis.json` and `settings.lex.json` must each by configured to support all entity types that occur in the `utterances.json` file.

## Putting it together

The generalized version of the tasks above have been incorporated into the [`nlu.yml`](../pipelines/nlu.yml) file we have checked into this repository.

To use this pipeline for LUIS, set `$(nlu.service)` to `luis`, or `lex` for Lex. To run this pipeline for continuous deployment from `master`, set `$(nlu.ci)` to `false`.
