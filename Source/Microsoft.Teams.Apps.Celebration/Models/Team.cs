// <copyright file="Team.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.Celebration.Models
{
    using Microsoft.Azure.Documents;
    using Newtonsoft.Json;

    /// <summary>
    /// Store the teams meta data
    /// </summary>
    public class Team : Resource
    {
        /// <summary>
        /// Gets or sets team name
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the service url for the team
        /// </summary>
        [JsonProperty("serviceUrl")]
        public string ServiceUrl { get; set; }

        /// <summary>
        /// Gets or sets the tenant id
        /// </summary>
        [JsonProperty("tenantId")]
        public string TenantId { get; set; }

        /// <summary>
        /// Gets or sets installer name
        /// </summary>
        [JsonProperty("installerName")]
        public string InstallerName { get; set; }
    }
}