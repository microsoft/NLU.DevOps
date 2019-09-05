// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace NLU.DevOps.Models
{
    using System;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Extension methods for entities.
    /// </summary>
    public static class EntityExtensions
    {
        /// <summary>
        /// Gets the start <code>char</code> index of the <paramref name="entity"/> in the <paramref name="text"/>.
        /// </summary>
        /// <returns>The start <code>char</code> index.</returns>
        /// <param name="entity">Entity.</param>
        /// <param name="text">Text.</param>
        public static int StartCharIndexInText(this Entity entity, string text)
        {
            var match = Regex.Match(text, entity.MatchText);
            for (var i = 0; i < entity.MatchIndex; ++i)
            {
                match = match.NextMatch();
            }

            if (!match.Success)
            {
                throw new InvalidOperationException("Unable to find matching entity.");
            }

            return match.Index;
        }
    }
}
