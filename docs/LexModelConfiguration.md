# Lex bot configuration

The `train` and `test` sub-commands in the NLU.DevOps CLI tool both accept an `--model-settings` parameter, which allows the user to configure entity types, as well as other NLU provider-specific features such as builtin intents.

The `--model-settings` for Lex is a JSON object with two properties,  `importBotTemplate` and `slots`. The former allows you to specify a partial Lex import JSON with identical schema to the [Lex import JSON schema](https://docs.aws.amazon.com/lex/latest/dg/import-export-format.html). The latter refers entity type configurations for any slots that show up in training utterances.

## Configuring Lex slots

### Custom slots

To configure an entity type with a custom slot on Lex, add the following to the `importBotTemplate` property of your Lex settings JSON (i.e., the [`--model-settings`](Train.md#-m---model-settings) file supplied to the `train` command):
```json
{
  "slots": [
    {
      "name": "Genre",
      "slotType": "CustomGenre"
    }
  ],
  "importBotTemplate": {
    "resource": {
      "slotTypes": [
        {
          "name": "CustomGenre",
          "enumerationValues": [
            {
              "value": "hip hop"
            },
            {
              "value": "country"
            },
            {
              "value": "jazz"
            }
          ],
          "valueSelectionStrategy": "ORIGINAL_VALUE"
        }
      ]
    }
  }
}
```

Any intents that include utterances with the `Genre` entity will include a slot configured with the `CustomGenre` slot type.

Note that in the case of `Genre`, we needed to remap the slot type to a name that did not conflict with the built-in slot type name for genre. For entity type names that do not conflict with Lex built-in slots, this remapping will not be required.

### Built-in slots

To configure an entity type as a built-in slot type from Lex, add the following to the `slots` property of your Lex settings JSON (i.e., the [`--model-settings`](Train.md#-m---model-settings) file supplied to the `train` command):
```json
{
  "slots": [
    {
      "name": "Genre",
      "slotType": "AMAZON.Genre"
    }
  ]
}
```

Any intents that include utterances with the `Genre` entity will include a slot configured with the `AMAZON.Genre` slot type.

## Configuring builtin intents

To configure a built-in intent for Lex, add the following to the `importBotTemplate` property of your Lex settings JSON (i.e., the [`--model-settings`](Train.md#-m---model-settings) file supplied to the `train` command):
```json
{
  "importBotTemplate": {
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
}
```

Note that for Lex, you can only use built-in intents if you supply at least one custom intent with at least one utterance. If you are training from generic utterances, this shouldn't be a problem.

## Schema

### `slots`
(Optional) The slot configurations that will be used in each intent with a relevant entity.

### `importBotTemplate`
(Optional) The partial Lex import JSON that will be merged into the model before importing.  

Note, if you plan to train and test with entities, you must supply entity type configurations through this property. See [Configuring Lex slots](#configuring-lex-slots) for more information.
