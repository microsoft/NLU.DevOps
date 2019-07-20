// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace NLU.DevOps.ModelPerformance
{
    /// <summary>
    /// Enumeration of confusion matrix result types.
    /// </summary>
    public enum ConfusionMatrixResultKind
    {
        /// <summary>
        /// True positive.
        /// </summary>
        TruePositive,

        /// <summary>
        /// True negative.
        /// </summary>
        TrueNegative,

        /// <summary>
        /// False positive.
        /// </summary>
        FalsePositive,

        /// <summary>
        /// False negative.
        /// </summary>
        FalseNegative,
    }
}
