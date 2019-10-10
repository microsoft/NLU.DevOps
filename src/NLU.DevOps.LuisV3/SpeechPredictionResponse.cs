// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace NLU.DevOps.Luis
{
    using Microsoft.Azure.CognitiveServices.Language.LUIS.Runtime.Models;

    /// <summary>
    /// LUIS response including speech confidence score.
    /// </summary>
    public class SpeechPredictionResponse
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SpeechPredictionResponse"/> class.
        /// </summary>
        /// <param name="predictionResponse">LUIS prediction response.</param>
        /// <param name="textScore">Text score.</param>
        public SpeechPredictionResponse(PredictionResponse predictionResponse, double? textScore)
        {
            this.PredictionResponse = predictionResponse;
            this.TextScore = textScore;
        }

        /// <summary>
        /// Gets the LUIS prediction response.
        /// </summary>
        public PredictionResponse PredictionResponse { get; }

        /// <summary>
        /// Gets the text score.
        /// </summary>
        public double? TextScore { get; }
    }
}