// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace NLU.DevOps.CommandLine
{
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Interface for commands.
    /// </summary>
    internal interface ICommand : IDisposable
    {
        /// <summary>
        /// Command entry point.
        /// </summary>
        /// <returns>Exit code.</returns>
        Task<int> RunAsync();
    }
}
