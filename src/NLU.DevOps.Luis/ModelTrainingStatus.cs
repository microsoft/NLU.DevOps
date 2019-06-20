// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace NLU.DevOps.Luis
{
#pragma warning disable CA1717 // Only FlagsAttribute enums should have plural names
    /// <summary>
    /// Status for Model training
    /// </summary>
    public enum ModelTrainingStatus
#pragma warning restore CA1717 // Only FlagsAttribute enums should have plural names
    {
        /// <summary>
        /// Indicates there was a failure training the model
        /// </summary>
        Fail,

        /// <summary>
        /// Indicates that the model is still being trained or queued for training
        /// </summary>
        InProgress,

        /// <summary>
        /// Indicates successful training of the model
        /// </summary>
        Success
    }
}