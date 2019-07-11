// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace NLU.DevOps.Luis
{
    using System.Composition;
    using Models;

    /// <inheritdoc />
    [Export("luis", typeof(INLUQueryFactory))]
    public class LuisV2NLUQueryFactory : LuisNLUQueryFactory
    {
    }
}
