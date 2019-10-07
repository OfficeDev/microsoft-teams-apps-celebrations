// <copyright file="ApplicationConfig.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.Celebration
{
    /// <summary>
    /// Application configuration keys
    /// </summary>
    public enum ApplicationConfig
    {
        /// <summary>
        /// Application base uri, without trailing slash
        /// </summary>
        BaseUrl,

        /// <summary>
        /// Cosmos DB endpoint url
        /// </summary>
        CosmosDBEndpointUrl,

        /// <summary>
        /// CosmosDB connection key
        /// </summary>
        CosmosDBKey,

        /// <summary>
        /// CosmosDB datbase name
        /// </summary>
        CosmosDBDatabaseName,

        /// <summary>
        /// Bot app id
        /// </summary>
        MicrosoftAppId,

        /// <summary>
        /// Bot app password
        /// </summary>
        MicrosoftAppPassword,

        /// <summary>
        /// Time to post the celebration in team
        /// </summary>
        TimeToPostCelebration,

        /// <summary>
        /// Maximum number of events per user
        /// </summary>
        MaxUserEventsCount,

        /// <summary>
        /// Number of days in advance to send the preview event notification
        /// </summary>
        DaysInAdvanceToSendEventPreview,

        /// <summary>
        /// Mininum time in hours to process event, otherwise the event will be skipped this year
        /// </summary>
        MinTimeToProcessEventInHours,

        /// <summary>
        /// Teams app ID
        /// </summary>
        ManifestAppId,
    }
}