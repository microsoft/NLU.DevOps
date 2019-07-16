// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace NLU.DevOps.Core
{
    using System;

    /// <summary>
    /// Default NLU test query.
    /// </summary>
    public class DefaultQuery
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultQuery"/> class.
        /// </summary>
        /// <param name="text">Utterance text.</param>
        public DefaultQuery(string text)
        {
            this.Text = text ?? throw new ArgumentNullException(nameof(text));
        }

        /// <summary>
        /// Gets the text.
        /// </summary>
        public string Text { get; }
    }
}
