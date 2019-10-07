// <copyright file="UserTeamMembership.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.Celebration.Models
{
    using Microsoft.Azure.Documents;
    using Newtonsoft.Json;

    /// <summary>
    /// Represents the membership of user within a team
    /// </summary>
    public class UserTeamMembership : Resource
    {
        /// <summary>
        /// Gets or sets user's teams id
        /// </summary>
        [JsonProperty("userTeamsId")]
        public string UserTeamsId { get; set; }

        /// <summary>
        /// Gets or sets id of team, user is member of
        /// </summary>
        [JsonProperty("teamId")]
        public string TeamId { get; set; }
    }
}