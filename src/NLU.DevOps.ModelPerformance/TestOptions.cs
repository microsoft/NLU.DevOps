// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace NLU.DevOps.ModelPerformance
{
    /// <summary>
    /// Test options for NLU results validation.
    /// </summary>
    public class TestOptions
    {
        /// <summary>
        /// Gets or sets a value indicating whether strict mode is enabled.
        /// </summary>
        /// <remarks>
        /// When <code>true</code>, always generates false
        /// positive test results for unexpected entities.
        /// </remarks>
        public bool Strict { get; set; }

        /// <summary>
        /// Gets or sets the true negative intent name.
        /// </summary>
        public string TrueNegativeIntent { get; set; }
    }
}