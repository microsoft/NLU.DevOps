// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace NLU.DevOps.ModelPerformance
{
    using System.Collections.Generic;
    using System.Linq;
    using Models;

    /// <summary>
    /// NLU test case.
    /// </summary>
    public class TestCase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TestCase"/> class.
        /// </summary>
        /// <param name="resultKind">Confusion matrix result kind.</param>
        /// <param name="targetKind">Comparison target kind.</param>
        /// <param name="expectedUtterance">Expected utterance.</param>
        /// <param name="actualUtterance">Actual utterance.</param>
        /// <param name="testName">Test name.</param>
        /// <param name="because">Because.</param>
        /// <param name="categories">Categories.</param>
        public TestCase(
            ConfusionMatrixResultKind resultKind,
            ComparisonTargetKind targetKind,
            LabeledUtterance expectedUtterance,
            LabeledUtterance actualUtterance,
            string testName,
            string because,
            IEnumerable<string> categories)
        {
            this.ResultKind = resultKind;
            this.TargetKind = targetKind;
            this.ExpectedUtterance = expectedUtterance;
            this.ActualUtterance = actualUtterance;
            this.TestName = testName;
            this.Because = because;
            this.Categories = categories.ToList();
        }

        /// <summary>
        /// Gets the test name.
        /// </summary>
        public string TestName { get; }

        /// <summary>
        /// Gets the kind of the confusion matrix result.
        /// </summary>
        public ConfusionMatrixResultKind ResultKind { get; }

        /// <summary>
        /// Gets the kind of the comparison target.
        /// </summary>
        public ComparisonTargetKind TargetKind { get; }

        /// <summary>
        /// Gets the expected utterance.
        /// </summary>
        public LabeledUtterance ExpectedUtterance { get; }

        /// <summary>
        /// Gets the actual utterance.
        /// </summary>
        public LabeledUtterance ActualUtterance { get; }

        /// <summary>
        /// Gets the justification.
        /// </summary>
        public string Because { get; }

        /// <summary>
        /// Gets the categories.
        /// </summary>
        public List<string> Categories { get; }
    }
}
