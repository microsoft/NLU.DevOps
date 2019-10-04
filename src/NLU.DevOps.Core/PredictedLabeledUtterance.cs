// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace NLU.DevOps.Core
{
    using System.Collections.Generic;
    using Models;
    using Newtonsoft.Json;

    /// <summary>
    /// Labeled utterance with confidence score.
    /// </summary>
    public class PredictedLabeledUtterance : LabeledUtterance
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PredictedLabeledUtterance"/> class.
        /// </summary>
        /// <param name="text">Text of the utterance.</param>
        /// <param name="intent">Intent of the utterance.</param>
        /// <param name="score">Confidence score for the intent label.</param>
        /// <param name="textScore">Confidence score for speech-to-text.</param>
        /// <param name="entities">Entities referenced in the utterance.</param>
        /// <param name="context">Labeled utterance context.</param>
        [JsonConstructor]
        public PredictedLabeledUtterance(string text, string intent, double score, double textScore, IReadOnlyList<PredictedEntity> entities, LabeledUtteranceContext context)
            : this(text, intent, score, textScore, (IReadOnlyList<Entity>)entities, context)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PredictedLabeledUtterance"/> class.
        /// </summary>
        /// <param name="text">Text of the utterance.</param>
        /// <param name="intent">Intent of the utterance.</param>
        /// <param name="score">Confidence score for the intent label.</param>
        /// <param name="textScore">Confidence score for speech-to-text.</param>
        /// <param name="entities">Entities referenced in the utterance.</param>
        /// <param name="context">Labeled utterance context.</param>
        public PredictedLabeledUtterance(string text, string intent, double score, double textScore, IReadOnlyList<Entity> entities, LabeledUtteranceContext context)
            : base(text, intent, entities)
        {
            this.Context = context;
            this.Score = score;
            this.TextScore = textScore;
        }

        /// <summary>
        /// Gets the context of the labeled utterance.
        /// </summary>
        public LabeledUtteranceContext Context { get; }

        /// <summary>
        /// Gets the confidence score for the intent label.
        /// </summary>
        public double Score { get; }

        /// <summary>
        /// Gets the confidence score for speech-to-text.
        /// </summary>
        public double TextScore { get; }
    }
}
