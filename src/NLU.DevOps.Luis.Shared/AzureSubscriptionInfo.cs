// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace NLU.DevOps.Luis
{
    internal class AzureSubscriptionInfo
    {
        private AzureSubscriptionInfo(
            string azureSubscriptionId,
            string resourceGroup,
            string accountName)
        {
            this.AzureSubscriptionId = azureSubscriptionId;
            this.ResourceGroup = resourceGroup;
            this.AccountName = accountName;
        }

        public string AzureSubscriptionId { get; }

        public string ResourceGroup { get; }

        public string AccountName { get; }

        public static AzureSubscriptionInfo Create(ILuisConfiguration luisConfiguration)
        {
            if (luisConfiguration.AzureSubscriptionId == null
                || luisConfiguration.AzureResourceGroup == null
                || luisConfiguration.AzureAppName == null
                || luisConfiguration.ArmToken == null)
            {
                return null;
            }

            return new AzureSubscriptionInfo(
                luisConfiguration.AzureSubscriptionId,
                luisConfiguration.AzureResourceGroup,
                luisConfiguration.AzureAppName);
        }
    }
}
