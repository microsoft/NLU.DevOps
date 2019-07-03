// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace NLU.DevOps.Luis
{
    using System;
    using System.Composition;
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
        {
            this.Text = text ?? throw new ArgumentNullException(nameof(text));
        }

        /// <summary>
        /// Gets the query text.
        /// </summary>
        public string Text { get; }

        /// <summary>
        /// Updates the query to use the specified utterance text.
        /// </summary>
        /// <returns>The updated query.</returns>
        /// <param name="text">Utterance text.</param>
        public LuisNLUQuery Update(string text)
        {
            if (this.Text == text)
            {
                return this;
            }

            return new LuisNLUQuery(text);
        }
    }
}
