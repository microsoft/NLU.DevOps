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
        /// Default <see cref="ConfusionMatrixResultKind"/>.
        /// </summary>
        None,

        /// <summary>
        /// True positive.
        /// </summary>
        TruePositive = 1,

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
