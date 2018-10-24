// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace LanguageUnderstanding.Models
{
    using System.Collections.Generic;

    /// <summary>
    /// Synonym set.
    /// </summary>
    public class SynonymSet
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SynonymSet"/> class.
        /// </summary>
        /// <param name="canonicalForm">Canonical form of the synonym set.</param>
        /// <param name="synonyms">Synonyms of the canonical form.</param>
        public SynonymSet(string canonicalForm, IReadOnlyList<string> synonyms)
        {
            this.CanonicalForm = canonicalForm;
            this.Synonyms = synonyms;
        }

        /// <summary>
        /// Gets the canonical form of the synonym set.
        /// </summary>
        public string CanonicalForm { get; }

        /// <summary>
        /// Gets the synonyms of the canonical form.
        /// </summary>
        public IReadOnlyList<string> Synonyms { get; }
    }
}
