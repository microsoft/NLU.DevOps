// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace NLU.DevOps.ModelPerformance
{
    using System.Collections.Generic;
    using System.Linq;
    using Models;

    internal class TestCase
    {
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

        public string TestName { get; }

        public ConfusionMatrixResultKind ResultKind { get; }

        public ComparisonTargetKind TargetKind { get; }

        public LabeledUtterance ExpectedUtterance { get; }

        public LabeledUtterance ActualUtterance { get; }

        public string Because { get; }

        public List<string> Categories { get; }

        public override string ToString() => this.TestName;
    }
}
