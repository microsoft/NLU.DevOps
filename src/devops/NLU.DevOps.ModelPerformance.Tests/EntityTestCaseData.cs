// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace NLU.DevOps.ModelPerformance.Tests
{
    using System;
    using Models;

    internal class EntityTestCaseData
    {
        public EntityTestCaseData(
            Entity expectedEntity,
            LabeledUtterance actualUtterance,
            string text,
            string testLabel)
        {
            this.ExpectedEntity = expectedEntity ?? throw new ArgumentNullException(nameof(expectedEntity));
            this.ActualUtterance = actualUtterance ?? throw new ArgumentNullException(nameof(actualUtterance));
            this.Text = text;
            this.TestLabel = testLabel;
        }

        public Entity ExpectedEntity { get; }

        public LabeledUtterance ActualUtterance { get; }

        private string Text { get; }

        private string TestLabel { get; }

        public override string ToString()
        {
            var entityValue = this.ExpectedEntity.MatchText ?? this.ExpectedEntity.EntityValue;
            var testLabelText = this.TestLabel != null ? $"{this.TestLabel}: " : string.Empty;
            return $"{testLabelText}{this.ExpectedEntity.EntityType}, \"{entityValue}\", \"{this.Text}\"";
        }
    }
}
