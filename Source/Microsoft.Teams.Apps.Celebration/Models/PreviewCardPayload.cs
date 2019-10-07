// <copyright file="PreviewCardPayload.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>
namespace Microsoft.Teams.Apps.Celebration.Models
{
    /// <summary>
    /// Preview card payload
    /// </summary>
    public class PreviewCardPayload : SubmitActionPayload
    {
        /// <summary>
        /// Gets or sets the event id
        /// </summary>
        public string EventId { get; set; }

        /// <summary>
        /// Gets or sets the occurrence id
        /// </summary>
        public string OccurrenceId { get; set; }

        /// <summary>
        /// Gets or sets OwnerAadObjectId
        /// </summary>
        public string OwnerAadObjectId { get; set; }

        /// <summary>
        /// Gets or sets OwnerName
        /// </summary>
        public string OwnerName { get; set; }
    }
}