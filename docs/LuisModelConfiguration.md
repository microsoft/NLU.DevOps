# LUIS model configuration

The `train` sub-command in the NLU.DevOps CLI tool accepts a `--model-settings` option, which allows the user to configure NLU provider-specific features such as builtin intents and phrase lists.

The `--model-settings` for LUIS is a pointer to a JSON file which is used as a template for the LUIS app JSON that will be used to train the model. You can use it to train either a fully defined model exported as JSON from the LUIS portal, or to combine it with a set of generic utterances supplied with the [`--utterances`](Train.md#-u---utterances) option.

## Configuring LUIS features

To configure any LUIS features, such as specifying an entity type as a closed list or prebuilt entity, including a phrase list, adding built-in intents, etc., create a JSON file with the corresponding LUIS features defined and pass it to the `train` command via the [`--model-settings`](Train.md#-m---model-settings) option:
```json
{
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
```

## Training entities with roles

In order to label a [LUIS role](https://docs.microsoft.com/en-us/azure/cognitive-services/luis/luis-concept-roles) on an entity in a generic utterance, you can label the [`entityType`](GenericUtterances.md#entityType) using the role name, but you must also pre-define an entity type with that role in the LUIS app template provided via the `train` command [`--model-settings`](Train.md#-m---model-settings) option.

For example, if you have the following utterance with a role-labeled entity:

```json
{
  "text": "play two songs from my play list",
  "intent": "PlayMusic",
  "entities": [
    {
      "matchText": "two",
      "entityType": "songCount"
    }
  ]
}
```

Then you must also include a LUIS app template with the `builtin.number` entity and `songCount` role pre-defined:

```json
{
  "prebuiltEntities": [
    {
      "name": "number",
      "roles": [
        "songCount"
      ]
    }
  ]
}
```

This will result in the labeled entity being sent to LUIS as follows:

```json
{
  "startPos": 5,
  "endPos": 7,
  "entity": "number",
  "role": "songCount"
}
```

## Testing entities with roles

In both LUIS v2 and v3, if a role is recognized, the [`entityType`](GenericUtterances.md#entityType) property will use the role name instead of the entity type.
