// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace NLU.DevOps.Core
{
    using System.Collections.Generic;
    using Newtonsoft.Json;

    /// <summary>
    /// Internal interface for manipulating properties from <see cref="JsonExtensionDataAttribute"/>.
    /// </summary>
    internal interface IJsonExtension
    {
        /// <summary>
        /// Gets the additional properties.
        /// </summary>
        IDictionary<string, object> AdditionalProperties { get; }
    }
}
