// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace NLU.DevOps.CommandLine
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    internal static class EnumerableExtensions
    {
        public static async Task<IEnumerable<TResult>> SelectAsync<T, TResult>(this IEnumerable<T> items, Func<T, Task<TResult>> selector, int degreeOfParallelism)
        {
            var indexedItems = items.Select((item, i) => new { Item = item, Index = i });
            var results = new TResult[items.Count()];
            var tasks = new List<Task<Tuple<int, TResult>>>(degreeOfParallelism);

            async Task<Tuple<int, TResult>> selectWithIndexAsync(T item, int i)
            {
                var result = await selector(item).ConfigureAwait(false);
                return Tuple.Create(i, result);
            }

            foreach (var indexedItem in indexedItems)
            {
                if (tasks.Count == degreeOfParallelism)
                {
                    var task = await Task.WhenAny(tasks).ConfigureAwait(false);
                    tasks.Remove(task);
                    var result = await task.ConfigureAwait(false);
                    results[/* (int) */ result.Item1] = /* (TResult) */ result.Item2;
                }

                tasks.Add(selectWithIndexAsync(indexedItem.Item, indexedItem.Index));
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);
            foreach (var task in tasks)
            {
                var result = await task.ConfigureAwait(false);
                results[/* (int) */ result.Item1] = /* (TResult) */ result.Item2;
            }

            return results;
        }
    }
}
