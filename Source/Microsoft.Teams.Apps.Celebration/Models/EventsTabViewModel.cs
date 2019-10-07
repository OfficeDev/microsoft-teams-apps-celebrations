// <copyright file="EventsTabViewModel.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.Celebration.Models
{
    using System.Collections.Generic;

    /// <summary>
    /// Model for Events tab view
    /// </summary>
    public class EventsTabViewModel
    {
        /// <summary>
        /// Gets or sets the list of events
        /// </summary>
        public IList<CelebrationEvent> Events { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of events for a user
        /// </summary>
        public int MaxUserEventsCount { get; set; }
    }
}