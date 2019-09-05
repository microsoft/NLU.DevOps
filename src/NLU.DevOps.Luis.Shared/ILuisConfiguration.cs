// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace NLU.DevOps.Luis
{
    using System;

    /// <summary>
    /// LUIS configuration.
    /// </summary>
    public interface ILuisConfiguration
    {
        /// <summary>
        /// Gets the LUIS app ID.
        /// </summary>
        string AppId { get; }

        /// <summary>
        /// Gets the LUIS app name.
        /// </summary>
        string AppName { get; }

        /// <summary>
        /// Gets the LUIS authoring key.
        /// </summary>
        string AuthoringKey { get; }

        /// <summary>
        /// Gets the LUIS authoring region.
        /// </summary>
        string AuthoringRegion { get; }

        /// <summary>
        /// Gets the LUIS endpoint key.
        /// </summary>
        string EndpointKey { get; }

        /// <summary>
        /// Gets the LUIS endpoint region.
        /// </summary>
        string EndpointRegion { get; }

        /// <summary>
        /// Gets the LUIS version ID.
        /// </summary>
        string VersionId { get; }

        /// <summary>
        /// Gets a value indicating whether the LUIS model should be published to the staging slot.
        /// </summary>
        bool IsStaging { get; }

        /// <summary>
        /// Gets the Cognitive Services speech key.
        /// </summary>
        string SpeechKey { get; }

        /// <summary>
        /// Gets the Cognitive Services speech region.
        /// </summary>
        string SpeechRegion { get; }

        /// <summary>
        /// Gets the Cognitive Services speech endpoint.
        /// </summary>
        Uri SpeechEndpoint { get; }
#if LUIS_V2

        /// <summary>
        /// Gets a value indicating whether the REST speech endpoint should be used as opposed to the Speech SDK.
        /// </summary>
        bool UseSpeechEndpoint { get; }
#endif
#if LUIS_V3

        /// <summary>
        /// Gets the LUIS staging name.
        /// </summary>
        string SlotName { get; }

        /// <summary>
        /// Gets a value indicating whether the LUIS model should use direct version publish.
        /// </summary>
        bool DirectVersionPublish { get; }
#endif

        /// <summary>
        /// Gets the Azure resource group.
        /// </summary>
        string AzureResourceGroup { get; }

        /// <summary>
        /// Gets the Azure subscription ID.
        /// </summary>
        string AzureSubscriptionId { get; }

        /// <summary>
        /// Gets the Azure Cognitive Services app name.
        /// </summary>
        string AzureAppName { get; }

        /// <summary>
        /// Gets the ARM token.
        /// </summary>
        string ArmToken { get; }
    }
}
