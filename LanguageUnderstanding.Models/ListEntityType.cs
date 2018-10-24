// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace LanguageUnderstanding.Models
{
    using System.Collections.Generic;

    /// <summary>
    /// List entity type.
    /// </summary>
    public class ListEntityType : EntityType
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ListEntityType"/> class.
        /// </summary>
        /// <param name="name">Entity type name.</param>
        /// <param name="values">List of entity type values.</param>
        public ListEntityType(string name, IReadOnlyList<SynonymSet> values)
            : base(name)
        {
            this.Values = values;
        }

        /// <summary>
        /// Gets the list of entity type values.
        /// </summary>
        /// <value>The values.</value>
        public IReadOnlyList<SynonymSet> Values { get; }

        /// <inheritdoc />
        public override EntityTypeKind Kind => EntityTypeKind.List;
    }
}
