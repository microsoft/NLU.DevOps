// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace LanguageUnderstanding.Logging
{
    using Microsoft.Extensions.Logging;

    public static class ApplicationLogger
    {
        private static ILoggerFactory s_loggerFactory;

        public static ILoggerFactory LoggerFactory
        {
            get
            {
                if (s_loggerFactory == null)
                {
                    s_loggerFactory = new LoggerFactory();
                }

                return s_loggerFactory;
            }
            set
            {
                s_loggerFactory = value;
            }
        }
    }
}
