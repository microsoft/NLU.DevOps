# Training an NLU service

The NLU.DevOps CLI tool includes a sub-command that allows you to train an NLU service from generic utterances.

## Getting Started

Run the following command:
```bash
dotnet nlu train -s luis -u utterances.json
```

The `utterances.json` argument is the path to the generic utterances file, which may look something like:
```json
[
  {
    "text": "play a song",
    "intent": "PlayMusic"
  },
  {
    "text": "play a country song",
    "intent": "PlayMusic"
  },
  {
    "text": "start playing music",
    "intent": "PlayMusic"
  },
  {
    "text": "is it cold outside",
    "intent": "None"
  },
  {
    "text": "what time is it",
    "intent": "None"
  },
  {
    "text": "call the pizza place",
    "intent": "None"
  }
]
```

See [Generic Utterances Model](GenericUtterances.md) for more information on the JSON schema for utterances.

See [LUIS Key Configuration](TODO) and [Lex Key Configuration](TODO) for more information on how to supply secrets, e.g., the authoring key, to the CLI tool.

## Detailed Usage

### `-s, --service`
Identifier of the NLU service to run against. Try `luis` for [LUIS](https://www.luis.ai) or `lex` for [Lex](https://aws.amazon.com/lex/).

### `-u, --utterances`
(Optional) Path to labeled utterances to include in the model.

### `-e, --extra-settings`
(Optional) Path to NLU service-specific settings.

E.g., run the following command:
```bash
dotnet nlu train -s luis -u utterances.json -e settings.luis.json
```

Your `utterances.json` file may contain entity labels in addition to intent labels, e.g.:
```json
[
  {
    "text": "play a song",
    "intent": "PlayMusic"
  },
  {
    "text": "play a country song",
    "intent": "PlayMusic",
    "entities": [
      {
        "entityType": "Genre",
        "matchText": "country",
        "matchIndex": 0
      }
    ]
  },
  {
    "text": "listen to jazz",
    "intent": "PlayMusic",
    "entities": [
      {
        "entityType": "Genre",
        "matchText": "jazz",
        "matchIndex": 0
      }
    ]
  },
  {
    "text": "start playing music",
    "intent": "PlayMusic"
  },
  {
    "text": "is it cold outside",
    "intent": "None"
  },
  {
    "text": "what time is it",
    "intent": "None"
  },
  {
    "text": "call the pizza place",
    "intent": "None"
  }
]
```

In this case, you must supply the `settings.luis.json` file that configures the entity types that occur in the utterances, e.g.:
```json
{
  "appTemplate": {
    "entities": [
      {
        "name": "Genre",
        "inherits": {
          "domain_name": "Music",
          "model_name": "Genre"
        }
      }
    ]
  }
}
```

The `settings.luis.json` file in this case will be merged into the generated LUIS app JSON that will be imported into the version created by the train command, so the entity type for genre will use the builtin domain entity type, `Music.Genre`.

See [LUIS App Configuration](LuisSettings.md) and [Lex App Configuration](LexSettings.md) for additional information on the kinds of settings, including entity types, that are supplied through this file.

### `-c, --overwrite-config`

(Optional) Output additional app settings for resources that were created by the train command for use in subsequent commands.

The `--overwrite-config` option is useful when you will be running test commands after training. For example, running:
```bash
dotnet nlu train -s luis -u utterances.json -c
dotnet nlu test -s luis -u tests.json
```

The train command may create a new LUIS application, and the subsequent call to `dotnet nlu test` will need to know the LUIS app ID that was created in the first call. Using the `-c` option with LUIS, for example, will output a file in the current working directory called `appsettings.luis.json`, with the following settings:
```json
{
  "luisAppId": "<guid>",
  "luisVersionId": "<versionId>"
}
```

See [Testing NLU Services](Testing.md) for more information about the test command.
