// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace NLU.DevOps.CommandLine
{
    using System;

    /// <summary>
    /// Interface for commands.
    /// </summary>
    internal interface ICommand : IDisposable
    {
        /// <summary>
        /// Command entry point.
        /// </summary>
        /// <returns>Exit code.</returns>
        int Main();
    }
}
