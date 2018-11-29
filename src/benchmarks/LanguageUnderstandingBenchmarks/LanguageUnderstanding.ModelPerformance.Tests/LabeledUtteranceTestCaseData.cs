// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace LanguageUnderstanding.ModelPerformance.Tests
{
    using System;
    using Models;

    internal class LabeledUtteranceTestCaseData
    {
        public LabeledUtteranceTestCaseData(
            LabeledUtterance expectedUtterance,
            LabeledUtterance actualUtterance,
            string testLabel)
        {
            this.ExpectedUtterance = expectedUtterance ?? throw new ArgumentNullException(nameof(expectedUtterance));
            this.ActualUtterance = actualUtterance ?? throw new ArgumentNullException(nameof(actualUtterance));
            this.TestLabel = testLabel;
        }

        public LabeledUtterance ActualUtterance { get; }

        public LabeledUtterance ExpectedUtterance { get; }

        /// <summary>
        /// Gets the test label.
        /// </summary>
        /// <remarks>
        /// The test label is useful for discriminating between tests for audio and text.
        /// </remarks>
        public string TestLabel { get; }

        public override string ToString()
        {
            var testLabelText = this.TestLabel != null ? $"{this.TestLabel}: " : string.Empty;
            return $"{testLabelText}\"{this.ExpectedUtterance.Text}\"";
        }
    }
}
