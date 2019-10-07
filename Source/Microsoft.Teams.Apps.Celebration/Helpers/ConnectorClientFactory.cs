// <copyright file="ConnectorClientFactory.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.Celebration.Helpers
{
    using System;
    using System.Collections.Concurrent;
    using Microsoft.Bot.Connector;

    /// <summary>
    /// Factory for <see cref="IConnectorClient"/>
    /// </summary>
    public class ConnectorClientFactory : IConnectorClientFactory
    {
        private readonly ConcurrentDictionary<string, IConnectorClient> clientsCache = new ConcurrentDictionary<string, IConnectorClient>();

        /// <summary>
        /// Returns the connector client to use for the specified service URL.
        /// </summary>
        /// <param name="serviceUrl">The service URL</param>
        /// <returns>The connector client instance to use</returns>
        public IConnectorClient GetConnectorClient(string serviceUrl)
        {
            MicrosoftAppCredentials.TrustServiceUrl(serviceUrl);
            return this.clientsCache.GetOrAdd(serviceUrl, (url) =>
            {
                return new ConnectorClient(new Uri(url));
            });
        }
    }
}