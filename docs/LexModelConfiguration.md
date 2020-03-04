# Lex bot configuration

The `train` sub-command in the NLU.DevOps CLI tool accepts a `--model-settings` option, which allows the user to configure NLU provider-specific features such as builtin intents and entities.

The `--model-settings` for Lex allows you to specify a partial bot JSON file with identical schema to the [Lex import JSON schema](https://docs.aws.amazon.com/lex/latest/dg/import-export-format.html). You can also use this feature to import a fully-defined Lex bot exported from the portal.

## Configuring Lex

### Built-in slots

To configure an entity type as a built-in slot type from Lex, you'll need to include the slot configuration for each intent in the Lex bot template supplied to the `train` command via the [`--model-settings`](Train.md#-m---model-settings) option:
```json
{
  "resource": {
    "intents": [
      {
        "name": "PlaySong",
        "slots": [
          {
            "name": "Genre",
            "slotType": "AMAZON.Genre"
          }
        ]
      },
      {
        "name": "SearchLibrary",
        "slots": [
          {
            "name": "Genre",
            "slotType": "AMAZON.Genre"
          }
        ]
      }
    ]
  }
}
```

### Built-in intents

To configure a built-in intent for Lex, add the following to the Lex bot template JSON file supplied to the `train` command via the [`--model-settings`](Train.md#-m---model-settings) option:
```json
{
  "resource": {
    "intents": [
      {
        "name": "Skip",
        "fulfillmentActivity": {
          "type": "ReturnIntent"
        },
        "sampleUtterances": [],
        "slots": [],
        "parentIntentSignature": "AMAZON.NextIntent"
      }
    ]
  }
}
```

Note that for Lex, you can only use built-in intents if you supply at least one custom intent with at least one utterance. If you are training from generic utterances, this shouldn't be a problem.
