// <copyright file="TimeZoneDisplayInfo.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.Celebration.Models
{
    /// <summary>
    /// Model to display timezone list
    /// </summary>
    public class TimeZoneDisplayInfo
    {
        /// <summary>
        /// Gets or sets timeZone display name
        /// </summary>
        public string TimeZoneDisplayName { get; set; }

        /// <summary>
        /// Gets or sets timeZoneId
        /// </summary>
        public string TimeZoneId { get; set; }
    }
}