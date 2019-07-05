// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace NLU.DevOps.Luis
{
    using Microsoft.Azure.CognitiveServices.Language.LUIS.Runtime.Models;
    using Models;

    /// <summary>
    /// Query for LUIS tests.
    /// </summary>
    public sealed class LuisNLUQuery : INLUQuery
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LuisNLUQuery"/> class.
        /// </summary>
        /// <param name="text">Query text.</param>
        public LuisNLUQuery(string text)
            : this(new PredictionRequest { Query = text })
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LuisNLUQuery"/> class.
        /// </summary>
        /// <param name="predictionRequest">Prediction request.</param>
        public LuisNLUQuery(PredictionRequest predictionRequest)
        {
            this.PredictionRequest = predictionRequest;
        }

        /// <summary>
        /// Gets the prediction request.
        /// </summary>
        public PredictionRequest PredictionRequest { get; }

        /// <summary>
        /// Updates the query to use the specified utterance text.
        /// </summary>
        /// <returns>The updated query.</returns>
        /// <param name="text">Utterance text.</param>
        public LuisNLUQuery Update(string text)
        {
            if (this.PredictionRequest.Query == text)
            {
                return this;
            }

            return new LuisNLUQuery(new PredictionRequest
            {
                Query = text,
                DynamicLists = this.PredictionRequest.DynamicLists,
                ExternalEntities = this.PredictionRequest.ExternalEntities,
                Options = this.PredictionRequest.Options,
            });
        }
    }
}
