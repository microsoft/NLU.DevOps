// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace NLU.DevOps.Luis
{
    using Newtonsoft.Json;

    internal class AzureSubscriptionInfo
    {
        private AzureSubscriptionInfo(
            string azureSubscriptionId,
            string resourceGroup,
            string accountName,
            string armToken)
        {
            this.AzureSubscriptionId = azureSubscriptionId;
            this.ResourceGroup = resourceGroup;
            this.AccountName = accountName;
            this.ArmToken = armToken;
        }

        public string AzureSubscriptionId { get; }

        public string ResourceGroup { get; }

        public string AccountName { get; }

        [JsonIgnore]
        public string ArmToken { get; }

        public static AzureSubscriptionInfo Create(
            string azureSubscriptionId,
            string resourceGroup,
            string accountName,
            string armToken)
        {
            if (azureSubscriptionId == null
                || resourceGroup == null
                || accountName == null
                || armToken == null)
            {
                return null;
            }

            return new AzureSubscriptionInfo(azureSubscriptionId, resourceGroup, accountName, armToken);
        }
    }
}
