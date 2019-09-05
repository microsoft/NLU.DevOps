// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace NLU.DevOps.ModelPerformance
{
    using NUnit.Framework;

    [TestFixture]
    [Parallelizable(ParallelScope.All)]
    internal static class ConfusionMatrixTests
    {
        [Test]
        [TestCaseSource(typeof(TestCaseSource), "PassingTests")]
        public static void Pass(string because)
        {
            Assert.Pass(because);
        }

        [Test]
        [TestCaseSource(typeof(TestCaseSource), "FailingTests")]
        public static void Fail(string because)
        {
            Assert.Fail(because);
        }
    }
}
