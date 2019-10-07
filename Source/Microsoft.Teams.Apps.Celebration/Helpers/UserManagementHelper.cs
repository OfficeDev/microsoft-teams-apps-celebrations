// <copyright file="UserManagementHelper.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.Celebration.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Threading.Tasks;
    using Microsoft.Azure.Documents;
    using Microsoft.Azure.Documents.Client;
    using Microsoft.Azure.Documents.Linq;
    using Microsoft.Teams.Apps.Celebration.Models;
    using Microsoft.Teams.Apps.Common.Configuration;
    using Microsoft.Teams.Apps.Common.Extensions;
    using Microsoft.Teams.Apps.Common.Logging;

    /// <summary>
    /// Stores methods to perform the crud operation in document DB
    /// </summary>
    public class UserManagementHelper : IUserManagementHelper
    {
        // Collection id to store teams
        private const string TeamsCollectionId = "Teams";

        // Collection id to store users
        private const string UsersCollectionId = "Users";

        // Collection id to store user team memberships
        private const string UserTeamMembershipCollectionId = "UserTeamMembership";

        // Request the minimum throughput by default
        private const int DefaultRequestThroughput = 400;

        private readonly IConfigProvider configProvider;
        private readonly ILogProvider logProvider;
        private readonly Lazy<Task> initializeTask;

        private Database database;
        private DocumentCollection teamsCollection;
        private DocumentCollection usersCollection;
        private DocumentCollection userTeamMembershipCollection;

        private DocumentClient documentClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserManagementHelper"/> class.
        /// </summary>
        /// <param name="configProvider">Configuration provider instance</param>
        /// <param name="logProvider">Log provider instance</param>
        public UserManagementHelper(IConfigProvider configProvider, ILogProvider logProvider)
        {
            this.configProvider = configProvider;
            this.logProvider = logProvider;
            this.initializeTask = new Lazy<Task>(() => this.IntializeDatabaseAsync());
        }

        /// <inheritdoc/>
        public async Task<Team> GetTeamsDetailsByTeamIdAsync(string teamId)
        {
            await this.EnsureInitializedAsync();

            try
            {
                var documentUri = UriFactory.CreateDocumentUri(this.database.Id, this.teamsCollection.Id, teamId);
                var team = await this.documentClient.ReadDocumentAsync<Team>(documentUri, new RequestOptions { PartitionKey = new PartitionKey(teamId) });
                return team;
            }
            catch (DocumentClientException ex)
            {
                if (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    this.logProvider.LogInfo($"Team ID '{teamId}' does not exist.");
                    return null;
                }
                else
                {
                    throw;
                }
            }
        }

        /// <inheritdoc/>
        public async Task<IList<Team>> GetTeamsDetailsByTeamIdsAsync(List<string> teamIds)
        {
            await this.EnsureInitializedAsync();
            var options = new FeedOptions { EnableCrossPartitionQuery = true };

            var query = this.documentClient.CreateDocumentQuery<Team>(this.teamsCollection.SelfLink, options).Where(x => teamIds.Contains(x.Id)).AsDocumentQuery();
            return await query.ToListAsync();
        }

        /// <inheritdoc/>
        public async Task SaveTeamDetailsAsync(Team team)
        {
            await this.EnsureInitializedAsync();
            await this.documentClient.UpsertDocumentAsync(this.teamsCollection.SelfLink, team);
        }

        /// <inheritdoc/>
        public async Task DeleteTeamDetailsAsync(string teamId)
        {
            await this.EnsureInitializedAsync();

            try
            {
                var documentUri = UriFactory.CreateDocumentUri(this.database.Id, this.teamsCollection.Id, teamId);
                var options = new RequestOptions { PartitionKey = new PartitionKey(teamId) };
                var team = await this.documentClient.ReadDocumentAsync<Team>(documentUri, options);
                await this.documentClient.DeleteDocumentAsync(documentUri, options);
            }
            catch (DocumentClientException ex)
            {
                if (ex.StatusCode == HttpStatusCode.NotFound)
                {
                    this.logProvider.LogInfo($"Team id '{teamId}' does not exist.");
                }
                else
                {
                    throw;
                }
            }
        }

        /// <inheritdoc/>
        public async Task<Models.User> GetUserByAadObjectIdAsync(string aadObjectId)
        {
            await this.EnsureInitializedAsync();

            return (await this.documentClient.CreateDocumentQuery<Models.User>(this.usersCollection.SelfLink)
                   .Where(x => x.AadObjectId == aadObjectId).AsDocumentQuery().ToListAsync()).FirstOrDefault();
        }

        /// <inheritdoc/>
        public async Task SaveUserAsync(Models.User user)
        {
            await this.EnsureInitializedAsync();
            await this.documentClient.UpsertDocumentAsync(this.usersCollection.SelfLink, user);
        }

        /// <inheritdoc/>
        public async Task DeleteUserByTeamsIdAsync(string userTeamsId)
        {
            await this.EnsureInitializedAsync();
            var document = await this.GetUserByAadObjectIdAsync(userTeamsId);

            if (document != null)
            {
                await this.documentClient.DeleteDocumentAsync(document.SelfLink, new RequestOptions { PartitionKey = new PartitionKey(document.AadObjectId) });
            }
        }

        /// <inheritdoc/>
        public async Task AddUserTeamMembershipAsync(UserTeamMembership userTeamMembership)
        {
            await this.EnsureInitializedAsync();
            await this.documentClient.CreateDocumentAsync(this.userTeamMembershipCollection.SelfLink, userTeamMembership);
        }

        /// <inheritdoc/>
        public async Task DeleteUserTeamMembershipAsync(string userTeamsId, string teamId)
        {
            await this.EnsureInitializedAsync();
            var options = new FeedOptions { PartitionKey = new PartitionKey(userTeamsId) };
            var document = (await this.documentClient.CreateDocumentQuery<UserTeamMembership>(this.userTeamMembershipCollection.SelfLink, options)
                           .Where(x => x.TeamId == teamId && x.UserTeamsId == userTeamsId)
                           .AsDocumentQuery().ToListAsync()).FirstOrDefault();

            if (document != null)
            {
                await this.documentClient.DeleteDocumentAsync(document.SelfLink, new RequestOptions { PartitionKey = new PartitionKey(userTeamsId) });
            }
        }

        /// <inheritdoc/>
        public async Task DeleteUserTeamMembershipByTeamIdAsync(string teamId)
        {
            await this.EnsureInitializedAsync();

            var documents = await this.GetUserTeamMembershipByTeamIdAsync(teamId);
            if (documents != null)
            {
                foreach (var document in documents)
                {
                    await this.documentClient.DeleteDocumentAsync(document.SelfLink, new RequestOptions { PartitionKey = new PartitionKey(document.UserTeamsId) });
                }
            }
        }

        /// <inheritdoc/>
        public async Task<IList<UserTeamMembership>> GetUserTeamMembershipByTeamIdAsync(string teamId)
        {
            await this.EnsureInitializedAsync();
            var options = new FeedOptions { EnableCrossPartitionQuery = true };

            return await this.documentClient.CreateDocumentQuery<UserTeamMembership>(this.userTeamMembershipCollection.SelfLink, options)
                           .Where(x => x.TeamId == teamId).AsDocumentQuery().ToListAsync();
        }

        /// <inheritdoc/>
        public async Task<IList<UserTeamMembership>> GetUserTeamMembershipByTeamsIdAsync(string userTeamsId)
        {
            await this.EnsureInitializedAsync();
            var options = new FeedOptions { EnableCrossPartitionQuery = true };

            return await this.documentClient.CreateDocumentQuery<UserTeamMembership>(this.userTeamMembershipCollection.SelfLink, options)
                           .Where(x => x.UserTeamsId == userTeamsId).AsDocumentQuery().ToListAsync();
        }

        private async Task IntializeDatabaseAsync()
        {
            this.logProvider.LogInfo("Initializing data store");

            var endpointUrl = new Uri(this.configProvider.GetSetting(ApplicationConfig.CosmosDBEndpointUrl));
            var key = this.configProvider.GetSetting(ApplicationConfig.CosmosDBKey);
            var databaseId = this.configProvider.GetSetting(ApplicationConfig.CosmosDBDatabaseName);
            var requestOptions = new RequestOptions { OfferThroughput = DefaultRequestThroughput };
            bool useSharedOffer = true;

            this.documentClient = new DocumentClient(endpointUrl, key);

            // Create the database if needed
            try
            {
                this.database = await this.documentClient.CreateDatabaseIfNotExistsAsync(new Database { Id = databaseId }, requestOptions);
            }
            catch (DocumentClientException ex)
            {
                if (ex.Error?.Message?.Contains("SharedOffer is Disabled") ?? false)
                {
                    this.logProvider.LogInfo("Database shared offer is disabled for the account, will provision throughput at container level");
                    useSharedOffer = false;

                    this.database = await this.documentClient.CreateDatabaseIfNotExistsAsync(new Database { Id = databaseId });
                }
                else
                {
                    throw;
                }
            }

            // Get a reference to the Teams collection, creating it if needed
            var teamsCollectionDefinition = new DocumentCollection
            {
                Id = TeamsCollectionId,
            };

            teamsCollectionDefinition.PartitionKey.Paths.Add("/id");
            this.teamsCollection = await this.documentClient.CreateDocumentCollectionIfNotExistsAsync(this.database.SelfLink, teamsCollectionDefinition, useSharedOffer ? null : requestOptions);

            // Get a reference to the Users collection, creating it if needed
            var usersCollectionDefinition = new DocumentCollection
            {
                Id = UsersCollectionId,
            };

            usersCollectionDefinition.PartitionKey.Paths.Add("/aadObjectId");
            this.usersCollection = await this.documentClient.CreateDocumentCollectionIfNotExistsAsync(this.database.SelfLink, usersCollectionDefinition, useSharedOffer ? null : requestOptions);

            // Get a reference to the userTeamMembership collection, creating it if needed
            var userTeamMembershipCollectionDefinition = new DocumentCollection
            {
                Id = UserTeamMembershipCollectionId,
            };

            userTeamMembershipCollectionDefinition.PartitionKey.Paths.Add("/userTeamsId");
            this.userTeamMembershipCollection = await this.documentClient.CreateDocumentCollectionIfNotExistsAsync(this.database.SelfLink, userTeamMembershipCollectionDefinition, useSharedOffer ? null : requestOptions);

            this.logProvider.LogInfo("Data store initialized");
        }

        private async Task EnsureInitializedAsync()
        {
            await this.initializeTask.Value;
        }
    }
}