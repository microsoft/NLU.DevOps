# NLU CI/CD with NLUTrain Azure Pipelines task

To train an NLU model, add the following configuration to your pipeline:
```yaml
- task: NLUTrain@0
  inputs:
    service: luis
    modelSettings: path/to/luis.json
```

You can also train a model from the [generic utterances model](GenericUtterances.md) using the [`utterances`](#utterances) input.

See the endpoint configuration documentation for [LUIS](LuisEndpointConfiguration.md) and [Lex](LexEndpointConfiguration.md) for more information on required and optional pipeline variables to set for the `NLUTrain` task.

Inputs to consider when using the `NLUClean` task:
- [`service`](#service)
- [`utterances`](#utterances)
- [`modelSettings`](#modelsettings)
- [`workingDirectory`](#workingdirectory)
- [`nupkgPath`](#nupkgpath)
- [`toolVersion`](#toolversion)

## Inputs

### `service`

Required. Specifies the NLU provider to use when deleting the model. Works for `luis`, `luisV3` and `lex`.

### `utterances`

Optional. The path to the JSON array of generic utterances to include when training the model, relative to the [`workingDirectory`](#workingdirectory).

### `modelSettings`

Optional. The path to the model settings file, relative to the [`workingDirectory`](#workingdirectory). Find more information on configuring the model settings for [LUIS](LuisModelConfiguration.md) and [Lex](LexModelConfiguration.md).

### `workingDirectory`

Optional. Specifies the working directory to use when running the `train` command. Defaults to the Azure DevOps default working directory (i.e., the root directory of the repository).

### `nupkgPath`

Optional. Specifies the folder containing a `.nupkg` for `dotnet-nlu` to install from. When not specified, `dotnet-nlu` is installed from the default NuGet repository.

### `toolVersion`

Optional. Specifies the version of `dotnet-nlu` to install from the default NuGet repository. You cannot specify both the [`nupkgPath`](#nupkgpath) input and `toolVersion`.
