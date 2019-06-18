// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace NLU.DevOps.Luis
{
    using System;

    /// <summary>
    /// Interface for LUIS operations.
    /// </summary>
    public interface ILuisClient : ILuisTrainingClient, ILuisPredictionClient, IDisposable
    {
    }
}
