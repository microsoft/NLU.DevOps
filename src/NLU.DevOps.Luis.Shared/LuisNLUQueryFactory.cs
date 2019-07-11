// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace NLU.DevOps.Luis
{
    using System;
    using System.Composition;
    using Core;
    using Models;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Query factory for creating <see cref="LuisNLUQuery"/> instances.
    /// </summary>
    public class LuisNLUQueryFactory : INLUQueryFactory
    {
        /// <inheritdoc />
        public INLUQuery Create(JToken json)
        {
            return json.ToObject<LuisNLUQuery>();
        }

        /// <inheritdoc />
        public INLUQuery Update(INLUQuery query, string utterance)
        {
            if (query == null)
            {
                return new LuisNLUQuery(utterance);
            }

            if (query is LuisNLUQuery luisQuery)
            {
                return luisQuery.Update(utterance);
            }

            throw new ArgumentException($"Expected query of type '{typeof(LuisNLUQuery)}'.", nameof(query));
        }
    }
}
