# NLU CI/CD with NLUClean Azure Pipelines task

For CI/CD, you often want to import, test, and then delete an NLU model. To delete the NLU model you set up for testing in a CI environment:
```yaml
- task: NLUClean@0
  inputs:
    service: luis
```

See the endpoint configuration documentation for [LUIS](LuisEndpointConfiguration.md) and [Lex](LexEndpointConfiguration.md) for more information on required and optional pipeline variables to set for the `NLUClean` task.

Inputs to consider when using the `NLUClean` task:
- [`service`](#service)
- [`includePath`](#includepath)
- [`workingDirectory`](#workingdirectory)
- [`nupkgPath`](#nupkgpath)
- [`toolVersion`](#toolversion)

## Inputs

### `service`

Specifies the NLU provider to use when deleting the model. Works for `luis`, `luisV3` and `lex`.

### `includePath`
(Optional) Path to custom NLU provider DLL. See documentation about [Specifying the include path](https://github.com/microsoft/NLU.DevOps/blob/master/docs/CliExtensions.md#specifying-the-include-path) for more details.

### `workingDirectory`

(Optional) Specifies the working directory to use when running the `clean` command. This task only works if you have previously used the [`NLUTrain` task](NLUTrainTask.md) from the same working directory. Defaults to the Azure DevOps default working directory (i.e., the root directory of the repository).

### `nupkgPath`

(Optional) Specifies the folder containing a `.nupkg` for `dotnet-nlu` to install from. When not specified, `dotnet-nlu` is installed from the default NuGet repository.

### `toolVersion`

(Optional) Specifies the version of `dotnet-nlu` to install from the default NuGet repository. You cannot specify both the [`nupkgPath`](#nupkgpath) input and `toolVersion`.
