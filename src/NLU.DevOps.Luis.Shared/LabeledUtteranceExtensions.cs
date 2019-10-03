// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace NLU.DevOps.Luis
{
    using System;
    using System.Linq;
    using Microsoft.Azure.CognitiveServices.Language.LUIS.Authoring.Models;
    using Models;

    internal static class LabeledUtteranceExtensions
    {
        public static JSONUtterance ToJSONUtterance(this Models.LabeledUtterance utterance, LuisSettings luisSettings)
        {
            JSONEntity toJSONEntity(Entity entity)
            {
                var startPos = entity.StartCharIndexInText(utterance.Text);
                var endPos = startPos + entity.MatchText.Length - 1;
                if (luisSettings.Roles.TryGetValue(entity.EntityType, out var entityType))
                {
                    return new JSONEntityWithRole(startPos, endPos, entityType, entity.EntityType);
                }

                entityType = luisSettings.PrebuiltEntityTypes.TryGetValue(entity.EntityType, out var builtinType)
                    ? builtinType
                    : entity.EntityType;
                return new JSONEntity(startPos, endPos, entityType);
            }

            return new JSONUtterance(
                utterance.Text,
                utterance.Intent,
                utterance.Entities?.Select(toJSONEntity).ToArray() ?? Array.Empty<JSONEntity>());
        }
    }
}
