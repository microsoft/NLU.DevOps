// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace NLU.DevOps.Luis
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Azure.CognitiveServices.Language.LUIS.Authoring.Models;
    using Models;

    internal static class LabeledUtteranceExtensions
    {
        public static JSONUtterance ToJSONUtterance(this Models.LabeledUtterance utterance, IReadOnlyDictionary<string, string> prebuiltEntityTypes)
        {
            JSONEntity toJSONEntity(Entity entity)
            {
                var entityType = prebuiltEntityTypes.TryGetValue(entity.EntityType, out var builtinType)
                    ? builtinType
                    : entity.EntityType;
                var startPos = entity.StartCharIndexInText(utterance.Text);
                return new JSONEntity(startPos, startPos + entity.MatchText.Length - 1, entityType);
            }

            return new JSONUtterance(
                utterance.Text,
                utterance.Intent,
                utterance.Entities?.Select(toJSONEntity).ToArray() ?? Array.Empty<JSONEntity>());
        }
    }
}
