// <copyright file="MessageType.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.Celebration.Models
{
    /// <summary>
    /// Represents the MessageType, to take the appropriate action for sending the type of card
    /// </summary>
    public enum MessageType
    {
        /// <summary>
        /// Not defined or default
        /// </summary>
        Unknown,

        /// <summary>
        /// Preview
        /// </summary>
        Preview,

        /// <summary>
        /// Event
        /// </summary>
        Event,
    }
}