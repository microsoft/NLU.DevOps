// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace LanguageUnderstanding.Luis
{
    using System;

    /// <summary>
    /// Builder for <see cref="LuisLanguageUnderstandingService"/>.
    /// </summary>
    public class LuisLanguageUnderstandingServiceBuilder
    {
        /// <summary>
        /// Gets or sets the name of the LUIS app.
        /// </summary>
        public string AppName { get; set; }

        /// <summary>
        /// Gets or sets the LUIS app ID.
        /// </summary>
        public string AppId { get; set; }

        /// <summary>
        /// Gets or sets the LUIS app version.
        /// </summary>
        public string AppVersion { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the LUIS app is staging.
        /// </summary>
        public bool IsStaging { get; set; }

        /// <summary>
        /// Gets or sets the LUIS authoring region.
        /// </summary>
        public string AuthoringRegion { get; set; }

        /// <summary>
        /// Gets or sets the LUIS endpoint region.
        /// </summary>
        public string EndpointRegion { get; set; }

        /// <summary>
        /// Gets or sets the LUIS authoring key.
        /// </summary>
        public string AuthoringKey { get; set; }

        /// <summary>
        /// Gets or sets the LUIS endpoint key.
        /// </summary>
        /// <value>The endpoint key.</value>
        public string EndpointKey { get; set; }

        /// <summary>
        /// Gets or sets the client to make requests to LUIS.
        /// </summary>
        public ILuisClient LuisClient { get; set; }

        /// <summary>
        /// Build this LUIS client.
        /// </summary>
        /// <returns>The LUIS client.</returns>
        public LuisLanguageUnderstandingService Build()
        {
            this.LuisClient = this.LuisClient ??
                new LuisClient(
                    this.AuthoringKey,
                    this.AuthoringRegion,
                    this.EndpointKey,
                    this.EndpointRegion,
                    this.IsStaging);

            return new LuisLanguageUnderstandingService(
                this.AppName,
                this.AppId,
                this.AppVersion,
                this.LuisClient);
        }
    }
}
