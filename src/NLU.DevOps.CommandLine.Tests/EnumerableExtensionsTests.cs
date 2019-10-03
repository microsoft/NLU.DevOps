// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace NLU.DevOps.CommandLine.Tests
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using FluentAssertions;
    using NUnit.Framework;

    [TestFixture]
    internal static class EnumerableExtensionsTests
    {
        [Test]
        public static async Task LimitsQueriesPerSecond()
        {
            Task<Tuple<int, DateTimeOffset>> TimestampAsync(int item) =>
                Task.FromResult(Tuple.Create(item, DateTimeOffset.Now));

            var items = Enumerable.Range(0, 101).ToList();
            var results = await items.SelectAsync(TimestampAsync, 10, 50).ConfigureAwait(false);

            results.Select(x => x.Item1).Should().BeEquivalentTo(items);

            for (var i = 51; i < items.Count; ++i)
            {
                var delay = results[i].Item2 - results[i - 50].Item2;
                delay.Should().BeGreaterThan(TimeSpan.FromSeconds(1));
            }
        }
    }
}
