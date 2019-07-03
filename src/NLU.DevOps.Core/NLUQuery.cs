// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace NLU.DevOps.Core
{
    using System;
    using Models;

    /// <summary>
    /// Default NLU query containing only the text utterance.
    /// </summary>
    public class NLUQuery : INLUQuery
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NLUQuery"/> class.
        /// </summary>
        /// <param name="text">Utterance text.</param>
        public NLUQuery(string text)
        {
            this.Text = text ?? throw new ArgumentNullException(nameof(text));
        }

        /// <summary>
        /// Gets the utterance text.
        /// </summary>
        public string Text { get; }
    }
}
