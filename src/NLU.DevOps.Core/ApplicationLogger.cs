// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace NLU.DevOps.Logging
{
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Application logger for NLU providers.
    /// </summary>
    public static class ApplicationLogger
    {
        private static ILoggerFactory loggingFactory;

        /// <summary>
        /// Gets or sets the logger factory used by NLU providers to create logger.
        /// </summary>
        public static ILoggerFactory LoggerFactory
        {
            get
            {
                if (loggingFactory == null)
                {
                    loggingFactory = new LoggerFactory();
                }

                return loggingFactory;
            }

            set
            {
                loggingFactory = value;
            }
        }
    }
}
