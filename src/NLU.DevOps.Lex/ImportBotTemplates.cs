// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace NLU.DevOps.Lex
{
    using Newtonsoft.Json.Linq;

    internal static class ImportBotTemplates
    {
        public static JObject ImportJson =>
            new JObject
            {
                {
                    "metadata",
                    new JObject
                    {
                        { "schemaVersion", "1.0" },
                        { "importType", "LEX" },
                        { "importFormat", "JSON" },
                    }
                },
                {
                    "resource",
                    new JObject
                    {
                        { "name", string.Empty },
                        { "intents", new JArray() },
                        { "slotTypes", new JArray() },
                        { "voiceId", "0" },
                        { "childDirected", false },
                        { "locale", "en-US" },
                        { "idleSessionTTLInSeconds", 300 },
                        { "clarificationPrompt", PromptJson("Sorry, can you please repeat that?", 5) },
                        { "abortStatement", AbortStatementJson },
                    }
                }
            };

        public static JObject IntentJson =>
            new JObject
            {
                { "name", string.Empty },
                { "fulfillmentActivity", FulfillmentActivityJson },
                { "sampleUtterances", new JArray() },
                { "slots", new JArray() },
            };

        public static JObject SlotJson =>
            new JObject
            {
                { "name", string.Empty },
                { "slotConstraint", "Optional" },
                { "slotType", string.Empty },
                { "valueElicitationPrompt", PromptJson("E.g. What thing?", 2) },
                { "priority", 1 },
                { "sampleUtterances", new JArray() },
            };

        private static JObject AbortStatementJson =>
            new JObject
            {
                {
                    "messages",
                    new JArray { MessageJson("Sorry, I could not understand. Goodbye.") }
                },
            };

        private static JObject FulfillmentActivityJson =>
            new JObject
            {
                { "type", "ReturnIntent" }
            };

        private static JObject MessageJson(string message) =>
            new JObject
            {
                { "contentType", "PlainText" },
                { "content", message },
            };

        private static JObject PromptJson(string message, int maxAttempts) =>
            new JObject
            {
                { "messages", new JArray { MessageJson(message) } },
                { "maxAttempts", maxAttempts },
            };
    }
}
