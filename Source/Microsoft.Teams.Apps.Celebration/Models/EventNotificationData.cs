// <copyright file="EventNotificationData.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.Celebration.Models
{
    using Microsoft.Bot.Connector;
    using Microsoft.Teams.Apps.Celebration.Helpers;
    using Microsoft.Teams.Apps.Celebration.Resources;

    /// <summary>
    /// Represents the data needed to send an event notification card.
    /// </summary>
    public class EventNotificationData
    {
        /// <summary>
        /// Gets or sets the event occurrence
        /// </summary>
        public EventOccurrence Occurrence { get; set; }

        /// <summary>
        /// Gets or sets the event information
        /// </summary>
        public CelebrationEvent Event { get; set; }

        /// <summary>
        /// Gets or sets the owning user
        /// </summary>
        public User User { get; set; }

        /// <summary>
        /// Gets the message to send
        /// </summary>
        /// <returns>The message to send</returns>
        public string GetMessage()
        {
            return string.Format(Strings.SingleEventMessage, this.User.DisplayName, this.Event.Title);
        }

        /// <summary>
        /// Gets the card to send
        /// </summary>
        /// <returns>The card to send</returns>
        public Attachment GetCard()
        {
            return CelebrationCard.GetEventCard(this.Event, this.User.DisplayName).ToAttachment();
        }

        /// <summary>
        /// Gets the mention for the user.
        /// </summary>
        /// <returns>The mention entity</returns>
        public Mention GetMention()
        {
            return new Mention
            {
                Text = $"<at>{this.User.DisplayName}</at>",
                Mentioned = new ChannelAccount()
                {
                    Name = this.User.DisplayName,
                    Id = this.User.TeamsId,
                },
            };
        }
    }
}