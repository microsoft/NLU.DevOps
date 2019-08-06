# Dialogflow endpoint configuration

Before using the NLU.DevOps tool, you need to supply Dialogflow secrets and endpoint configuration to be able to test a Dialogflow model. The values are configured through [`Microsoft.Extensions.Configuration`](https://docs.microsoft.com/en-us/dotnet/api/microsoft.extensions.configuration?view=aspnetcore-2.1) (i.e., an `appsettings.local.json` file or environment variables).

## Configuration for testing

At a minimum to get started, you must supply the Dialogflow client authentication key with [relevant permissions](https://dialogflow.com/docs/reference/v2-auth-setup), as well as the Dialogflow project ID.

```json
{
  "dialogflowClientKeyPath": "...",
  "dialogflowProjectId": "..."
}
```

This will allow you to call the `test` sub-command for Dialogflow (see [Testing an NLU model](Test.md) for more details).

Options to consider for testing a Lex bot include:
- [`dialogflowClientKeyPath`](#dialogflowclientkeypath)
- [`dialogflowClientKeyJson`](#dialogflowclientkeyjson)
- [`dialogflowProjectId`](#dialogflowprojectid)
- [`dialogflowSessionId`](#dialogflowsessionid)

## App Settings Variables

### `dialogflowClientKeyPath`
(Optional) Google credential path for authenticating the Dialogflow client.

Optional for `test`. If not supplied, [`dialogflowClientKeyJson`](#dialogflowclientkeypath) must be specified.

### `dialogflowClientKeyJson`
(Optional) Google credential JSON for authenticating the Dialogflow client.

Optional for `test`. If not supplied, [`dialogflowClientKeyPath`](#dialogflowclientkeypath) must be specified.

### `dialogflowProjectId`
Dialogflow project ID.

Required for `test`.

### `dialogflowSessionId`
(Optional) Dialogflow session ID.

Optional for `train`. If not supplied, a random GUID is selected for the session ID for each utterance test.
