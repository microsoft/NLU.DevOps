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
        public static JSONUtterance ToJSONUtterance(this Models.LabeledUtterance utterance, LuisApp luisApp)
        {
            JSONEntity toJSONEntity(Entity entity)
            {
                var startPos = entity.StartCharIndexInText(utterance.Text);
                var endPos = startPos + entity.MatchText.Length - 1;
                if (luisApp.TryGetEntityTypeForRole(entity.EntityType, out var entityType))
                {
                    return new JSONEntityWithRole(startPos, endPos, entityType, entity.EntityType);
                }

                return new JSONEntity(startPos, endPos, entity.EntityType);
            }

            return new JSONUtterance(
                utterance.Text,
                utterance.Intent,
                utterance.Entities?.Select(toJSONEntity).ToArray() ?? Array.Empty<JSONEntity>());
        }

        private static bool TryGetEntityTypeForRole(this LuisApp luisApp, string role, out string entityType)
        {
            // Confirm no entity type has given name
            if (luisApp.Entities.Any(e => e.Name == role) ||
                luisApp.PrebuiltEntities.Any(e => e.Name == role) ||
                luisApp.RegexEntities.Any(e => e.Name == role) ||
                luisApp.PatternAnyEntities.Any(e => e.Name == role))
            {
                entityType = null;
                return false;
            }

            // Search for any entity type with given name as role
            var entity = luisApp.Entities.FirstOrDefault(e => e.Roles.Contains(role));
            if (entity != null)
            {
                entityType = entity.Name;
                return true;
            }

            var prebuiltEntity = luisApp.PrebuiltEntities.FirstOrDefault(e => e.Roles.Contains(role));
            if (prebuiltEntity != null)
            {
                entityType = prebuiltEntity.Name;
                return true;
            }

            var regexEntity = luisApp.RegexEntities.FirstOrDefault(e => e.Roles.Contains(role));
            if (regexEntity != null)
            {
                entityType = regexEntity.Name;
                return true;
            }

            var patternAnyEntity = luisApp.PatternAnyEntities.FirstOrDefault(e => e.Roles.Contains(role));
            if (patternAnyEntity != null)
            {
                entityType = patternAnyEntity.Name;
                return true;
            }

            entityType = null;
            return false;
        }
    }
}
