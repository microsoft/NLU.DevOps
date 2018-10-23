// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace LanguageUnderstanding.Models
{
    /// <summary>
    /// Base class for entity types.
    /// </summary>
    public abstract class EntityType
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EntityType"/> class.
        /// </summary>
        /// <param name="name">Entity type name.</param>
        protected EntityType(string name)
        {
            this.Name = name;
        }

        /// <summary>
        /// Gets the entity type name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the entity type kind.
        /// </summary>
        public abstract EntityTypeKind Kind { get; }
    }
}
