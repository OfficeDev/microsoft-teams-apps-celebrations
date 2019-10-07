// <copyright file="EventOccurrence.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.Celebration.Models
{
    using System;
    using System.ComponentModel;
    using Microsoft.Azure.Documents;
    using Newtonsoft.Json;

    /// <summary>
    /// Represents the upcoming occurrences of a recurring event
    /// </summary>
    public class EventOccurrence : Resource
    {
        /// <summary>
        /// Gets or sets id, which is Id(Guid) in events collection
        /// </summary>
        [JsonProperty("eventId")]
        public string EventId { get; set; }

        /// <summary>
        /// Gets or sets the owner AAD object ID
        /// </summary>
        [JsonProperty("ownerAadObjectId")]
        public string OwnerAadObjectId { get; set; }

        /// <summary>
        /// Gets or sets UTC date and time of upcoming occurrence
        /// </summary>
        [JsonProperty("date")]
        public DateTimeOffset DateTime { get; set; }

        /// <summary>
        /// Gets or sets event's Status
        /// </summary>
        [JsonProperty("status")]
        [DefaultValue(EventStatus.Default)]
        public EventStatus Status { get; set; }

        /// <summary>
        /// Gets or sets the time to live in seconds. See https://docs.microsoft.com/en-us/azure/cosmos-db/time-to-live.
        /// </summary>
        [JsonProperty(PropertyName = "ttl", NullValueHandling = NullValueHandling.Ignore)]
        public int? TimeToLive { get; set; }

        /// <summary>
        /// Gets the last allowable time to send the notification. After this time, the event will simply be skipped.
        /// </summary>
        /// <returns>Last allowable time for notification</returns>
        public DateTimeOffset GetLastAllowableTimeToSendNotification()
        {
            return this.DateTime.AddHours(12);
        }
    }
}