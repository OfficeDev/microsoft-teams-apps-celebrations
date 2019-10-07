// <copyright file="EventStatus.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.Celebration.Models
{
    /// <summary>
    /// Represents the states in which events can be at any particular time
    /// </summary>
    public enum EventStatus
    {
        /// <summary>
        /// Continue to post/celebrate event
        /// </summary>
        Default,

        /// <summary>
        /// Skip the event for current year
        /// </summary>
        Skipped,

        /// <summary>
        /// Event occurrence has been sent
        /// </summary>
        Sent,
    }
}