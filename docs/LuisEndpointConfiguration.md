# LUIS endpoint configuration

Before using the NLU.DevOps tool, you need to supply subscription keys to be able to train or test LUIS. To split up the keys settings that are "safe" for check-in to source control and those that should remain secure, or remain variable for different environments, the NLU.DevOps tool splits the settings into the [`--model-settings`](Train.md#-m---model-settings) command line option, which points to a file that can be checked in to source control, and settings configured through [`Microsoft.Extensions.Configuration`](https://docs.microsoft.com/en-us/dotnet/api/microsoft.extensions.configuration?view=aspnetcore-2.1) (i.e., an `appsettings.local.json` file or environment variables). This document focuses on the latter. See [LUIS app configuration](LuisModelConfiguration.md) for details about the former.

## Configuration for training

At a minimum to get started, you must supply the [LUIS authoring key](https://docs.microsoft.com/en-us/azure/cognitive-services/luis/luis-concept-keys) and authoring region to train a model with the NLU.DevOps CLI tools.

Within an `appsettings.local.json` file use the following:

```json
{
  "luisAuthoringKey": "...",
  "luisAuthoringRegion": "westus"
}
```

If using environment variables set the values (example shown using Powershell and Bash):

```powershell
$env:luisAuthoringKey='...'
$env:luisAuthoringRegion='westus'
```

Example below uses the export command to create environment variables on a Mac:

```vim
export luisAuthoringKey='...'
export luisAuthoringRegion='westus'
```

This will allow you to call the `train` sub-command for LUIS (see [Training an NLU model](Train.md) for more details).

Options to consider for training a LUIS model include:

- [`luisAuthoringKey`](#luisauthoringkey)
- [`luisAuthoringResourceName`](#luisauthoringresourcename)
- [`luisAuthoringRegion`](#luisauthoringregion)
- [`luisAppId`](#luisappid)
- [`luisIsStaging`](#luisisstaging)
- [`luisAppName`](#luisappname)
- [`luisAppNamePrefix`](#luisappnameprefix)
- [`luisVersionId`](#luisversionid)
- [`luisVersionPrefix`](#luisversionprefix)
- [`BUILD_BUILDID`](#build_buildid)

## Configuration for testing

At a minimum to get started, you must supply a [LUIS authoring or endpoint key](https://docs.microsoft.com/en-us/azure/cognitive-services/luis/luis-concept-keys), an authoring or endpoint region, and an app ID to test a model with the NLU.DevOps CLI tools.

Within an `appsettings.local.json` file use the following:

```json
{
  "luisAuthoringKey": "...",
  "luisAuthoringRegion": "westus",
  "luisAppId": "00000000-0000-0000-0000-000000000000"
}
```

If using environment variables set the values (example shown using Powershell):

```powershell
$env:luisAuthoringKey='...'
$env:luisAuthoringRegion='westus'
$env:luisAppId='00000000-0000-0000-0000-000000000000'
```

Example below uses the export command to create environment variables on a Mac:

```vim
export luisAuthoringKey='...'
export luisAuthoringRegion='westus'
export luisAppId='00000000-0000-0000-0000-000000000000'
```

This will allow you to call the `test` sub-command for LUIS (see [Testing an NLU model](Test.md) for more details).

To simplify the configuration process in continuous integration scenarios, you can use the [`--save-appsettings`](Train.md#-a---save-appsettings) option to save the LUIS app ID generated from a previous call to `train` in a `appsettings.luis.json` file.

Also note that the LUIS authoring key has a [quota](https://docs.microsoft.com/en-us/azure/cognitive-services/luis/luis-boundaries#key-limits) when used for query (up to 1,000 text queries/month and at most 5 requests/second). As such it is recommended that you suppy a [`luisEndpointKey`](#luisendpointkey) and [`luisPredictionResourceName`](#luispredictionresourcename) or [`luisEndpointRegion`](#luisendpointregion). You may not use the [`luisAuthoringKey`](#luisauthoringkey) for testing with the [`--speech`](Test.md#--speech) option, unless you also supply a [`speechKey`](#speechkey). See [Configuring Azure resource assignment](#configuration-for-azure-resource-assignment) for details on how to avoid the quota.

Options to consider for testing a LUIS model include:

- [`luisAuthoringKey`](#luisauthoringkey)
- [`luisAuthoringResourceName`](#luisauthoringresourcename)
- [`luisAuthoringRegion`](#luisauthoringregion)
- [`luisEndpointKey`](#luisendpointkey)
- [`luisPredictionResourceName`](#luispredictionresourcename)
- [`luisEndpointRegion`](#luisendpointregion)
- [`luisAppId`](#luisappid)
- [`luisIsStaging`](#luisIsStaging)
- [`luisSlotName`](#luisslotname)
- [`luisVersionId`](#luisversionid)
- [`luisVersionPrefix`](#luisversionprefix)
- [`BUILD_BUILDID`](#build_buildid)
- [`luisDirectVersionPublish`](#luisDirectVersionPublish)
- [`speechKey`](#speechkey)
- [`speechRegion`](#speechregion)
- [`customSpeechAppId`](#customspeechappid)
- [`luisUseSpeechEndpoint`](#luisusespeechendpoint)

## Configuration for clean

At a minimum to get started, you must supply a LUIS authoring key, an authoring region, and an app ID to delete a LUIS model with the NLU.DevOps CLI tools.

Within an `appsettings.luis.json` file use the following:

```json
{
  "luisAuthoringKey": "...",
  "luisAuthoringRegion": "westus",
  "luisAppId": "00000000-0000-0000-0000-000000000000",
  "luisAppCreated": true
}
```

If using environment variables set the values (example shown using Powershell):

```powershell
$env:luisAuthoringKey='...'
$env:luisAuthoringRegion='westus'
$env:luisAppId='00000000-0000-0000-0000-000000000000'
$env:luisAppCreated='true'
```

Example below uses the export command to create environment variables on a Mac:

```vim
export luisAuthoringKey='...'
export luisAuthoringRegion='westus'
export luisAppId='00000000-0000-0000-0000-000000000000'
export luisAppCreated='true'
```

This will allow you to call the `clean` sub-command for LUIS (see [Tearing down an NLU model](Clean.md) for more details).

If you only wish to delete a specific version trained by NLU.DevOps (see [Configuration for LUIS version](#configuration-for-luis-version)), make sure the [`luisAppCreated`](#luisappcreated) configuration value is not set to `true`.

To simplify the configuration process in continuous integration scenarios, you can use the [`--save-appsettings`](Train.md#-a---save-appsettings) option to save the LUIS app ID generated from a previous call to `train` in a `appsettings.luis.json` file.

Options to consider for tearing down a LUIS model include:

- [`luisAuthoringKey`](#luisauthoringkey)
- [`luisAuthoringResourceName`](#luisauthoringresourcename)
- [`luisAuthoringRegion`](#luisauthoringregion)
- [`luisAppId`](#luisappid)
- [`luisAppCreated`](#luisappcreated)

## Configuration for LUIS version

By default, NLU.DevOps will try to publish your LUIS app using version `"0.1.1"`. There are a few configuration values that can be set to create set a specific LUIS version instead.

If you are publishing a specific version, you can simply set the [`luisVersionId`](#luisversionid) configuration value.

If you are setting up a pipeline for CI/CD in Azure DevOps, you may want to consider using the [`luisVersionPrefix`](#luisversionprefix), which will be prepended to the Azure Pipelines build ID, i.e., `$(luisVersionPrefix).$(Build.BuildId)`.

## Configuration for Azure resource assignment

As mentioned in [Configuration for testing](#configuration-for-testing), the LUIS authoring key is subject to a [quota](https://docs.microsoft.com/en-us/azure/cognitive-services/luis/luis-boundaries#key-limits).

LUIS exposes an endpoint to assign an Azure resource to the LUIS app, allowing you to test without worrying about the authoring key quota.

You will need to configure the following variables for the CLI tool to assign the Azure resource:

Within an `appsettings.luis.json` file use the following:

```json
{
  "azureSubscriptionId": "00000000-0000-0000-0000-000000000000",
  "azureResourceGroup": "...",
  "luisPredictionResourceName": "...",
  "ARM_TOKEN": "..."
}
```

If using environment variables set the values (example shown using Powershell):

```powershell
$env:azureSubscriptionId='00000000-0000-0000-0000-000000000000'
$env:azureResourceGroup='...'
$env:luisPredictionResourceName='...'
$env:ARM_TOKEN='...'
```

Example below uses the export command to create environment variables on a Mac:

```vim
azureSubscriptionId='00000000-0000-0000-0000-000000000000'
export azureResourceGroup='...'
export luisPredictionResourceName='...'
export ARM_TOKEN='...'
```

Options to consider for assigning an Azure resource during training:

- [`azureSubscriptionId`](#azuresubscriptionid)
- [`azureResourceGroup`](#azureresourcegroup)
- [`luisPredictionResourceName`](#luisPredictionResourceName)
- [`ARM_TOKEN`](#arm_token)

## App Settings Variables

### `luisAuthoringKey`
(Optional) LUIS authoring key.

Required for `train` and `clean`. May be used (to a limited extent subject to [quota](https://docs.microsoft.com/en-us/azure/cognitive-services/luis/luis-boundaries#key-limits)) for `test` from text.

### `luisAuthoringResourceName`
(Optional) Azure LUIS authoring resource name.

Required for `train` and `clean` if [`luisAuthoringRegion`](#luisauthoringregion) not specified. May be used (to a limited extent subject to [quota](https://docs.microsoft.com/en-us/azure/cognitive-services/luis/luis-boundaries#key-limits)) for `test` from text.

### `luisAuthoringRegion`
(Optional) LUIS authoring region.

Required for `train` and `clean` if [`luisAuthoringResourceName`](#luisauthoringresourcename) not specified. May be used (to a limited extent subject to [quota](https://docs.microsoft.com/en-us/azure/cognitive-services/luis/luis-boundaries#key-limits)) for `test` from text. 

### `luisEndpointKey`
(Optional) LUIS endpoint key.

Optional for `test`. If not specified, [`luisAuthoringKey`](#luisauthoringkey) will be used, which may be subject to a [quota](https://docs.microsoft.com/en-us/azure/cognitive-services/luis/luis-boundaries#key-limits).

### `luisEndpointRegion`
(Optional) LUIS endpoint region.

Optional for `test`. If not specified, one of [`luisPredictionResourceName`](#luispredictionresourcename), [`luisAuthoringResourceName`](#luisauthoringresourcename), or [`luisAuthoringRegion`](#luisauthoringregion) will be used.

### `luisAppId`
(Optional) The LUIS app ID.

Required for `test` and `clean`. Optional for `train`; when supplied, the sub-command will publish a new version to the existing LUIS app, rather than creating a new LUIS app.

### `luisIsStaging`
(Optional) Boolean signaling whether to use the LUIS staging endpoint.

Optional for `train` and `test`. When supplied for `train` as `true`, the CLI tool will publish the model to the staging endpoint. When supplied for `test` as `true`, the CLI tool will make requests against the staging endpoint.

### `luisAppCreated`
(Optional) Boolean signaling whether the LUIS app was created in the current context.

Optional for `clean`. When supplied as `true`, the CLI tool will delete the LUIS app. Otherwise, when supplied as `false`, the CLI tool will only delete the configured LUIS version.

### `luisSlotName`

(Optional) The slot name to use for the LUIS model.

Optional for `test`. When supplied, the `test` command will use the prediction API for the given `luisSlotName`. If not supplied, and [`luisDirectVersionPublish`] is not set to `true`, the `test` command will test against either the `Production` or `Staging` slot, depending on the setting for [`luisIsStaging`](#luisisstaging).

This option is only used for LUIS v3 (i.e., when using `--service luisV3`).

### `luisDirectVersionPublish`

(Optional) Flag that signals whether the LUIS model uses direct version publish.

Optional for `train` and `test`. When supplied with `true` value, the `train` command will publish the model with the `directVersionPublish` flag set to `true` and the `test` command will use the prediction API for the supplied [`luisVersionId`](#luisversionid) (or version computed based on [`luisVersionPrefix`](#luisversionprefix) and [`BUILD_BUILDID`](#build_buildid)).

This option is only used for LUIS v3 (i.e., when using `--service luisV3`).

### `luisAppNamePrefix`
(Optional) Prefix for the app name to supply when creating and importing a new LUIS app.

Optional for `train`. This option is only used when [`luisAppName`](#luisappname) is not provided. The prefix will be prepended to a random eight character string to generate the app name. A common use case for the `luisAppNamePrefix` is in continuous integration scenarios, when a generated name is needed, but you may also want to have a prefix to filter on.

### `luisAppName`
(Optional) App name to supply when creating and importing a new LUIS app.

Optional for `train`. If not supplied, a random eight character string will be generated for the app name, potentially with the [`luisAppNamePrefix`](#luisappnameprefix).

### `luisVersionId`
(Optional) Version ID to use when importing a LUIS model.

Optional for `train`. If not supplied, the default version ID is `0.1.1`, for compatibility with the default version ID used when creating a LUIS app (`0.1`). 

### `luisVersionPrefix`
(Optional) Version prefix to use when importing a model version based on the current [build ID](#build_buildid). 

Optional for `train`. The [`BUILD_BUILDID`](#build_buildid) must also be supplied, and will be appended to the version prefix. If either [`luisVersionPrefix`](#luisversionprefix) or [`BUILD_BUILDID`](#build_buildid) is not supplied, the app will attempt to use the [`luisVersionId`](#luisversionid), or the default version `0.1.1`.

### `BUILD_BUILDID`
(Optional) Suffix for LUIS version ID.

Optional for `train`. When supplied the `BUILD_BUILDID` value will be appended to the [`luisVersionPrefix`](#luisversionprefix). This is useful in continuous integration and deployment scenarios when you need to generate a new version ID to import a new LUIS model. Note that the `BUILD_BUILDID` environment variable is available by default in [Azure Pipelines](https://azure.microsoft.com/en-us/services/devops/pipelines/) builds.

### `speechKey`
(Optional) LUIS speech key.

Optional for `test`. When supplied, the `speechKey` will be used to authenticate REST calls for speech-to-text. If not supplied, [`luisEndpointKey`](#luisauthoringkey) will be used.

### `speechRegion`

(Optional) LUIS speech region.

Optional for `test`. When supplied, the `speechRegion` is used when configuring the speech endpoint. If not supplied, the [`luisEndpointRegion`](#luisendpointregion) will be used.

### `customSpeechAppId`

(Optional) LUIS custom speech app ID.

Optional for `test`. When supplied, the `customSpeechAppId` is used to configure a speech endpoint for a [CRIS](https://cris.ai) model with the given app ID. If not supplied, the generic speech endpoint will be used.

### `luisUseSpeechEndpoint`

(Optional) Flag that signals that a REST endpoint for speech should be used rather than the speech SDK.

Optional for `test`. When supplied with `true` value, speech-to-text transcription will use the Cognitive Services REST endpoint for speech as opposed to the [Speech Service SDK](https://docs.microsoft.com/en-us/azure/cognitive-services/speech-service/speech-sdk).

This option is only used for LUIS v2 (i.e., when using `--service luis`).

### `azureSubscriptionId`
(Optional) Azure subscription ID.

Optional for `train`. When supplied along with [`azureResourceGroup`](#azureresourcegroup), [`luisPredictionResourceName`](#luisPredictionResourceName), and [`ARM_TOKEN`](#arm_token), the CLI tool will assign an Azure LUIS resource to the LUIS app. See [Configuring Azure resource assignment](#configuration-for-azure-resource-assignment) for more details.

### `azureResourceGroup`
(Optional) Azure resource group containing the LUIS resource.

Optional for `train`. When supplied along with [`azureSubscriptionId`](#azuresubscriptionid), [`luisPredictionResourceName`](#luisPredictionResourceName), and [`ARM_TOKEN`](#arm_token), the CLI tool will assign an Azure LUIS resource to the LUIS app. See [Configuring Azure resource assignment](#configuration-for-azure-resource-assignment) for more details.

### `luisPredictionResourceName`
(Optional) Azure LUIS prediction resource name.

Optional for `train` and `test`. For `train`, when supplied along with [`azureSubscriptionId`](#azuresubscriptionid), [`azureResourceGroup`](#azureresourcegroup), and [`ARM_TOKEN`](#arm_token), the CLI tool will assign an Azure LUIS resource to the LUIS app. See [Configuring Azure resource assignment](#configuration-for-azure-resource-assignment) for more details. If not specified for `test`, the implementation will fallback on the [`luisEndpointRegion`](#luisendpointregion), [`luisAuthoringResourceName`](#luisauthoringresourcename), or [`luisAuthoringRegion`](#luisauthoringregion).

### `ARM_TOKEN`
(Optional) ARM token for authorizing Azure requests.

Optional for `train`. When supplied along with [`azureSubscriptionId`](#azuresubscriptionid), [`azureResourceGroup`](#azureresourcegroup), and [`luisPredictionResourceName`](#luisPredictionResourceName), the CLI tool will assign an Azure LUIS resource to the LUIS app. See [Configuring Azure resource assignment](#configuration-for-azure-resource-assignment) for more details.

## Additional Information

A detailed walkthrough of configuring for LUIS: [NLU.DevOps for LUIS](https://medium.com/@in4margaret/nlu-devops-for-luis-64cd1cb7fd6e)
