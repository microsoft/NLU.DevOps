// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace NLU.DevOps.Core
{
    using System;
    using System.Globalization;

    /// <summary>
    /// Labeled utterance context.
    /// </summary>
    public class LabeledUtteranceContext
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LabeledUtteranceContext"/> class.
        /// </summary>
        /// <param name="timestamp">Timestamp.</param>
        public LabeledUtteranceContext(string timestamp)
        {
            this.Timestamp = timestamp;
        }

        private LabeledUtteranceContext()
            : this(DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture))
        {
        }

        /// <summary>
        /// Gets the timestamp for the labeled utterance.
        /// </summary>
        public string Timestamp { get; }

        /// <summary>
        /// Creates default instance of <see cref="LabeledUtteranceContext"/>.
        /// </summary>
        /// <returns>The default instance.</returns>
        public static LabeledUtteranceContext CreateDefault() => new LabeledUtteranceContext();
    }
}
