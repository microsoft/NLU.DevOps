// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace NLU.DevOps.Models
{
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Entity types class.
    /// </summary>
    public class EntityType
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EntityType"/> class.
        /// </summary>
        /// <param name="name">Entity type name.</param>
        /// <param name="kind">Entity type kind.</param>
        /// <param name="data">Entity type data.</param>
        public EntityType(string name, string kind, JToken data)
        {
            this.Name = name;
            this.Kind = kind;
            this.Data = data;
        }

        /// <summary>
        /// Gets the entity type name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the entity type kind.
        /// </summary>
        public string Kind { get; }

        /// <summary>
        /// Gets the entity type data.
        /// </summary>
        public JToken Data { get; }
    }
}
