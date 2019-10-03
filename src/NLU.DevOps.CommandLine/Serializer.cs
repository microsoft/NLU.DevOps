// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace NLU.DevOps.CommandLine
{
    using System.IO;
    using System.Text;
    using Core;
    using ModelPerformance;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;
    using Newtonsoft.Json.Serialization;

    internal static class Serializer
    {
        public static T Read<T>(string path)
        {
            var serializer = JsonSerializer.CreateDefault();
            serializer.Converters.Add(new LabeledUtteranceConverter());
            using (var jsonReader = new JsonTextReader(File.OpenText(path)))
            {
                return serializer.Deserialize<T>(jsonReader);
            }
        }

        public static void Write(string path, object value)
        {
            using (var stream = File.Open(path, FileMode.Create))
            {
                Write(stream, value);
            }
        }

        public static void Write(Stream stream, object value)
        {
            var serializer = JsonSerializer.CreateDefault();
            serializer.ContractResolver = new CamelCasePropertyNamesContractResolver();
            serializer.DefaultValueHandling = DefaultValueHandling.Ignore;
            serializer.Converters.Add(new StringEnumConverter());
            serializer.Converters.Add(new ConfusionMatrixConverter());
            serializer.Formatting = Formatting.Indented;
            using (var textWriter = new StreamWriter(stream, Encoding.UTF8, 4096, true))
            {
                serializer.Serialize(textWriter, value);
            }
        }
    }
}
