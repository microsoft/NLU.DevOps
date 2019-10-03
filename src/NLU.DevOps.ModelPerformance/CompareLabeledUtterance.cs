// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace NLU.DevOps.ModelPerformance
{
    using System.Collections.Generic;
    using Models;

    /// <summary>
    /// Labeled utterance with confidence score.
    /// </summary>
    public class CompareLabeledUtterance : LabeledUtterance
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CompareLabeledUtterance"/> class.
        /// </summary>
        /// <param name="utteranceId">Utterance ID.</param>
        /// <param name="text">Text of the utterance.</param>
        /// <param name="intent">Intent of the utterance.</param>
        /// <param name="entities">Entities referenced in the utterance.</param>
        public CompareLabeledUtterance(string utteranceId, string text, string intent, IReadOnlyList<Entity> entities)
            : base(text, intent, entities)
        {
            this.UtteranceId = utteranceId;
        }

        /// <summary>
        /// Gets the utterance ID.
        /// </summary>
        public string UtteranceId { get; }
    }
}
