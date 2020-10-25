// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace NLU.DevOps.ModelPerformance
{
    /// <summary>
    /// Measurement threshold for NLU comparison.
    /// </summary>
    public class NLUThreshold
    {
        /// <summary>
        /// Gets or sets the type of measurement to compare, one of "intent" or "entity".
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Gets or sets the key to compare, one of intent name, entity type, or "*".
        /// </summary>
        public string Group { get; set; }

        /// <summary>
        /// Gets or sets the threshold.
        /// </summary>
        public double Threshold { get; set; }

        /// <summary>
        /// Gets or sets the comparison metric.
        /// </summary>
        public string Metric { get; set; }

        /// <summary>
        /// Gets or sets the comparison type, relative or absolute.
        /// </summary>
        public NLUThresholdKind Comparison { get; set; }
    }
}
