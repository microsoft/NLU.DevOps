// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace NLU.DevOps.ModelPerformance
{
    using Newtonsoft.Json;

    /// <summary>
    /// Confusion matrix container.
    /// </summary>
    [JsonConverter(typeof(ConfusionMatrixConverter))]
    public class ConfusionMatrix
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ConfusionMatrix"/> class.
        /// </summary>
        /// <param name="truePositive">True positive count.</param>
        /// <param name="trueNegative">True negative count.</param>
        /// <param name="falsePositive">False positive count.</param>
        /// <param name="falseNegative">False negative count.</param>
        public ConfusionMatrix(
            int truePositive,
            int trueNegative,
            int falsePositive,
            int falseNegative)
        {
            this.TruePositive = truePositive;
            this.TrueNegative = trueNegative;
            this.FalsePositive = falsePositive;
            this.FalseNegative = falseNegative;
        }

        /// <summary>
        /// Gets the true positive count.
        /// </summary>
        public int TruePositive { get; }

        /// <summary>
        /// Gets the true negative count.
        /// </summary>
        public int TrueNegative { get; }

        /// <summary>
        /// Gets the false positive count.
        /// </summary>
        public int FalsePositive { get; }

        /// <summary>
        /// Gets the false negative count.
        /// </summary>
        public int FalseNegative { get; }
    }
}
