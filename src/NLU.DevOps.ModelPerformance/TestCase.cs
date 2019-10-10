// Copyright (c) Microsoft Corporation.
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
        /// <param name="utteranceId">Utterance ID.</param>
        /// <param name="resultKind">Confusion matrix result kind.</param>
        /// <param name="targetKind">Comparison target kind.</param>
        /// <param name="expectedUtterance">Expected utterance.</param>
        /// <param name="actualUtterance">Actual utterance.</param>
        /// <param name="score">Confidence score for test case result.</param>
        /// <param name="group">Test case group name.</param>
        /// <param name="testName">Test name.</param>
        /// <param name="because">Because.</param>
        /// <param name="categories">Categories.</param>
        public TestCase(
            string utteranceId,
            ConfusionMatrixResultKind resultKind,
            ComparisonTargetKind targetKind,
            LabeledUtterance expectedUtterance,
            LabeledUtterance actualUtterance,
            double? score,
            string group,
            string testName,
            string because,
            IEnumerable<string> categories)
        {
            this.UtteranceId = utteranceId;
            this.ResultKind = resultKind;
            this.TargetKind = targetKind;
            this.ExpectedUtterance = expectedUtterance;
            this.ActualUtterance = actualUtterance;
            this.Score = score;
            this.Group = group;
            this.TestName = testName;
            this.Because = because;
            this.Categories = categories.ToList();
        }

        /// <summary>
        /// Gets the utterance ID.
        /// </summary>
        public string UtteranceId { get; }

        /// <summary>
        /// Gets the test name.
        /// </summary>
        public string TestName { get; }

        /// <summary>
        /// Gets the test case group name.
        /// </summary>
        public string Group { get; }

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
        /// Gets the confidence score.
        /// </summary>
        public double? Score { get; }

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
