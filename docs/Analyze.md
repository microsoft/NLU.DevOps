# Analyzing NLU model results

The NLU.DevOps CLI tool includes the `compare` command, which allows you to compare results returned from an NLU model via the `test` command with the expected results.

The `compare` command generates NUnit test output and confusion matrix results in JSON format, and can be used either to assert all tests pass or measure performance against a baseline test run.

## Getting Started

Run the following command:
```bash
dotnet nlu compare -e utterances.json -a results.json
```

The `utterances.json` argument is the path to the "expected" utterances file, usually the file path you supplied to the `test` command. The `results.json` argument is the path to the output utterances from a `test` command (see [Testing an NLU model](Test.md) for more details). The two files must have the same number of utterances in the exact same order, which will be the case if you supply the same `utterances.json` to the `compare` command as you supplied to `test`.

The `compare` command generates confusion matrix results identifying true positive, true negative, false positive, and false negative results for intents, entities and entity values. Both false positive and false negative results are also generated for mismatched intents, except in cases that the expected intent is either `null`, `undefined` or equal to the configured value for [`trueNegativeIntent`](#truenegativeintent).

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

The unit test XML results are also written to a file called `TestResult.xml` in the folder specified by the [`--output-folder`](#-o---output-folder) option (or the current working directory if not specified).

## JSON Output

### Metadata Output

In addition to the NUnit output, the `compare` command will output a JSON file called `metadata.json` to the [--output-folder](#-o---output-folder) for the confusion matrix with the following format:
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

### Statistics Output

The `compare` command also generates a much smaller high-level summary of confusion matrix results grouped by intent and entity type, called `statistics.json`:
```plaintext
{
  "intent": [
    7, /* true positive */
    4, /* true negative */
    1, /* false positive */
    3  /* false negative */
  ],
  "entity": [ 2, 10, 1, 2 ],
  "byIntent": {
    "PlayMusic": [ 5, 0, 1, 1 ],
    "Skip": [ 2, 0, 0, 2 ]
  },
  "byEntityType": {
    "Genre": [ 1, 0, 0, 2 ]
  }
}
```

The `metadata.json` output is useful for reviewing fine-grained details of NLU tests, whereas the `statistics.json`, given it's more compact representation, is useful for comparing course-grained measurements (e.g., precision and recall) over time across multiple test runs.

## Test Modes

The `compare` command can run in one of two modes, performance test mode (default) or unit test mode. The difference between performance test mode and unit test mode is in the way false positive results are handled: unit test mode is more lenient in the sense that, by default, false positive results are ignored.

### Performance Test Mode

Performance test mode is the default mode for the `compare` command. In performance test mode, false positive results will be generated for any entity that is not declared as an expected entity, unless it's type is included in the `ignoreEntities` configuration in the supplied [`--test-settings`](#-t---test-settings), or in an `ignoreEntities` property on the labeled test utterance.

#### Performance Baseline

Performance test mode allows you to compare against a baseline performance benchmark (e.g., the results from the latest commit to master or the current release). You can specify this baseline by supplying a path to the [`statistics.json`](#statistics-output) output of a previous run via the [`--baseline`](#-b---baseline) CLI option.

#### Performance Regression Thresholds

When comparing against a [baseline](#performance-baseline), you can also specify performance regression thresholds. This is useful for setting up continuous integration that prevents performance regressions greater than a specific amount. Currently, the only supported regression thresholds are based on the F<sub>1</sub> scores for intents and entities. The thresholds are configured via the [test settings](#configuring-test-settings) supplied to the `compare` command:

Here are the properties that can be declared for thresholds:
- `type` - Specify `intent` for setting a regression threshold for intents, or `entity` for entities.
- `group` - Optional. When specified, narrows the regression to the specific intent or entity name supplied. When not specified, uses the threshold for all intents or entities (depending on `type`). You may also specify `*` to target all intents or entities.
- `threshold` - Optional. The numeric regression threshold value. For example, specifying a threshold of 0.1 means that the command will fail if the F<sub>1</sub> score for the targeted intent(s) or entity(s) decreases by more than 0.1. When not specified, a threshold of 0 is used.

Here's an example threshold configuration that limits the performance regression for all intents to 0.05, the `PlayMusic` intent to 0, and all entities to 0.1:

```yaml
thresholds:
- type: intent
  threshold: 0.05
- type: intent
  group: PlayMusic
- type: entity
  group: '*'
  threshold: 0.1
```

### Unit Test Mode

Unit test mode can be enabled using the [`--unit-test`](#-u---unit-test) flag. This flag configures the command to return a non-zero exit code if any false positive or false negative results are detected. When in unit test mode, false positive results for entities are only generated for entity types included in the `strictEntities` configuration from `--test-settings` or the labeled test utterance. Similarly, false positive results will only be generated for intents when an explicit negative intent (e.g., "None") is included in the expected results. For example:

#### Example: False positive entity is not generated
If the expected JSON is:
```json
{
  "text": "Play two songs",
  "intent": "PlayMusic"
}
```

And the results JSON includes an entity with no matching expected entity:
```json
{
  "text": "Play two songs",
  "intent": "PlayMusic",
  "entities": [
    {
      "entityType": "count",
      "matchText": "two",
      "entityValue": 2
    }
  ]
}
```
A false positive entity **will not** be generated in unit test mode.

#### Example: False positive entity is generated
If the expected JSON sets strict behavior for an entity type:
```json
{
  "text": "Play rock music",
  "intent": "PlayMusic",
  "strictEntities": [ "celebrity" ],
  "entities": [
    {
      "entityType": "genre",
      "matchText": "rock"
    }
  ]
}
```

And the results JSON includes that entity type with no matching expected entity:
```json
{
  "text": "Play rock music",
  "intent": "PlayMusic",
  "entities": [
    {
      "entityType": "genre",
      "matchText": "rock"
    },
    {
      "entityType": "celebrity",
      "matchText": "rock",
      "entityValue": "Dwayne Johnson"
    },
  ]
}
```

A false positive entity **will** be generated in unit test mode.

#### Example: False positive intent is not generated
If the expected JSON does not include an intent:
```json
{
  "text": "What is jazz?",
  "entities": [
    {
      "entityType": "genre",
      "matchText": "jazz"
    },
  ]
}
```

And the results JSON does include an intent:
```json
{
  "text": "What is jazz?",
  "intent": "PlayMusic",
  "entities": [
    {
      "entityType": "genre",
      "matchText": "jazz"
    }
  ]
}
```
A false positive intent **will not** be generated in unit test mode.

### False positive intent is generated

If the expected JSON explicitly declares a negative intent:
```json
{
  "text": "What is jazz?",
  "intent": "None"
}
```

And the results JSON has a positive intent:
```json
{
  "text": "What is jazz?",
  "intent": "PlayMusic",
  "entities": [
    {
      "entityType": "genre",
      "matchText": "jazz"
    }
  ]
}
```
A false positive intent **will** be generated in unit test mode.

## Configuring test settings

There are a few test settings that you can specify when running the `compare` command using the [`--test-settings`](#-t---test-settings) option.

### `trueNegativeIntent`

The intent name used to denote a negative response. In LUIS, this is typically the "None" intent. In Dialogflow or Lex, this is generally the default intent.

### `strictEntities`

Configures a global set of entity types that should generate false positive entity results. This is only used when the [`--unit-test`](#-u---unit-test) flag is set. You can override this setting for an entity type in a particular utterance test case by specifying it in the local `ignoreEntities` property on the test utterance.

### `ignoreEntities`

Configures a global set of entity types that should not generate false positive entity results. This is only used when the [`--unit-test`](#-u---unit-test) flag is not set. You can override this setting for an entity type in a particular utterance test case by specifying it in the local `strictEntities` property on the test utterance.

### Example test settings

The following specifies an intent called "None" as the true negative intent and ignores false postives from the "number" entity:

```json
{
  "trueNegativeIntent": "None",
  "ignoreEntities": [ "number" ]
}
```

```yaml
trueNegativeIntent: None
ignoreEntities:
- number
```

The test settings can be formatted as JSON or YAML and are supplied via the [`--test-settings`](#-t---test-settings) option.

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

### `-u, --unit-test`
(Optional) Flag used to specify that the compare operation should run in unit test mode. See [Unit Test Mode](#unit-test-mode) for more details.

### `-b, --baseline`
(Optional) Specifies the path to the confusion matrix [statistics](#statistics-output) of a previous test run to compare against.
