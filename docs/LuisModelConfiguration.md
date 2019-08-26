# LUIS model configuration

The `train` and `test` sub-commands in the NLU.DevOps CLI tool both accept an `--model-settings` parameter, which allows the user to configure entity types, as well as other NLU provider-specific features such as builtin intents and phrase lists.

The `--model-settings` for LUIS is either an exported LUIS model or a JSON object with two properties,  `appTemplate` and `prebuiltEntityTypes`. The latter allows you to specify a partial LUIS JSON model in `appTemplate` with identical schema to the [LUIS import JSON schema](https://westus.dev.cognitive.microsoft.com/docs/services/5890b47c39e2bb17b84a55ff/operations/5890b47c39e2bb052c5b9c31). You can also supply mappings from the from user supplied names for entity types to LUIS prebuilt entity type names in the `prebuiltEntityTypes` property.

## Configuring LUIS entities

### Closed lists

To configure an entity type as a closed list from LUIS, add the following to the `appTemplate` property of your LUIS settings JSON (i.e., the [`--model-settings`](Train.md#-m---model-settings) file supplied to the `train` command):
```json
{
  "appTemplate": {
    "closedLists": [
      {
        "name": "Genre",
        "subLists": [
          {
            "canonicalForm": "country",
            "list": []
          },
          {
            "canonicalForm": "hip hop",
            "list": []
          },
          {
            "canonicalForm": "jazz",
            "list": []
          }
        ]
      }
    ]
  }
}
```

### Prebuilt domain entities

To configure an entity type as a prebuilt domain entity from LUIS, add the following to the `appTemplate` property of your LUIS settings JSON (i.e., the [`--model-settings`](Train.md#-m---model-settings) file supplied to the `train` command):
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

### Entities with phrase lists

An alternative to closed lists for specifying entity types is to create a "simple" entity (i.e., an item in the `entities` property of the LUIS app JSON), and configure a phrase list that captures an non-exhaustive list of valid matches. To achieve this, add the following to the `appTemplate` property of your LUIS settings JSON (i.e., the [`--model-settings`](Train.md#-m---model-settings) file supplied to the `train` command):
```json
{
  "appTemplate": {
    "entities": [
      {
        "name": "Genre"
      }
    ],
    "model_features": [
      {
        "name": "genre",
        "mode": false,
        "words": "hip hop,jazz,pop,country",
        "activated": true
      }
    ]
  }
}
```

### Configuring prebuilt entities

Prebuilt entity types in LUIS are handled in a slightly different way than other entity types. Specifically, users cannot supply a custom name for prebuilt entity types, and the `type` label for recognized prebuilt entities in LUIS is prefixed with `builtin.`. E.g., if you add the `personName` prebuilt entity to a LUIS app, labeled entities returned from the model will have type `builtin.personName`. The expectation of the [utterances model](GenericUtterances.md) is that users supply their own name for all entity types, including those that will be configured as prebuilt entity types on LUIS. To overcome the limitation on LUIS, we've added a settings property called `prebuiltEntityTypes`. This property includes all mappings from user supplied names for entity types to the LUIS prebuilt entity type name.

E.g., for the following training utterances:
```json
[
  {
    "text": "say hi to bill",
    "intent": "SendMessage",
    "entities": [
      {
        "entityType": "Recipient",
        "matchText": "bill",
        "matchIndex": 0
      }
    ]
  },
  {
    "text": "tell anne thanks",
    "intent": "SendMessage",
    "entities": [
      {
        "entityType": "Recipient",
        "matchText": "anne",
        "matchIndex": 0
      }
    ]
  },
  {
    "text": "say goodbye to mary",
    "intent": "SendMessage",
    "entities": [
      {
        "entityType": "Recipient",
        "matchText": "mary",
        "matchIndex": 0
      }
    ]
  }
]
```

To configure the `Recipient` entity type as a LUIS prebuilt entity, the [`--model-settings`](Train.md#-m---model-settings) file supplied to the `train` command should look like:
```json
{
  "prebuiltEntityTypes": {
    "Recipient": "personName"
  },
  "appTemplate": {
    "prebuiltEntities": [
      {
        "name": "personName"
      }
    ]
  }
}
```

During testing, to ensure that the entity types returned from LUIS are remapped back to the user-supplied entity type name, you must also supply the LUIS settings file above to the `test` command via the [`--model-settings`](Test.md#-m---model-settings) option.

## Configuring builtin intents

To configure a builtin intent for LUIS, add the following to the `appTemplate` property of your LUIS settings JSON (i.e., the [`--model-settings`](Train.md#-m---model-settings) file supplied to the `train` command):
```json
{
  "appTemplate": {
    "intents": [
      {
        "name": "Skip",
        "inherits": {
          "domain_name": "Music",
          "model_name": "SkipForward"
        }
      }
    ],
    "utterances": [
      {
        "text": "next song",
        "intent": "Skip",
        "entities": []
      }
    ]
  }
}
```

Note that for LUIS, you will also need to include at least one utterance for each configured intent, including builtin intents. If you do not wish to include utterances in the generic utterances file supplied to the `train` command, it's fine to add these to the `appTemplate` section as well. We've done this in the sample above.

## Schema

### `prebuiltEntityTypes`
(Optional) The mapping from user-supplied entity type names to LUIS prebuilt entity names.

See [Configuring prebuilt entities](#configuring-prebuilt-entities) for more information.

### `appTemplate`
(Optional) The partial LUIS import JSON that will be merged into the model before importing. 

Note, if you plan to train and test with entities, you must supply entity type configurations through this property. See [Configuring LUIS entities](#configuring-luis-entities) for more information.
