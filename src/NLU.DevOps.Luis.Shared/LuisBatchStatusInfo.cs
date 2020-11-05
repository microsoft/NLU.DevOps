// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace NLU.DevOps.Luis
{
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// LUIS batch evaluation status details.
    /// </summary>
    public class LuisBatchStatusInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LuisBatchStatusInfo"/> class.
        /// </summary>
        /// <param name="status">Evaluation operation status.</param>
        /// <param name="errorDetails">Error details.</param>
        public LuisBatchStatusInfo(string status, JToken errorDetails)
        {
            this.Status = status;
            this.ErrorDetails = errorDetails;
        }

        /// <summary>
        /// Gets the evaluation operation status.
        /// </summary>
        public string Status { get; }

        /// <summary>
        /// Gets the error details.
        /// </summary>
        public string ErrorDetails { get; }
    }
}
