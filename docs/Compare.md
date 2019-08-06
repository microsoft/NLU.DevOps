# Analyzing NLU model results

The NLU.DevOps CLI tool includes a sub-command that allows you to compare results returned from an NLU model via the `test` command with the expected results.

## Getting Started

Run the following command:
```bash
dotnet nlu compare -e utterances.json -a results.json
```

The `utterances.json` argument is the path to the "expected" utterances file, usually the file path you supplied to the `test` command. The `results.json` is the path to the 
to the output utterances from a `test` command (see [Testing an NLU model](Test.md) for more details). The two files must have the same number of utterances in the exact same order, which will be the case if you supply the same `utterances.json` to the `compare` command as you supplied to `test`.

The `compare` sub-command will generate sensitivity and specifity test results for the text, intents, and entities in the two files. I.e., it will identify true positives, true negatives, false positives, and false negatives for text, intents, entities and entity values.

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

## How text is compared

Currently, we do not have any way of overriding the string comparison logic used when comparing expected text versus actual text in the utterances. For now, the text is compared using `StringComparison.OrdinalIngoreCase` after all whitespace is normalized and punctuation is removed.

## How entities are compared

Currently, we do not have any way of overriding the string comparison logic used when comparing expected entity text versus actual entity text in the utterances. For now, the entity text is compared using `StringComparison.OrdinalIngoreCase` after all whitespace is normalized and punctuation is removed.

## Why have separate test cases for entity values?

The generic utterances model includes an ["entityValue"](GenericUtterances.md#entityvalue) property, which is the semantic or canonical form of the entity. Often times, it's useful enough to know that the NLU model identifies a match for the entity text in the utterance, so we created a separate test case type that strictly compares the expected entity value with the actual entity value, in cases where this property is expected.

## Detailed Usage

### `-e, --expected`
The path to the expected labeled utterances.

Usually, this is the same file you supplied to the `test` command.

### `-a, --actual`
The path to the result utterances from the `test` command.

Be sure to use the [`--output`](Test.md#-o---output) option when running the `test` command.

### `-o, --output-folder`
(Optional) The path to write the NUnit test results.

### `-l, --label`
(Optional) A prefix for the test case names, in cases where you may want to publish multiple test runs for different options (e.g., simultaneously test text utterances and speech).
