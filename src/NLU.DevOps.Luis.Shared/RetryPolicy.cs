// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace NLU.DevOps.Luis
{
    using System;
    using Microsoft.Rest.TransientFaultHandling;

    /// <summary>
    /// Retry policies for LUIS.
    /// </summary>
    public static class RetryPolicy
    {
        /// <summary>
        /// Gets the train retry policy.
        /// </summary>
        public static RetryPolicy<HttpStatusCodeErrorDetectionStrategy> TrainPolicy =>
            new RetryPolicy<HttpStatusCodeErrorDetectionStrategy>(
                int.MaxValue,
                TrainDelay);

        /// <summary>
        /// Gets the test retry policy.
        /// </summary>
        public static RetryPolicy<HttpStatusCodeErrorDetectionStrategy> TestPolicy =>
            new RetryPolicy<HttpStatusCodeErrorDetectionStrategy>(
                int.MaxValue,
                TestDelay);

        private static TimeSpan TrainDelay => TimeSpan.FromSeconds(2);

        private static TimeSpan TestDelay => TimeSpan.FromMilliseconds(100);
    }
}