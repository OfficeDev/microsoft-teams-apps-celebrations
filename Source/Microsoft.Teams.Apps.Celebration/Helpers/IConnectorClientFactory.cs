// <copyright file="IConnectorClientFactory.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.Celebration.Helpers
{
    using Microsoft.Bot.Connector;

    /// <summary>
    /// Factory for <see cref="IConnectorClient"/>
    /// </summary>
    public interface IConnectorClientFactory
    {
        /// <summary>
        /// Returns the connector client to use for the specified service URL.
        /// </summary>
        /// <param name="serviceUrl">The service URL</param>
        /// <returns>The connector client instance to use</returns>
        IConnectorClient GetConnectorClient(string serviceUrl);
    }
}