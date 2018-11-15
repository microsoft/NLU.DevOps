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
            if (this.AppName == null)
            {
                throw new InvalidOperationException("Must provide app name for LUIS.");
            }

            if (this.AuthoringRegion == null && this.EndpointRegion == null)
            {
                throw new InvalidOperationException("Must specify either authoring or endpoint region.");
            }

            if (this.LuisClient == null && this.AuthoringKey == null && this.EndpointKey == null)
            {
                throw new InvalidOperationException("Must provide at least one subscription key.");
            }

            this.EndpointRegion = this.EndpointRegion ?? this.AuthoringRegion;
            this.EndpointKey = this.EndpointKey ?? this.AuthoringKey;
            this.LuisClient = this.LuisClient ?? new LuisClient(this.AuthoringKey, this.EndpointKey, this.EndpointRegion);

            return new LuisLanguageUnderstandingService(
                this.AppName,
                this.AppId,
                this.AppVersion,
                this.IsStaging,
                this.AuthoringRegion,
                this.EndpointRegion,
                this.LuisClient);
        }
    }
}
