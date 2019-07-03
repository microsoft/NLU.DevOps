// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace NLU.DevOps.Core
{
    using System;
    using Models;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Default NLU query factory.
    /// </summary>
    public sealed class DefaultNLUQueryFactory : INLUQueryFactory
    {
        private DefaultNLUQueryFactory()
        {
        }

        /// <summary>
        /// Gets the default instance for <see cref="DefaultNLUQueryFactory"/>.
        /// </summary>
        public static DefaultNLUQueryFactory Instance { get; } = new DefaultNLUQueryFactory();

        /// <inheritdoc />
        public INLUQuery Create(JToken json)
        {
            return json.ToObject<NLUQuery>();
        }

        /// <inheritdoc />
        public INLUQuery Update(INLUQuery query, string utterance)
        {
            if (query == null || query is NLUQuery)
            {
                return new NLUQuery(utterance);
            }

            throw new ArgumentException($"Expected query of type '{typeof(NLUQuery)}'.", nameof(query));
        }
    }
}
