# Lex endpoint configuration

Before using the NLU.DevOps tool, you need to supply configuration values and secrets to be able to train or test Lex. To split up the keys settings that are "safe" for check-in to source control and those that should remain secure, or remain variable for different environments, the NLU.DevOps tool splits the settings into the [`--model-settings`](Train.md#-m---model-settings) command line option, which points to a file that can be checked in to source control, and settings configured through [`Microsoft.Extensions.Configuration`](https://docs.microsoft.com/en-us/dotnet/api/microsoft.extensions.configuration?view=aspnetcore-2.1) (i.e., an `appsettings.local.json` file or environment variables). This document focuses on the latter. See [Lex bot configuration](LexModelConfiguration.md) for details about the former.

## Configuration for training

At a minimum to get started, you must supply the AWS access key and secret key with [relevant permissions](https://docs.aws.amazon.com/lex/latest/dg/access-control-managing-permissions.html) for Lex authoring, as well as the AWS region to train a model with the NLU.DevOps CLI tools.
```json
{
  "awsAccessKey": "...",
  "awsSecretKey": "...",
  "awsRegion": "us-east-1"
}
```

This will allow you to call the `train` sub-command for Lex (see [Training an NLU model](Train.md) for more details).

Options to consider for training a Lex bot include:
- [`awsAccessKey`](#awsaccesskey)
- [`awsSecretKey`](#awssecretkey)
- [`awsRegion`](#awsregion)
- [`lexBotName`](#lexbotname)
- [`lexBotAlias`](#lexbotalias)
- [`lexBotNamePrefix`](#lexbotnameprefix)

## Configuration for testing

At a minimum to get started, you must supply the AWS access key and secret key with [relevant permissions](https://docs.aws.amazon.com/lex/latest/dg/access-control-managing-permissions.html) for Lex queries, as well as the AWS region, Lex bot name, and Lex bot alias to test a model with the NLU.DevOps CLI tools.
```json
{
  "awsAccessKey": "...",
  "awsSecretKey": "...",
  "awsRegion": "us-east-1",
  "lexBotName": "...",
  "lexBotAlias": "..."
}
```

This will allow you to call the `test` sub-command for Lex (see [Testing an NLU model](Test.md) for more details).

To simplify the configuration process in continuous integration scenarios, you can use the [`--save-appsettings`](Train.md#-a---save-appsettings) option to save the Lex bot name and bot alias used to `train` in a `appsettings.lex.json` file.

Options to consider for testing a Lex bot include:
- [`awsAccessKey`](#awsaccesskey)
- [`awsSecretKey`](#awssecretkey)
- [`awsRegion`](#awsregion)
- [`lexBotName`](#lexbotname)
- [`lexBotAlias`](#lexbotalias)

## Configuration for clean

At a minimum to get started, you must supply the AWS access key and secret key with [relevant permissions](https://docs.aws.amazon.com/lex/latest/dg/access-control-managing-permissions.html) for Lex authoring, as well as the AWS region, Lex bot name, and Lex bot alias to delete a Lex bot with the NLU.DevOps CLI tools.
```json
{
  "awsAccessKey": "...",
  "awsSecretKey": "...",
  "awsRegion": "us-east-1",
  "lexBotName": "...",
  "lexBotAlias": "..."
}
```

This will allow you to call the `clean` sub-command for Lex (see [Tearing down an NLU model](Clean.md) for more details).

To simplify the configuration process in continuous integration scenarios, you can use the [`--save-appsettings`](Train.md#-a---save-appsettings) option to save the Lex bot name and bot alias used to `train` in a `appsettings.lex.json` file.

Options to consider for tearing down a Lex bot include:
- [`awsAccessKey`](#awsaccesskey)
- [`awsSecretKey`](#awssecretkey)
- [`awsRegion`](#awsregion)
- [`lexBotName`](#lexbotname)
- [`lexBotAlias`](#lexbotalias)

## App Settings Variables

### `awsAccessKey`
AWS access key.

Required for `train`, `test`, and `clean`. See [Managing Access Keys for IAM Users](https://docs.aws.amazon.com/IAM/latest/UserGuide/id_credentials_access-keys.html) for more details.

### `awsSecretKey`
AWS secret key.

Required for `train`, `test`, and `clean`. See [Managing Access Keys for IAM Users](https://docs.aws.amazon.com/IAM/latest/UserGuide/id_credentials_access-keys.html) for more details.

### `awsRegion`
AWS region.

Required for `train`, `test`, and `clean`. See [Managing Access Keys for IAM Users](https://docs.aws.amazon.com/IAM/latest/UserGuide/id_credentials_access-keys.html) for more details.

### `lexBotNamePrefix`
(Optional) The prefix for the bot name and bot alias to supply when creating and importing a new Lex bot.

Optional for `train`. This option is only used when [`lexBotName`](#lexbotname) is not provided. The prefix will be prepended to a random eight character string to generate the bot name. A common use case for the `lexBotNamePrefix` is in continuous integration scenarios, when a generated name is needed, but you may also want to have a prefix to filter on.

### `lexBotName`
(Optional) Lex bot name to use when training and testing a Lex bot. 

Required for `test` and `clean`. Optional for `train`. If not supplied for `train`, a random eight character string will be generated for the Lex bot name, potentially with the [`lexBotNamePrefix`](#lexbotnameprefix).

### `lexBotAlias`
(Optional) Lex bot alias to use when training and testing a Lex bot.

Required for `test` and `clean`. Optional for `train`. If not supplied for `train`, the [`lexBotName`](#lexbotname) will be used.
