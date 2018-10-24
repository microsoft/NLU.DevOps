// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace LanguageUnderstanding.Models
{
    /// <summary>
    /// Simple entity type.
    /// </summary>
    public class SimpleEntityType : EntityType
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SimpleEntityType"/> class.
        /// </summary>
        /// <param name="name">Entity type name.</param>
        public SimpleEntityType(string name)
            : base(name)
        {
        }

        /// <inheritdoc />
        public override EntityTypeKind Kind => EntityTypeKind.Simple;
    }
}
