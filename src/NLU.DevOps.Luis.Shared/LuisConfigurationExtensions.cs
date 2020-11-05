// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace NLU.DevOps.Luis
{
    internal static class LuisConfigurationExtensions
    {
        public static string GetBatchEvaluationEndpoint(this ILuisConfiguration luisConfiguration)
        {
            var baseEndpoint = $"{luisConfiguration.BatchEndpoint}api/v3.0/apps/";
            if (luisConfiguration.DirectVersionPublish)
            {
                return $"{baseEndpoint}{luisConfiguration.AppId}/versions/{luisConfiguration.VersionId}/evaluations";
            }

            return $"{baseEndpoint}{luisConfiguration.AppId}/slots/{luisConfiguration.SlotName}/evaluations";
        }

        public static string GetBatchStatusEndpoint(this ILuisConfiguration luisConfiguration, string operationId)
        {
            return $"{luisConfiguration.GetBatchEvaluationEndpoint()}/{operationId}/status";
        }

        public static string GetBatchResultEndpoint(this ILuisConfiguration luisConfiguration, string operationId)
        {
            return $"{luisConfiguration.GetBatchEvaluationEndpoint()}/{operationId}/result";
        }
    }
}
