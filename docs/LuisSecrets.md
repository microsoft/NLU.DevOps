# Configuring LUIS secrets

Before using the NLU.DevOps tool, you need to supply subscription keys to be able to train or test LUIS. To split up the keys settings that are "safe" for check-in to source control and those that should remain secure, the NLU.DevOps tool splits the settings into the [`--service-settings`](Train.md#-e---service-settings) command line option, which points to a file that can be checked in to source control, and settings configured through [`Microsoft.Extensions.Configuration`](https://docs.microsoft.com/en-us/dotnet/api/microsoft.extensions.configuration?view=aspnetcore-2.1) (i.e., an `appsettings.json` file or environment variables). This document focuses on the latter. See [LUIS app configuration](LuisSettings.md) for details about the former.

## Configuring secrets for training

At a minimum to get started, you must supply the [LUIS authoring key](https://docs.microsoft.com/en-us/azure/cognitive-services/luis/luis-concept-keys) and authoring region to train a model with the NLU.DevOps CLI tools.
```json
{
  "luisAuthoringKey": "...",
  "luisAuthoringRegion": "westus"
}
```

This will allow you to call the `train` sub-command for LUIS (see [Training an NLU Service](Train.md) for more details).

Options to consider for training a LUIS model include:
- [`luisAuthoringKey`](#luisauthoringkey)
- [`luisAuthoringRegion`](#luisauthoringregion)
- [`luisAppId`](#luisappid)
- [`luisIsStaging`](#luisisstaging)
- [`luisAppName`](#luisappname)
- [`luisAppNamePrefix`](#luisappnameprefix)
- [`luisVersionId`](#luisversionid)
- [`BUILD_BUILDID`](#build_buildid)

## Configuring secrets for testing

At a minimum to get started, you must supply a [LUIS authoring or endpoint key](https://docs.microsoft.com/en-us/azure/cognitive-services/luis/luis-concept-keys), an authoring or endpoint region, and an app ID to test a model with the NLU.DevOps CLI tools.
```json
{
  "luisAuthoringKey": "...",
  "luisAuthoringRegion": "westus",
  "luisAppId": "00000000-0000-0000-0000-000000000000"
}
```

This will allow you to call the `test` sub-command for LUIS (see [Testing an NLU Service](Test.md) for more details).

To simplify the configuration process in continuous integration scenarios, you can use the [`--save-appsettings`](Train.md#-a---save-appsettings) option to save the LUIS app ID generated from a previous call to `train` in a `appsettings.luis.json` file.

Also note that the LUIS authoring key has a [quota](https://docs.microsoft.com/en-us/azure/cognitive-services/luis/luis-boundaries#key-limits) when used for query (up to 1,000 text queries/month and at most 5 requests/second). As such it is recommended that you suppy a [`luisEndpointKey`](#luisendpointkey) and [`luisEndpointRegion`](#luisendpointregion). You may not use the [`luisAuthoringKey`](#luisauthoringkey) for testing with the [`--speech`](Test.md#--speech) option, unless you also supply a [`speechKey`](#speechkey). 

Options to consider for testing a LUIS model include:
- [`luisAuthoringKey`](#luisauthoringkey)
- [`luisAuthoringRegion`](#luisauthoringregion)
- [`luisEndpointKey`](#luisendpointkey)
- [`luisEndpointRegion`](#luisendpointregion)
- [`speechKey`](#speechkey)
- [`luisAppId`](#luisappid)

## Configuring secrets for clean

At a minimum to get started, you must supply a LUIS authoring key, an authoring region, and an app ID to delete a LUIS model with the NLU.DevOps CLI tools.
```json
{
  "luisAuthoringKey": "...",
  "luisAuthoringRegion": "westus",
  "luisAppId": "00000000-0000-0000-0000-000000000000"
}
```

This will allow you to call the `clean` sub-command for LUIS (see [Tearing down an NLU Service](Clean.md) for more details).

To simplify the configuration process in continuous integration scenarios, you can use the [`--save-appsettings`](Train.md#-a---save-appsettings) option to save the LUIS app ID generated from a previous call to `train` in a `appsettings.luis.json` file.

Options to consider for tearing down a LUIS model include:
- [`luisAuthoringKey`](#luisauthoringkey)
- [`luisAuthoringRegion`](#luisauthoringregion)
- [`luisAppId`](#luisappid)

## App Settings Variables

### `luisAuthoringKey`
(Optional) LUIS authoring key.

Required for `train` and `clean`. May be used (to a limited extent subject to [quota](https://docs.microsoft.com/en-us/azure/cognitive-services/luis/luis-boundaries#key-limits)) for `test` from text.

### `luisAuthoringRegion`
(Optional) LUIS authoring region.

Required for `train` and `clean`. May be used (to a limited extent subject to [quota](https://docs.microsoft.com/en-us/azure/cognitive-services/luis/luis-boundaries#key-limits)) for `test` from text.

### `luisEndpointKey`
(Optional) LUIS endpoint key.

Required for `test` when the [`--speech`](Test.md#--speech) option is used and the [`speechKey`](#speechkey) is not provided.

### `luisEndpointRegion`
(Optional) LUIS endpoint region.

Required for `test` when the [`--speech`](Test.md#--speech) option is used and the [`speechKey`](#speechkey) is not provided.

### `speechKey`
(Optional) LUIS speech key.

Optional for `test`. When supplied, the `speechKey` will configure LUIS to make a REST call to transcribe the speech prior to sending the transcribed text to LUIS, rather than making an end-to-end call using the [Speech Service SDK](https://docs.microsoft.com/en-us/azure/cognitive-services/speech-service/speech-sdk).

### `luisAppId`
(Optional) The LUIS app ID.

Required for `test` and `clean`. Optional for `train`; when supplied, the sub-command will publish a new version to the existing LUIS app, rather than creating a new LUIS app.

### `luisIsStaging`
(Optional) Boolean signaling whether to use the LUIS staging endpoint.

Optional for `train` and `test`. When supplied for `train` as `true`, the CLI tool will publish the model to the staging endpoint. When supplied for `test` as `true`, the CLI tool will make requests against the staging endpoint.

### `luisAppNamePrefix`
(Optional) Prefix for the app name to supply when creating and importing a new LUIS app.

Optional for `train`. This option is only used when [`luisAppName`](#luisappname) is not provided. The prefix will be prepended to a random eight character string to generate the app name. A common use case for the `luisAppNamePrefix` is in continuous integration scenarios, when a generated name is needed, but you may also want to have a prefix to filter on.

### `luisAppName`
(Optional) App name to supply when creating and importing a new LUIS app.

Optional for `train`. If not supplied, a random eight character string will be generated for the app name, potentially with the [`luisAppNamePrefix`](#luisappnameprefix).

### `luisVersionId`
(Optional) Version ID to use when importing a LUIS model.

Optional for `train`. If not supplied, the default version ID is `0.1.1`, for compatibility with the default version ID used when creating a LUIS app (`0.1`). If supplied, the [`BUILD_BUILDID`](#build_buildid) will be appended to the version ID.

### `BUILD_BUILDID`
(Optional) Suffix for LUIS version ID.

Optional for `train`. When supplied the `BUILD_BUILDID` value will be appended to the [`luisVersionId`](#luisversionid) (or `0.1.1` if it's not provided). This is useful in continuous integration and deployment scenarios when you need to generate a new version ID to import a new LUIS model. Note that the `BUILD_BUILDID` environment variable is available by default in [Azure Pipelines](https://azure.microsoft.com/en-us/services/devops/pipelines/) builds.
