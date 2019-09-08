// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace NLU.DevOps.Luis
{
    using Microsoft.Azure.CognitiveServices.Language.LUIS.Authoring.Models;
    using Newtonsoft.Json;

    /// <summary>
    /// <see cref="JSONEntity"/> with additional role property.
    /// </summary>
    public class JSONEntityWithRole : JSONEntity
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="JSONEntityWithRole"/> class.
        /// </summary>
        /// <param name="startPos">Start position of the entity.</param>
        /// <param name="endPos">End position of the entity.</param>
        /// <param name="entity">Entity type.</param>
        /// <param name="role">Entity role.</param>
        public JSONEntityWithRole(int startPos, int endPos, string entity, string role)
            : base(startPos, endPos, entity)
        {
            this.Role = role;
        }

        /// <summary>
        /// Gets or sets the role for the entity.
        /// </summary>
        /// <remarks>
        /// Do not change the setter to private, as this will prevent the
        /// LUIS client serializer from serializing this property.
        /// </remarks>
        [JsonProperty("role")]
        public string Role { get; set; }
    }
}
