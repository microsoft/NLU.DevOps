// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace NLU.DevOps.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    /// <summary>
    /// Extensions to the <see cref="IEnumerable{T}"/> interface.
    /// </summary>
    public static class EnumerableExtensions
    {
        /// <summary>
        /// Creates a higher order <see cref="IEnumerable{T}"/> of batches of items.
        /// </summary>
        /// <typeparam name="T">Element type.</typeparam>
        /// <param name="items">Collection of items.</param>
        /// <param name="batchSize">Batch size.</param>
        /// <returns>Batched collection.</returns>
        public static IEnumerable<IEnumerable<T>> Batch<T>(this IEnumerable<T> items, int batchSize)
        {
            var skip = 0;
            var batch = items.Skip(skip).Take(batchSize);
            while (batch.Any())
            {
                yield return batch;
                skip += batchSize;
                batch = items.Skip(skip).Take(batchSize);
            }
        }

        /// <summary>
        /// Runs map operations in parallel.
        /// </summary>
        /// <typeparam name="T">Element type.</typeparam>
        /// <typeparam name="TResult">Result type.</typeparam>
        /// <param name="items">Collection of items.</param>
        /// <param name="selector">Map operation.</param>
        /// <param name="degreeOfParallelism">Degree of parallelism.</param>
        /// <returns>A task to await the mapped collection.</returns>
        public static async Task<IEnumerable<TResult>> SelectAsync<T, TResult>(this IEnumerable<T> items, Func<T, Task<TResult>> selector, int degreeOfParallelism)
        {
            if (degreeOfParallelism < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(degreeOfParallelism), "Value must be greater than zero.");
            }

            var indexedItems = items.Select((item, i) => new { Item = item, Index = i });
            var results = new List<TResult>();
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

                results.Add(default);
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
