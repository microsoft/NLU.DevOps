// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace NLU.DevOps.Luis
{
    using System;
    using System.Composition;
    using Core;
    using Microsoft.Azure.CognitiveServices.Language.LUIS.Runtime.Models;
    using Models;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Query factory for creating <see cref="LuisNLUQuery"/> instances.
    /// </summary>
    [Export("luisV3", typeof(INLUQueryFactory))]
    public class LuisNLUQueryFactory : INLUQueryFactory
    {
        /// <inheritdoc />
        public INLUQuery Create(JToken json)
        {
            var text = json.Value<string>("text");
            var predictionRequest = json.ToObject<PredictionRequest>();
            predictionRequest.Query = text ?? predictionRequest.Query;

            // TODO: improve exception
            if (predictionRequest.Query == null)
            {
                throw new ArgumentException("Expected 'text' or 'query'.");
            }

            return new LuisNLUQuery(predictionRequest);
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
