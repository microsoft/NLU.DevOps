// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace NLU.DevOps.ModelPerformance
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    internal class TestCase
    {
        public TestCase(TestResultKind kind, string message, string because, IEnumerable<string> categories)
        {
            this.Kind = kind;
            this.Message = message;
            this.Because = because;
            this.Categories = categories.ToList();
        }

        public string TestName => this.ToString();

        public TestResultKind Kind { get; }

        public string Because { get; }

        public List<string> Categories { get; }

        internal string Message { get; }

        public override string ToString()
        {
            var testLabelText = TestCaseSource.TestLabel != null ? $"{TestCaseSource.TestLabel}: " : string.Empty;
            return $"{testLabelText}{this.Message}";
        }
    }
}
