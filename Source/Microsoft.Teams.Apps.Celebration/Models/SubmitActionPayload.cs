// <copyright file="SubmitActionPayload.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.Celebration.Models
{
    using System;

    /// <summary>
    /// Payload for an adaptive card submit button
    /// </summary>
    [Serializable]
    public class SubmitActionPayload
    {
        /// <summary>
        /// Gets or sets adaptive card submit button action
        /// </summary>
        public string Action { get; set; }
    }
}