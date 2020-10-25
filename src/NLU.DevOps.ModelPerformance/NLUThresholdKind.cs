// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace NLU.DevOps.ModelPerformance
{
    /// <summary>
    /// Comparison types, either relative or absolute.
    /// </summary>
    public enum NLUThresholdKind
    {
        /// <summary>
        /// Relative threshold.
        /// </summary>
        Relative = 0,

        /// <summary>
        /// Absolute threshold.
        /// </summary>
        Absolute,
    }
}
