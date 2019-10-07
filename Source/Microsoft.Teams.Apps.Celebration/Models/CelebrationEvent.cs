// <copyright file="CelebrationEvent.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.Celebration.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using Microsoft.Azure.Documents;
    using Newtonsoft.Json;

    /// <summary>
    /// Represent event data
    /// </summary>
    public class CelebrationEvent : Resource
    {
         /// <summary>
        /// Gets or sets type of event. Birthday/Anniversary/others
        /// </summary>
        [JsonProperty("type")]
        [Required]
        public EventTypes Type { get; set; }

        /// <summary>
        /// Gets or sets event title
        /// </summary>
        [JsonProperty("title")]
        [Required]
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets message to post
        /// </summary>
        [JsonProperty("message")]
        public string Message { get; set; }

        /// <summary>
        /// Gets or Sets event Date
        /// </summary>
        [JsonProperty("date")]
        [Required]
        public DateTime Date { get; set; }

        /// <summary>
        /// Gets or sets timezone id given by TimeZoneInfo.Id
        /// </summary>
        [JsonProperty("timeZoneId")]
        [Required]
        public string TimeZoneId { get; set; }

        /// <summary>
        /// Gets or sets owner teamsId of event
        /// </summary>
        [JsonProperty("OwnerId")]
        public string OwnerTeamsId { get; set; }

        /// <summary>
        /// Gets or sets user AAD object id
        /// </summary>
        [JsonProperty("ownerAadObjectId")]
        public string OwnerAadObjectId { get; set; }

        /// <summary>
        /// Gets or sets image URL for event
        /// </summary>
        [JsonProperty("imageURL")]
        public string ImageUrl { get; set; }

        /// <summary>
        /// Gets month part of the event date
        /// </summary>
        [JsonProperty("eventMonth")]
        public int EventMonth
        {
            get { return this.Date.Month; }
        }

        /// <summary>
        /// Gets day part of the event date
        /// </summary>
        [JsonProperty("eventDay")]
        public int EventDay
        {
            get { return this.Date.Day; }
        }

        /// <summary>
        /// Gets or sets list of team information where bot is installed
        /// </summary>
        [JsonProperty("teams")]
        public List<Team> Teams { get; set; }
    }
}