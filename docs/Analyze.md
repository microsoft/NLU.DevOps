# Analyzing NLU model results

The NLU.DevOps CLI tool includes two sub-commands that allows you to compare results returned from an NLU model via the `test` command with the expected results, `compare` and `benchmark`.

The `compare` command generates NUnit test output and can often be useful to generate smoke tests for high priority NLU scenarios. The `benchmark` command generates confusion matrix output, and can be used for comparing NLU model results against a baseline (e.g., results from previous release).

## Getting Started

Run one of the following commands:
```bash
dotnet nlu compare -e utterances.json -a results.json
dotnet nlu benchmark -e utterances.json -a results.json
```

The `utterances.json` argument is the path to the "expected" utterances file, usually the file path you supplied to the `test` command. The `results.json` argument is the path to the output utterances from a `test` command (see [Testing an NLU model](Test.md) for more details). The two files must have the same number of utterances in the exact same order, which will be the case if you supply the same `utterances.json` to the `compare` or `benchmark` command as you supplied to `test`.

For example, if you use the training cases supplied in [Training an NLU model](Train.md#getting-started) and the test cases supplied in [Testing an NLU model](Test.md#getting-started) on LUIS, you will recall that we had one resulting intent that was incorrectly labeled with the "None" intent. In this case, the `compare` command will generate passing tests (either true positive or true negative) for all text, intents, and entities, except for the one mismatched case. The mismatched case will generate a single failing test result, labeled as a false negative intent. Here is the specific output:
```bash
Test Discovery
  Start time: 2018-12-12 21:51:23Z
    End time: 2018-12-12 21:51:24Z
    Duration: 0.735 seconds

Errors, Failures and Warnings

1) Failed : NLU.DevOps.ModelPerformance.Tests.Tests.FalseNegativeIntent('PlayMusic', 'listen to hip hop')
Actual intent is 'None', expected 'PlayMusic'
   at NLU.DevOps.ModelPerformance.Tests.Tests.Fail(String because) in c:\src\NLU.DevOps\src\NLU.DevOps.ModelPerformance.Tests\Tests.cs:line 22

Run Settings
    Number of Test Workers: 4
    Work Directory: C:\src\sandbox\nlu-demo
    Internal Trace: Off

Test Run Summary
  Overall result: Failed
  Test Count: 18, Passed: 17, Failed: 1, Warnings: 0, Inconclusive: 0, Skipped: 0
    Failed Tests - Failures: 1, Errors: 0, Invalid: 0
  Start time: 2018-12-12 21:51:24Z
    End time: 2018-12-12 21:51:24Z
    Duration: 0.224 seconds
```

### Command Differences

### `compare`

The `compare` command will generate NUnit output, where true positive and true negative results are treated as passing tests and false positive and false negative results are treated as failing tests. It will identify true positives, true negatives, false positives, and false negatives for intents, entities and entity values. For entities, false positive results will only be identified for explicitly declared entity types. For entity values, only either true positive or false negative results are generated.

For example, the following is likely to generate a false positive entity result for `genre`, as it was not declared in `entities` and the type was listed in the `strictEntities` property.
```json
[
  {
    "text": "play a jazz song",
    "intent": "PlayMusic",
    "entities": [],
    "strictEntities": [ "genre" ]
  }
]
```

### `benchmark`

The `benchmark` command will generate JSON output for the confusion matrix with the following format:
```plaintext
[
  {
    "utteranceId": "0", /* index or user-provided ID */
    "group": "intentName", /* intent name or entity type for result */
    "resultKind": "truePositive", /* one of: truePositive, trueNegative, falsePositive, falseNegative */
    "targetKind": "intent", /* one of: intent, entity, entityValue, text */
    "expectedUtterance": {
      ...
    }, /* JSON for expected utterance */
    "actualUtterance": {
      ...
    }, /* JSON for actual utterance */
    "score": 0.5, /* prediction score */
  }
]
```

The results generated for the `benchmark` command are equivalent to the `compare` command for intents. For entities, `benchmark` generates all false positive results for unexpected entities by default, except in cases where they are explicity ignored.

For example, the following would not generate a false positive result for `genre`, as even though it was not declared in `entities`, the entity type was listed in the `ignoreEntities` property.
```json
[
  {
    "text": "play a jazz song",
    "intent": "PlayMusic",
    "entities": [],
    "ignoreEntities": [ "genre" ]
  }
]
```

## Configuring test settings

There are a handful of test settings that you can specify when running either the `compare` or `benchmark` command. Currently, you can configure:

- `trueNegativeIntent` - The intent name used to denote a negative response. In LUIS, this is typically the "None" intent. In Dialogflow or Lex, this is generally the default intent.
- `strictEntities` - Configures a global set of entity types that should generate false positive entity results. This is only used for unit testing with the `compare` command. You can override this setting for an entity type in a particular utterance test case by specifying it in the local `ignoreEntities` property on the test utterance.
- `ignoreEntities` - Configures a global set of entity types that should not generate false positive entity results. This is only used for F-measure testing with the `benchmark` command. You can override this setting for an entity type in a particular utterance test case by specifying it in the local `strictEntities` property on the test utterance.

For example, the following specifies an intent called "None" as the true negative intent and ignores false postives from the "number" entity:
```json
{
  "trueNegativeIntent": "None",
  "ignoreEntities": [ "number" ]
}
```

You can specify these test settings using the [`--test-settings`](#-t---test-settings) option.

## How text is compared

Currently, we do not have any way of overriding the string comparison logic used when comparing expected text versus actual text in the utterances. For now, the text is compared using `StringComparison.OrdinalIngoreCase` after all whitespace is normalized and punctuation is removed.

## How entities are compared

Currently, we do not have any way of overriding the string comparison logic used when comparing expected entity text versus actual entity text in the utterances. For now, the entity text is compared using `StringComparison.OrdinalIngoreCase` after all whitespace is normalized and punctuation is removed. If the NLU provider does not specify the [`matchText`](GenericUtterances.md#matchText) in the actual entity, as is the case for Lex and Dialogflow, the [`entityValue`](GenericUtterances.md#entityvalue) is used to find a matching entity with the same value in either the [`matchText`](GenericUtterances.md#matchText) or [`entityValue`](GenericUtterances.md#entityvalue) in the expected entity.

## Why have separate test cases for entity values

The generic utterances model includes an [`entityValue`](GenericUtterances.md#entityvalue) property, which is the semantic or canonical form of the entity. Often times, it's useful enough to know that the NLU model identifies a match for the entity text in the utterance, so we created a separate test case type that compares the expected entity value with the actual entity value, in cases where this property is expected. We assert that the actual entity value contains the JSON subtree specified in the expected entity value.

## Detailed Usage

### `-e, --expected`
The path to the expected labeled utterances.

Usually, this is the same file you supplied to the `test` command.

### `-a, --actual`
The path to the result utterances from the `test` command.

Be sure to use the [`--output`](Test.md#-o---output) option when running the `test` command.

### `-o, --output-folder`
(Optional) The folder where the NUnit test results or confusion matrix JSON is written. If not provided, the current working directory is used.

### `-t, --test-settings`
(Optional) Path to test settings file used to configure the NLU comparison. See [Configuring test settings](#configuring-test-settings).

### `-b, --baseline`
(Optional) For the `benchmark` sub-command only, specifies output folder of a previous test run to compare against.
