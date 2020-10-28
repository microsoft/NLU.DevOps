// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace NLU.DevOps.Models
{
    using System.Collections.Generic;

    /// <summary>
    /// Labeled utterance.
    /// </summary>
    public interface ILabeledUtterance
    {
        /// <summary>
        /// Gets the text of the utterance.
        /// </summary>
        string Text { get; }

        /// <summary>
        /// Gets the intent of the utterance.
        /// </summary>
        string Intent { get; }

        /// <summary>
        /// Gets the entities referenced in the utterance.
        /// </summary>
        IReadOnlyList<IEntity> Entities { get; }
    }
}
