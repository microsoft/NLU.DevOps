## GitHub Actions workflow using NLU.DevOps CLI tool

This document covers setting up a CI pipeline for LUIS using GitHub Actions and NLU.DevOps.
The workflow will be the following: 
1. create, train, publish the LUIS model using sample utterances
2. send a test set to created LUIS model
3. evaluate model by comparing results received from LUIS with expected values
4. delete the model from the portal

Supply a name for the action and set it up to trigger on pull requests.

```
name: CINLU

on: [pull_request]
```

1. Install NLU.DevOps CLI tool on GitHub agent.

```
    - name: Install dotnet-nlu
      run: dotnet tool install -g dotnet-nlu
```
For Ubuntu agents, you need to prepend a directory to the system PATH variable for all subsequent actions in the current job to make sure that CLI tool works. More about this command [here](https://help.github.com/en/actions/automating-your-workflow-with-github-actions/development-tools-for-github-actions#add-a-system-path-add-path).

```
    - name: Path
      run: echo "::add-path::$HOME/.dotnet/tools"
```
We use [utterances.json](../models/utterances.json) for training. You can replace this file with another file that consists of intents, utterances, entities that you need for your own model.
More about the format of this file [here](https://github.com/microsoft/NLU.DevOps/blob/master/docs/GenericUtterances.md).
To train your model we should add the following:

```
- name: Train Luis model
  run: dotnet nlu train -s luis -u utterances.json --save-appsettings
  env:
    luisAuthoringRegion: ${{ secrets.luisAuthoringRegion }}
    luisAuthoringKey: ${{ secrets.luisAuthoringKey }}
 ```

More about the command [here](https://github.com/microsoft/NLU.DevOps/blob/master/docs/Train.md). 
Before you push to the repo, you need to add credentials (at least luisAuthoringKey and luisAuthoringRegion are required) to your GitHub Secrets. For example,
![credentials](./images/credentials.png)

Check out the [LUIS docs](https://docs.microsoft.com/en-us/azure/cognitive-services/luis/luis-how-to-azure-subscription) for more information on where to find your authoring or runtime keys.

2. To test LUIS model let's use [utterancesTest.json](../models/utterancesTest.json) file.
We can save the result in results.json. During the training step, we may have created a new LUIS application. By using the `--save-appsettings` flag, the LUIS application ID is stored locally in a settings file that is picked up by subsequent NLU.DevOps CLI commands.

Yaml may look like that:
```
    - name: Test Luis model
      run: dotnet nlu test -s luis -u utterancesTest.json -o results.json
      env: 
        luisAuthoringRegion: ${{ secrets.luisAuthoringRegion }}
        luisAuthoringKey: ${{ secrets.luisAuthoringKey }}
```
3. We use the `compare` command from the NLU.DevOps CLI to evaluate the results. The expected intents and entities in this case are given in the `utterancesTest.json` file, while the results predicted by the LUIS model are in the `results.json` file.

```
    - name: Compare Luis model
      run: dotnet nlu compare -e utterancesTest.json -a results.json
```

If you open your GitHub workflow run step for this command in the console, you can see something similar to
![compareResults](./images/compareResults.png)

4. When you work on several hypotheses, sometimes you need only to get results and you don't want to keep the model. It is possible to delete the model in the same pipeline after you get results.
```
    - name: Delete Luis model
      run: dotnet nlu clean -s luis
      env: 
        luisAuthoringRegion: ${{ secrets.luisAuthoringRegion }}
        luisAuthoringKey: ${{ secrets.luisAuthoringKey }}
```

You can find GitHub Action workflow yaml file [here](../pipelines/.github/workflows/nlugithub.yml).
