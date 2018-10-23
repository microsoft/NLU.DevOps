// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace LanguageUnderstanding.Models
{
    /// <summary>
    /// Enumeration of entity type kinds
    /// </summary>
    public enum EntityTypeKind
    {
        /// <summary>
        /// Simple entity type.
        /// </summary>
        Simple,

        /// <summary>
        /// Built-in entity type.
        /// </summary>
        Builtin,

        /// <summary>
        /// List entity type.
        /// </summary>
        List,
    }
}
