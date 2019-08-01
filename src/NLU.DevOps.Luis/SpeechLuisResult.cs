// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace NLU.DevOps.Luis
{
    using Microsoft.Azure.CognitiveServices.Language.LUIS.Runtime.Models;

    /// <summary>
    /// LUIS response including speech confidence score.
    /// </summary>
    public class SpeechLuisResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SpeechLuisResult"/> class.
        /// </summary>
        /// <param name="luisResult">Luis result.</param>
        /// <param name="textScore">Text score.</param>
        public SpeechLuisResult(LuisResult luisResult, double textScore)
        {
            this.LuisResult = luisResult;
            this.TextScore = textScore;
        }

        /// <summary>
        /// Gets the LUIS result.
        /// </summary>
        public LuisResult LuisResult { get; }

        /// <summary>
        /// Gets the text score.
        /// </summary>
        public double TextScore { get; }
    }
}