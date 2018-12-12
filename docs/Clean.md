# Tearing down an NLU service

The NLU.DevOps CLI tool includes a sub-command that allows you to tear down an NLU service. This command is useful in continuous integration scenarios where you may stand up a resource only to validate model performance.

## Getting Started

Run the following command:
```bash
dotnet nlu clean -s luis
```

## Detailed Usage

### `-s, --service`
Identifier of the NLU service to run against. Try `luis` for [LUIS](https://www.luis.ai) or `lex` for [Lex](https://aws.amazon.com/lex/).

### `-c, --delete-config`

(Optional) Delete the NLU service-specific configuration overrides that were generated using the `--overwrite-config` option in a `train` command.
