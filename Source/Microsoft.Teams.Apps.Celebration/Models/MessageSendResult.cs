// <copyright file="MessageSendResult.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.Celebration.Models
{
    using System;

    /// <summary>
    /// Represents result of last message sent
    /// </summary>
    public class MessageSendResult
    {
        /// <summary>
        /// Gets or sets last attempt time
        /// </summary>
        public DateTimeOffset LastAttemptTime { get; set; }

        /// <summary>
        /// Gets or sets HTTP status code
        /// </summary>
        public int StatusCode { get; set; }

        /// <summary>
        /// Gets or sets Response body
        /// </summary>
        public string ResponseBody { get; set; }
    }
}