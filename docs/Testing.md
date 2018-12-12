# Testing an NLU service

The NLU.DevOps CLI tool includes a sub-command that allows you to test an NLU service from generic utterances.

## Getting started

Run the following command:
```bash
dotnet nlu test -s luis -u utterances.json
```

The `utterances.json` argument is the path to the generic utterances file, which may look something like:
```json
[
  {
    "text": "start playing music",
    "intent": "PlayMusic"
  },
  {
    "text": "play music",
    "intent": "PlayMusic"
  },
  {
    "text": "listen to hip hop",
    "intent": "PlayMusic"
  },
  {
    "text": "is it cold out",
    "intent": "None"
  },
  {
    "text": "how many days until Christmas",
    "intent": "None"
  },
  {
    "text": "what's the weather like",
    "intent": "None"
  }
]
```

The resulting output will be a JSON array in the generic utterances format, with the intents and entities labeled by the NLU service. E.g., after training a LUIS service from [Getting Started section in Training NLU Services](Training.md#getting-started), testing with the example utterances above will output the following results:

```json
[
  {
    "text": "start playing music",
    "intent": "PlayMusic"
  },
  {
    "text": "play music",
    "intent": "PlayMusic"
  },
  {
    "text": "listen to hip hop",
    "intent": "None"
  },
  {
    "text": "is it cold out",
    "intent": "None"
  },
  {
    "text": "how many days until Christmas",
    "intent": "None"
  },
  {
    "text": "what's the weather like",
    "intent": "None"
  }
]
```

See [Generic Utterances Model](GenericUtterances.md) for more information on the JSON schema for utterances.

See [LUIS Key Configuration](TODO) and [Lex Key Configuration](TODO) for more information on how to supply secrets, e.g., the endpoint key, to the CLI tool.

## Getting started with speech

The NLU.DevOps CLI tool also supports testing from speech. You still must supply the generic utterances file, except that each utterance in the file must also include a `speechFile` property, e.g.:

```json
{
  "text": "start playing music",
  "intent": "PlayMusic",
  "speechFile": "sample1.wav"
}
```

The `speechFile` corresponds to the file name of an audio file in the current working directory. You may also specify the `--speech-directory` option to set the base path for the speech files. Please note, the LUIS and Lex service options currently only support the 16KHz WAV format.

To kick off testing from speech with the CLI tool, run the following command:

```bash
dotnet nlu test -s luis --speech -u utterances.json
```

## Testing an existing NLU service

You do not need to train your NLU service with the NLU.DevOps CLI tool in order to test it with the tool. You can just as easily point the tool at existing trained models, e.g., on LUIS or Lex without training from the CLI tool.

See [LUIS Key Configuration](TODO) and [Lex Key Configuration](TODO) for more information on how to point to an existing service, e.g., with a LUIS app ID.

## Caching speech-to-text transcriptions

Running speech-to-text can be expensive both in terms of costs for the NLU service and in terms of compute time for how long it takes to transcribe the speech before extracting intents and entities. The NLU.DevOps CLI tool allows you to specify a transcriptions file path, which is a JSON object containing a mapping of speech file names to transcribed text, e.g.:

```json
{
  "sample1.wav": "Start playing music."
}
```

When the `--transcriptions` option is used, the CLI tool will check to see if a transcription is already cached, and if so, call the test API for text. The tool will also save any new transcriptions that are computed by the NLU service to that same file.

To kick off testing from speech with cached transcriptions, run the following command:

```bash
dotnet nlu test -s luis --speech -u utterances.json -t transcriptions.json
```

## Detailed Usage

### `-s, --service`
Identifier of the NLU service to run against. Try `luis` for [LUIS](https://www.luis.ai) or `lex` for [Lex](https://aws.amazon.com/lex/).

### `-u, --utterances`
Path to labeled utterances to test with. Only the text field from each utterance will be sent to the NLU service (or the audio in the case of the `--speech` file).

### `-o, --output`
(Optional) Path to labeled results output.

If the `--output` option is not provided, the results will be written to stdout.

### `--speech`
(Optional) Test using speech files from utterances.

Please note, the LUIS and Lex service options currently only support the 16KHz WAV format.

See [Getting started with speech](#getting-started-with-speech).

### `-d, --speech-directory`
(Optional) Path to speech files directory.

See [Getting started with speech](#getting-started-with-speech).

### `-t, --transcriptions`
(Optional) Path to transcriptions file.

See [Caching speech-to-text transcriptions](#caching-speech-to-text-transcriptions).

### `-e, --extra-settings`
(Optional) Path to NLU service-specific settings.

This is currently only used for LUIS, see the section on prebuilt entity types in [Configuring LUIS Apps](TODO).

