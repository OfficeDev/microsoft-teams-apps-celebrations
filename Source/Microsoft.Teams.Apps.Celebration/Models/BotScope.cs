// <copyright file="BotScope.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.Celebration.Models
{
    /// <summary>
    /// Defines Kind of conversation
    /// </summary>
    public enum BotScope
    {
         /// <summary>
         /// Represents conversation between a bot and a single user
         /// </summary>
         Personal,

         /// <summary>
         /// Represents channel conversation
         /// </summary>
         Team,
    }
}