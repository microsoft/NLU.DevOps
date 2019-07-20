// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace NLU.DevOps.ModelPerformance
{
    /// <summary>
    /// Comparison target kind.
    /// </summary>
    public enum ComparisonTargetKind
    {
        /// <summary>
        /// Text.
        /// </summary>
        Text,

        /// <summary>
        /// Intent.
        /// </summary>
        Intent,

        /// <summary>
        /// Entity.
        /// </summary>
        Entity,

        /// <summary>
        /// Entity value.
        /// </summary>
        EntityValue,

        /// <summary>
        /// Entity resolution.
        /// </summary>
        EntityResolution
    }
}
