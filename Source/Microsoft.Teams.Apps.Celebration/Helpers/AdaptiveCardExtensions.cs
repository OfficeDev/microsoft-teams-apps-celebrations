// <copyright file="AdaptiveCardExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.Celebration.Helpers
{
    using AdaptiveCards;
    using Microsoft.Bot.Connector;

    /// <summary>
    /// Store extension method for Adaptive card
    /// </summary>
    public static class AdaptiveCardExtensions
    {
        /// <summary>
        /// AdaptiveCard instance
        /// </summary>
        /// <param name="card">Adaptive card</param>
        /// <returns>Attachment.</returns>
        public static Attachment ToAttachment(this AdaptiveCard card)
        {
            return new Attachment
            {
                ContentType = AdaptiveCard.ContentType,
                Content = card,
            };
        }
    }
}