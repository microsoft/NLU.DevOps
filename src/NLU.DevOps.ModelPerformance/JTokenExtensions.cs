// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace NLU.DevOps.ModelPerformance
{
    using System;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using Logging;
    using Microsoft.CodeAnalysis.CSharp.Scripting;
    using Microsoft.CodeAnalysis.Scripting;
    using Microsoft.Extensions.Logging;
    using Newtonsoft.Json.Linq;

    internal static class JTokenExtensions
    {
        private static readonly Regex CSharpScriptRegex = new Regex(@"csharp\((.*)\)");

        private static ILogger Logger => LazyLogger.Value;

        private static Lazy<ILogger> LazyLogger { get; } = new Lazy<ILogger>(() => ApplicationLogger.LoggerFactory.CreateLogger(typeof(JTokenExtensions)));

        public static JToken Evaluate(this JToken json, object globals)
        {
            try
            {
                return json.EvaluateAsync(globals).Result;
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, ex.Message);
                return json;
            }
        }

        public static bool ContainsSubtree(this JToken expected, JToken actual)
        {
            if (expected == null)
            {
                return true;
            }

            if (actual == null)
            {
                return false;
            }

            switch (expected)
            {
                case JObject expectedObject:
                    var actualObject = actual as JObject;
                    if (actualObject == null)
                    {
                        return false;
                    }

                    foreach (var expectedProperty in expectedObject.Properties())
                    {
                        var actualProperty = actualObject.Property(expectedProperty.Name, StringComparison.Ordinal);
                        if (!ContainsSubtree(expectedProperty.Value, actualProperty?.Value))
                        {
                            return false;
                        }
                    }

                    return true;
                case JArray expectedArray:
                    var actualArray = actual as JArray;
                    if (actualArray == null)
                    {
                        return false;
                    }

                    foreach (var expectedItem in expectedArray)
                    {
                        // Order is not asserted
                        if (!actualArray.Any(actualItem => ContainsSubtree(expectedItem, actualItem)))
                        {
                            return false;
                        }
                    }

                    return true;
                default:
                    return JToken.DeepEquals(expected, actual);
            }
        }

        private static async Task<JToken> EvaluateAsync(this JToken json, object globals)
        {
            if (json == null)
            {
                return null;
            }

            switch (json)
            {
                case JObject jsonObject:
                    var updatedObject = default(JObject);
                    foreach (var jsonProperty in jsonObject.Properties())
                    {
                        var propertyValue = jsonObject.Property(jsonProperty.Name, StringComparison.Ordinal);
                        var evaluatedValue = await EvaluateAsync(propertyValue.Value, globals).ConfigureAwait(false);
                        if (evaluatedValue != propertyValue.Value)
                        {
                            updatedObject = updatedObject ?? (JObject)jsonObject.DeepClone();
                            updatedObject[jsonProperty.Name] = evaluatedValue;
                        }
                    }

                    return updatedObject ?? jsonObject;
                case JArray jsonArray:
                    var updatedArray = default(JArray);
                    for (var i = 0; i < jsonArray.Count; ++i)
                    {
                        var evaluatedValue = await EvaluateAsync(jsonArray[i], globals).ConfigureAwait(false);
                        if (evaluatedValue != jsonArray[i])
                        {
                            updatedArray = updatedArray ?? (JArray)jsonArray.DeepClone();
                            updatedArray[i] = evaluatedValue;
                        }
                    }

                    return updatedArray ?? jsonArray;
                case JValue jsonValue:
                    if (jsonValue.Type == JTokenType.String)
                    {
                        var scriptMatch = CSharpScriptRegex.Match(jsonValue.Value<string>());
                        if (scriptMatch.Success)
                        {
                            var script = scriptMatch.Groups[1].Value;
                            var result = await EvaluateScriptAsync(script, globals).ConfigureAwait(false);
                            return JToken.FromObject(result);
                        }
                    }

                    return json;
                default:
                    return json;
            }
        }

        private static async Task<object> EvaluateScriptAsync(string script, object globals)
        {
            var options = ScriptOptions.Default.WithImports("System");
            try
            {
                return await CSharpScript.EvaluateAsync(script, options, globals).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to evaluate script '{script}'.", ex);
            }
        }
    }
}
