// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace NLU.DevOps.Core
{
    using System.Collections.Generic;
    using Models;

    /// <summary>
    /// Entity with nested children.
    /// </summary>
    public interface IHierarchicalEntity : IEntity
    {
        /// <summary>
        /// Gets the child entities.
        /// </summary>
        IEnumerable<IHierarchicalEntity> Children { get; }
    }
}
