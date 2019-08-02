This extension provides tasks for training, testing, and deleting NLU models from [LUIS](https://luis.ai), [Lex](https://aws.amazon.com/lex), and [DialogFlow](https://dialogflow.com). It wraps the [NLU.DevOps](https://github.com/microsoft/NLU.DevOps) CLI tool to reduce the number of steps needed in a build pipeline to set up CI/CD for an NLU model.

# Training an NLU model

To train an NLU model, add the following configuration to your pipeline:
```yaml
- task: NLUTrain@0
  inputs:
    service: luis
    modelSettings: path/to/luis.json
```
You can also train a model from the [generic utterances model](https://github.com/microsoft/NLU.DevOps/blob/master/docs/GenericUtterances.md) using the `utterances` input.

Check out the docs for [LUIS](https://github.com/microsoft/NLU.DevOps/blob/master/docs/LuisSecrets.md), [Lex](https://github.com/microsoft/NLU.DevOps/blob/master/docs/LexSecrets.md), and [DialogFlow](https://github.com/microsoft/NLU.DevOps/blob/master/docs/DialogFlowSecrets.md) for more information on pipeline variables you will need to set up access tokens and toggle NLU provider-specific features.

# Testing an NLU model

To test an NLU model, add the following configuration to your pipeline:
```yaml
- task: NLUTest@0
  inputs:
    service: luis
    utterances: path/to/tests.json
    publishTestResults: true
```

The `utterances` input should be a path to a JSON file with labeled [generic utterances](https://github.com/microsoft/NLU.DevOps/blob/master/docs/GenericUtterances.md). This format is similar to the [LUIS batch test](https://docs.microsoft.com/en-us/azure/cognitive-services/luis/luis-concept-batch-test#batch-file-format) format, except that entities are configured with `matchText` and `matchIndex` instead of `startPos` and `endPos`.

To test with speech WAV files, set the `speech` input to `true` and set the base directory for the speech files with the `speechDirectory` input. For more information about running NLU tests from speech, take a look at the [docs](https://github.com/microsoft/NLU.DevOps/blob/master/docs/Test.md#getting-started-with-speech) on NLU.DevOps.

Setting `publishTestResults` to `true` will run your test utterances against the NLU provider and compare the results against the details in the `utterances` input. It outputs failing tests for any false positive or false negative intents or entities. It outputs passing tests for any true positive or true negative intents or entities.

For reference, here is an example run of [NLU test results](https://dev.azure.com/NLUDevOps/NLU.DevOps/_build/results?buildId=574&view=ms.vss-test-web.build-test-results-tab).

# Deleting an NLU model

For CI/CD, you often want to import, test, and then delete an NLU model. To delete the NLU model you set up for testing in a CI environment:
```yaml
- task: NLUClean@0
  inputs:
    service: luis
```
