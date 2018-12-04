// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace NLU.DevOps.Luis
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Azure.CognitiveServices.Language.LUIS.Authoring.Models;
    using NLU.DevOps.Models;

    internal static class LabeledUtteranceExtensions
    {
        public static JSONUtterance ToJSONUtterance(this Models.LabeledUtterance utterance, IEnumerable<EntityType> entityTypes, LuisApp luisApp)
        {
            JSONEntity toJSONEntity(Entity entity)
            {
                var startPos = entity.StartCharIndexInText(utterance.Text);
                return new JSONEntity(startPos, startPos + entity.MatchText.Length - 1, entity.EntityType);
            }

            var entities = from entity in utterance.Entities ?? Array.Empty<Entity>()
                           where !entityTypes.Any(e => e.Kind == "prebuiltEntities" && e.Name == entity.EntityType)
                           where !luisApp.PrebuiltEntities.Any(p => p.Name == entity.EntityType)
                           select toJSONEntity(entity);

            return new JSONUtterance(
                utterance.Text,
                utterance.Intent,
                entities.ToArray());
        }
    }
}
