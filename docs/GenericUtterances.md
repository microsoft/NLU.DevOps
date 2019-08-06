# Generic utterances model

The NLU.DevOps CLI tool uses a generic utterances format that can work with multiple NLU providers, and also contains enough information for sensitivity and specificity testing across intents and entities.

## Example

```json
{
  "text": "play music by the stones",
  "intent": "PLayMusic",
  "entities": [
    {
      "entityType": "Artist",
      "matchText": "the stones",
      "matchIndex": 0,
      "entityValue": "The Rolling Stones"
    }
  ]
}
```

## Schema

### `text`

The utterance text.

### `intent`

The intent label for the utterance.

### `entities`

(Optional) The array of entities in the utterance.

#### `entityType`

The entity type or slot type.

#### `matchText`

The substring in the utterance `text` that represents the entity.

#### `matchIndex`

The occurrence index of the `matchText` in the utterance `text`.

E.g., if the `matchIndex` is 1, `matchText` is "music", and the utterance `text` is "play music by music", the entity will map to the second occurrence of the term "music". The algorithm does not try to be smart about tokenization or spacing, so to match "foo" in "food fool foo", you need to specify `"matchIndex": 2`.

#### `entityValue`

(Optional) Specify the semantic value or canonical form of the matched entity. This can be useful for, e.g., entity types matching dates where the entity "next week" is expected to map to some specific date, or for canonical forms of entities linked as a synset.
