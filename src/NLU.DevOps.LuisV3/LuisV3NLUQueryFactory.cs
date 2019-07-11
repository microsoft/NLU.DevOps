// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace NLU.DevOps.Luis
{
    using System.Composition;
    using Models;

    /// <inheritdoc />
    [Export("luisV3", typeof(INLUQueryFactory))]
    public class LuisV3NLUQueryFactory : LuisNLUQueryFactory
    {
    }
}
