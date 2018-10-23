// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace LanguageUnderstanding.Models
{
    /// <summary>
    /// Built-in entity type.
    /// </summary>
    public class BuiltinEntityType : EntityType
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BuiltinEntityType"/> class.
        /// </summary>
        /// <param name="name">Entity type name.</param>
        /// <param name="builtinId">Built-in entity type identifier.</param>
        public BuiltinEntityType(string name, string builtinId)
            : base(name)
        {
            this.BuiltinId = builtinId;
        }

        /// <summary>
        /// Gets the built-in entity type identifier.
        /// </summary>
        public string BuiltinId { get; }

        /// <inheritdoc />
        public override EntityTypeKind Kind => EntityTypeKind.Builtin;
    }
}
