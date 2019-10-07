// <copyright file="ShareEventPayload.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.Celebration.Models
{
    /// <summary>
    /// Payload to share event with team
    /// </summary>
    public class ShareEventPayload : SubmitActionPayload
    {
        /// <summary>
        /// Gets or sets userAadObjectId
        /// </summary>
        public string UserAadObjectId { get; set; }

        /// <summary>
        /// Gets or sets teamId
        /// </summary>
        public string TeamId { get; set; }

        /// <summary>
        /// Gets or sets teamName
        /// </summary>
        public string TeamName { get; set; }
    }
}