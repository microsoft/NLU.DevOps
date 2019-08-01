// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace NLU.DevOps.ModelPerformance
{
    using System.Collections.Generic;
    using System.Linq;
    using Models;

    /// <summary>
    /// Labeled utterance with confidence score.
    /// </summary>
    public class ScoredLabeledUtterance : LabeledUtterance
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ScoredLabeledUtterance"/> class.
        /// </summary>
        /// <param name="text">Text of the utterance.</param>
        /// <param name="intent">Intent of the utterance.</param>
        /// <param name="score">Confidence score for the intent label.</param>
        /// <param name="textScore">Confidence score for speech-to-text.</param>
        /// <param name="entities">Entities referenced in the utterance.</param>
        public ScoredLabeledUtterance(string text, string intent, double score, double textScore, IReadOnlyList<ScoredEntity> entities)
            : base(text, intent, entities)
        {
            this.Score = score;
            this.TextScore = textScore;
        }

        /// <summary>
        /// Gets the confidence score for the intent label.
        /// </summary>
        public double Score { get; }

        /// <summary>
        /// Gets the confidence score for speech-to-text.
        /// </summary>
        public double TextScore { get; }

        /// <summary>
        /// Gets the entities referenced in the utterance.
        /// </summary>
        public new IReadOnlyList<ScoredEntity> Entities => base.Entities.OfType<ScoredEntity>().ToList();
    }
}
