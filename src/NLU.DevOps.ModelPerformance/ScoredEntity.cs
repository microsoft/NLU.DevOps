// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace NLU.DevOps.ModelPerformance
{
    using Models;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Entity appearing in utterance with confidence score.
    /// </summary>
    public class ScoredEntity : Entity
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ScoredEntity"/> class.
        /// </summary>
        /// <param name="entityType">Entity type name.</param>
        /// <param name="entityValue">Entity value, generally a canonical form of the entity.</param>
        /// <param name="matchText">Matching text in the utterance.</param>
        /// <param name="matchIndex">Occurrence index of matching token in the utterance.</param>
        /// <param name="score">Confidence score for the entity.</param>
        public ScoredEntity(string entityType, JToken entityValue, string matchText, int matchIndex, double score)
            : base(entityType, entityValue, matchText, matchIndex)
        {
            this.Score = score;
        }

        /// <summary>
        /// Gets the confidence score for the entity.
        /// </summary>
        public double Score { get; }
    }
}
