// <copyright file="ManageEventViewModel.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.Celebration.Models
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Model for Manage Event view
    /// </summary>
    public class ManageEventViewModel
    {
        /// <summary>
        /// Gets or sets event ID
        /// </summary>
        public string EventId { get; set; }

        /// <summary>
        /// Gets or sets client time zone
        /// </summary>
        public string ClientTimeZone { get; set; }

        /// <summary>
        /// Gets or sets CelebrationEvent
        /// </summary>
        public CelebrationEvent CelebrationEvent { get; set; }

        /// <summary>
        /// Gets or sets TeamDetails
        /// </summary>
        public IList<Team> TeamDetails { get; set; }

        /// <summary>
        /// Gets or sets list of windows timezones, from TimeZoneInfo API
        /// </summary>
        public IList<TimeZoneDisplayInfo> TimeZoneList { get; set; }

        /// <summary>
        /// Gets or sets selected time zone id
        /// </summary>
        public string SelectedTimeZoneId { get; set; }

        /// <summary>
        /// Gets or sets the list of event types
        /// </summary>
        public IList<Tuple<EventTypes, string>> EventTypesInfo { get; set; }
    }
}