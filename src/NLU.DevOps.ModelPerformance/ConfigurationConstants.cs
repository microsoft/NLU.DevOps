// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace NLU.DevOps.ModelPerformance
{
    /// <summary>
    /// Configuration constants.
    /// </summary>
    public static class ConfigurationConstants
    {
        /// <summary>
        /// The expected utterances path key.
        /// </summary>
        public const string ExpectedUtterancesPathKey = "expected";

        /// <summary>
        /// The actual utterances path key.
        /// </summary>
        public const string ActualUtterancesPathKey = "actual";

        /// <summary>
        /// A Boolean value that signals whether to generate text comparison tests.
        /// </summary>
        public const string CompareTextKey = "compareText";

        /// <summary>
        /// The test label key.
        /// </summary>
        public const string TestLabelKey = "testLabel";
    }
}
