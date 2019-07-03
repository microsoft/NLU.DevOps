// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace NLU.DevOps.Core
{
    using Models;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Factory for creating NLU queries from JSON.
    /// </summary>
    public interface INLUQueryFactory
    {
        /// <summary>
        /// Create an NLU query from the given JSON.
        /// </summary>
        /// <returns>NLU query.</returns>
        /// <param name="json">JSON query data.</param>
        INLUQuery Create(JToken json);

        /// <summary>
        /// Update the specified query with the given utterance.
        /// </summary>
        /// <returns>The updated query.</returns>
        /// <param name="query">NLU query.</param>
        /// <param name="utterance">Utterance.</param>
        INLUQuery Update(INLUQuery query, string utterance);
    }
}
