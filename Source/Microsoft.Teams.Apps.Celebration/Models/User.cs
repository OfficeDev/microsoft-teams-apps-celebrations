// <copyright file="User.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.Celebration.Models
{
    using Microsoft.Azure.Documents;
    using Newtonsoft.Json;

    /// <summary>
    /// Represents users who have installed the app, either personally or as part of a team
    /// </summary>
    public class User : Resource
    {
        /// <summary>
        /// Gets or sets user's AadObjectId
        /// </summary>
        [JsonProperty("aadObjectId")]
        public string AadObjectId { get; set; }

        /// <summary>
        /// Gets or sets user's teams id
        /// </summary>
        [JsonProperty("teamsId")]
        public string TeamsId { get; set; }

        /// <summary>
        /// Gets or sets scope of bot
        /// </summary>
        [JsonProperty("installationMethod")]
        public BotScope InstallationMethod { get; set; }

        /// <summary>
        /// Gets or sets conversation Id to start the communication between bot and user
        /// </summary>
        [JsonProperty("conversationId")]
        public string ConversationId { get; set; }

        /// <summary>
        /// Gets or sets service URL,required to instantiate connector service
        /// </summary>
        [JsonProperty("serviceUrl")]
        public string ServiceUrl { get; set; }

        /// <summary>
        /// Gets or sets the display name
        /// </summary>
        [JsonProperty("userName")]
        public string DisplayName { get; set; }

        /// <summary>
        /// Gets or sets Tenant Id of user
        /// </summary>
        [JsonProperty("tenantId")]
        public string TenantId { get; set; }
    }
}