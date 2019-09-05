// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace NLU.DevOps.CommandLine.Tests
{
    using System.IO;
    using System.Linq;
    using FluentAssertions;
    using Newtonsoft.Json.Linq;
    using NUnit.Framework;

    [TestFixture]
    internal class SerializerTests
    {
        [Test]
        public void WriteTrucatesFile()
        {
            var path = Path.GetRandomFileName();
            File.WriteAllText(path, string.Concat(string.Empty, Enumerable.Repeat("!", 1000)));
            Serializer.Write(path, 42);
            var content = File.ReadAllText(path);
            var json = JToken.Parse(content);
            json.Type.Should().Be(JTokenType.Integer);
            json.Value<int>().Should().Be(42);
        }
    }
}
