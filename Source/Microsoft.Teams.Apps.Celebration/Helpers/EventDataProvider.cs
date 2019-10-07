// <copyright file="EventDataProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.Celebration.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
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
    /// Data provider for events
    /// </summary>
    public class EventDataProvider : IEventDataProvider
    {
        // Collection id to store user events
        private const string EventsCollectionId = "Events";

        // Collection id to store tracked occurrences
        private const string OccurrencesCollectionId = "Occurrences";

        // Collection id to store messages to send
        private const string EventMessagesCollectionId = "EventMessages";

        // Request the minimum throughput by default
        private const int DefaultRequestThroughput = 400;

        // Minimum document time to live in seconds
        private const int MinimumTimeToLiveInSeconds = 3600;

        private readonly IConfigProvider configProvider;
        private readonly ILogProvider logProvider;
        private readonly Lazy<Task> initializeTask;
        private DocumentClient documentClient;
        private Database database;

        private DocumentCollection eventsCollection;
        private DocumentCollection occurencesCollection;
        private DocumentCollection eventMessagesCollection;

        /// <summary>
        /// Initializes a new instance of the <see cref="EventDataProvider"/> class.
        /// </summary>
        /// <param name="configProvider">Configuration provider instance</param>
        /// <param name="logProvider">LogProvider instance</param>
        public EventDataProvider(IConfigProvider configProvider, ILogProvider logProvider)
        {
            this.configProvider = configProvider;
            this.logProvider = logProvider;
            this.initializeTask = new Lazy<Task>(() => this.IntializeDatabaseAsync());
        }

        /// <inheritdoc/>
        public async Task<IList<CelebrationEvent>> GetAllEventsAsync()
        {
            await this.EnsureInitializedAsync();

            var options = new FeedOptions { EnableCrossPartitionQuery = true };
            var documentQuery = this.documentClient.CreateDocumentQuery<CelebrationEvent>(this.eventsCollection.SelfLink, options).AsDocumentQuery();

            return await documentQuery.ToListAsync();
        }

        /// <inheritdoc/>
        public async Task<IList<CelebrationEvent>> GetEventsByOwnerObjectIdAsync(string aadObjectId)
        {
            await this.EnsureInitializedAsync();

            var documentQuery = this.documentClient.CreateDocumentQuery<CelebrationEvent>(this.eventsCollection.SelfLink)
                .Where(x => x.OwnerAadObjectId == aadObjectId)
                .AsDocumentQuery();
            return await documentQuery.ToListAsync();
        }

        /// <inheritdoc/>
        public async Task<IList<CelebrationEvent>> GetEventsAsync(string query)
        {
            await this.EnsureInitializedAsync();

            var options = new FeedOptions { EnableCrossPartitionQuery = true };
            var documentQuery = this.documentClient.CreateDocumentQuery<CelebrationEvent>(this.eventsCollection.SelfLink, query, options)
                .AsDocumentQuery();

            return await documentQuery.ToListAsync();
        }

        /// <inheritdoc/>
        public async Task<CelebrationEvent> GetEventByIdAsync(string eventId, string ownerAadObjectId)
        {
            try
            {
                await this.EnsureInitializedAsync();

                var documentUri = UriFactory.CreateDocumentUri(this.database.Id, this.eventsCollection.Id, eventId);
                var options = new RequestOptions { PartitionKey = new PartitionKey(ownerAadObjectId) };

                return await this.documentClient.ReadDocumentAsync<CelebrationEvent>(documentUri, options);
            }
            catch (DocumentClientException ex)
            {
                if (ex.StatusCode == HttpStatusCode.NotFound)
                {
                    this.logProvider.LogInfo($"Event id {eventId} (owner = {ownerAadObjectId}) does not exist.");
                    return null;
                }
                else
                {
                    this.logProvider.LogError($"Failed to fetch event {eventId}: {ex.Message}", ex);
                    throw;
                }
            }
        }

        /// <inheritdoc/>
        public async Task<CelebrationEvent> AddEventAsync(CelebrationEvent celebrationEvent)
        {
            if (celebrationEvent.Id != null)
            {
                throw new ArgumentException("A new event must not have an assigned ID", nameof(celebrationEvent));
            }

            await this.EnsureInitializedAsync();

            var response = await this.documentClient.CreateDocumentAsync(this.eventsCollection.SelfLink, celebrationEvent);
            return (CelebrationEvent)(dynamic)response.Resource;
        }

        /// <inheritdoc/>
        public async Task UpdateEventAsync(CelebrationEvent celebrationEvent)
        {
            if (celebrationEvent.Id == null)
            {
                throw new ArgumentException("Event must have an ID", nameof(celebrationEvent));
            }

            await this.EnsureInitializedAsync();

            var documentUri = UriFactory.CreateDocumentUri(this.database.Id, this.eventsCollection.Id, celebrationEvent.Id);
            await this.documentClient.ReplaceDocumentAsync(documentUri, celebrationEvent);
        }

        /// <inheritdoc/>
        public async Task DeleteEventAsync(string eventId, string ownerAadObjectId)
        {
            await this.EnsureInitializedAsync();
            var document = await this.GetEventByIdAsync(eventId, ownerAadObjectId);

            if (document != null)
            {
                await this.documentClient.DeleteDocumentAsync(document.SelfLink, new RequestOptions { PartitionKey = new PartitionKey(ownerAadObjectId) });
            }
        }

        /// <inheritdoc/>
        public async Task<IList<EventOccurrence>> GetEventOccurrencesByEventIdsAsync(List<string> eventIds)
        {
            await this.EnsureInitializedAsync();

            var options = new FeedOptions { EnableCrossPartitionQuery = true };
            var documentQuery = this.documentClient.CreateDocumentQuery<EventOccurrence>(this.occurencesCollection.SelfLink, options)
                .Where(x => eventIds.Contains(x.EventId))
                .AsDocumentQuery();

            return await documentQuery.ToListAsync();
        }

        /// <inheritdoc/>
        public async Task<IList<EventOccurrence>> GetDueEventOccurrencesAsync(DateTimeOffset currentDateTime)
        {
            await this.EnsureInitializedAsync();

            var options = new FeedOptions { EnableCrossPartitionQuery = true };
            var documentQuery = this.documentClient.CreateDocumentQuery<EventOccurrence>(this.occurencesCollection.SelfLink, options)
                .Where(x => x.Status == EventStatus.Default && x.DateTime <= currentDateTime)
                .AsDocumentQuery();

            return await documentQuery.ToListAsync();
        }

        /// <inheritdoc/>
        public async Task<EventOccurrence> AddEventOccurrenceAsync(EventOccurrence eventOccurrence)
        {
            if (eventOccurrence.Id != null)
            {
                throw new ArgumentException("A new occurrence must not have an assigned ID", nameof(eventOccurrence));
            }

            await this.EnsureInitializedAsync();

            eventOccurrence.TimeToLive = this.GetTimeToLive(eventOccurrence.GetLastAllowableTimeToSendNotification());

            var response = await this.documentClient.CreateDocumentAsync(this.occurencesCollection.SelfLink, eventOccurrence);
            return (EventOccurrence)(dynamic)response.Resource;
        }

        /// <inheritdoc/>
        public async Task UpdateEventOccurrenceAsync(EventOccurrence eventOccurrence)
        {
            if (eventOccurrence.Id == null)
            {
                throw new ArgumentException("Occurrence must have an ID", nameof(eventOccurrence));
            }

            await this.EnsureInitializedAsync();

            eventOccurrence.TimeToLive = this.GetTimeToLive(eventOccurrence.DateTime);

            var documentUri = UriFactory.CreateDocumentUri(this.database.Id, this.occurencesCollection.Id, eventOccurrence.Id);
            await this.documentClient.ReplaceDocumentAsync(documentUri, eventOccurrence);
        }

        /// <inheritdoc/>
        public async Task<EventOccurrence> GetEventOccurrenceByIdAsync(string occurrenceId, string eventId)
        {
            await this.EnsureInitializedAsync();

            try
            {
                var documentUri = UriFactory.CreateDocumentUri(this.database.Id, this.occurencesCollection.Id, occurrenceId);
                var options = new RequestOptions { PartitionKey = new PartitionKey(eventId) };

                return await this.documentClient.ReadDocumentAsync<EventOccurrence>(documentUri, options);
            }
            catch (DocumentClientException ex)
            {
                if (ex.StatusCode == HttpStatusCode.NotFound)
                {
                    this.logProvider.LogInfo($"Event occurrence {occurrenceId} (eventId = {eventId}) does not exist.");
                    return null;
                }
                else
                {
                    this.logProvider.LogError($"Failed to fetch event occurrence {occurrenceId}: {ex.Message}", ex);
                    throw;
                }
            }
        }

        /// <inheritdoc/>
        public async Task DeleteEventOccurrenceAsync(string id, string eventId)
        {
            await this.EnsureInitializedAsync();

            try
            {
                var documentUri = UriFactory.CreateDocumentUri(this.database.Id, this.occurencesCollection.Id, id);
                var options = new RequestOptions { PartitionKey = new PartitionKey(eventId) };

                var eventOccurrence = await this.documentClient.ReadDocumentAsync<EventOccurrence>(documentUri, options);

                await this.documentClient.DeleteDocumentAsync(eventOccurrence.Document.SelfLink, options);
            }
            catch (DocumentClientException ex)
            {
                if (ex.StatusCode == HttpStatusCode.NotFound)
                {
                    this.logProvider.LogInfo($"EventOccurrence {id} (event id = {eventId}) does not exist.");
                }
                else
                {
                    this.logProvider.LogError($"Failed to fetch event occurrence {id}: {ex.Message}", ex);
                    throw;
                }
            }
        }

        /// <inheritdoc/>
        public async Task DeleteEventOccurrencesByEventIdAsync(string eventId)
        {
            await this.EnsureInitializedAsync();

            var options = new FeedOptions { PartitionKey = new PartitionKey(eventId) };
            var document = this.documentClient.CreateDocumentQuery<EventOccurrence>(this.occurencesCollection.SelfLink, options)
                .Where(x => x.EventId == eventId)
                .AsEnumerable()
                .FirstOrDefault();

            if (document != null)
            {
                await this.documentClient.DeleteDocumentAsync(document.SelfLink, new RequestOptions { PartitionKey = new PartitionKey(eventId) });
            }
        }

        /// <inheritdoc/>
        public async Task<IList<EventMessage>> GetEventMessagesByDeliveryStatusCodeAsync(List<int> statusCodes)
        {
            await this.EnsureInitializedAsync();

            var statusCodesArray = string.Join(", ", statusCodes.Select(x => x.ToString(CultureInfo.InvariantCulture)));
            var query = $"SELECT * FROM msg WHERE NOT(IS_OBJECT(msg.messageSendResult)) OR ARRAY_CONTAINS([{statusCodesArray}], msg.messageSendResult.StatusCode)";

            var options = new FeedOptions { EnableCrossPartitionQuery = true };
            var documentQuery = this.documentClient.CreateDocumentQuery<EventMessage>(this.eventMessagesCollection.SelfLink, query, options)
                .AsDocumentQuery();

            return await documentQuery.ToListAsync();
        }

        /// <inheritdoc/>
        public async Task<EventMessage> AddEventMessageAsync(EventMessage eventMessage)
        {
            if (eventMessage.Id != null)
            {
                throw new ArgumentException("A new event message must not have an assigned ID", nameof(eventMessage));
            }

            await this.EnsureInitializedAsync();

            eventMessage.TimeToLive = this.GetTimeToLive(eventMessage.ExpireAt);
            var response = await this.documentClient.CreateDocumentAsync(this.eventMessagesCollection.SelfLink, eventMessage);

            return (EventMessage)(dynamic)response.Resource;
        }

        /// <inheritdoc/>
        public async Task UpdateEventMessageAsync(EventMessage eventMessage)
        {
            await this.EnsureInitializedAsync();

            eventMessage.TimeToLive = this.GetTimeToLive(eventMessage.ExpireAt);

            var documentUri = UriFactory.CreateDocumentUri(this.database.Id, this.eventMessagesCollection.Id, eventMessage.Id);
            await this.documentClient.ReplaceDocumentAsync(documentUri, eventMessage);
        }

        /// <inheritdoc/>
        public async Task DeleteEventMessagesByEventIdAsync(string eventId)
        {
            await this.EnsureInitializedAsync();

            var options = new FeedOptions { EnableCrossPartitionQuery = true };
            var eventMessages = this.documentClient.CreateDocumentQuery<EventMessage>(this.eventMessagesCollection.SelfLink, options)
                .Where(x => x.EventId == eventId)
                .AsEnumerable();

            foreach (var document in eventMessages)
            {
                await this.documentClient.DeleteDocumentAsync(document.SelfLink, new RequestOptions { PartitionKey = new PartitionKey(eventId) });
            }
        }

        private async Task IntializeDatabaseAsync()
        {
            this.logProvider.LogInfo("Initializing data store");

            var endpointUrl = new Uri(this.configProvider.GetSetting(ApplicationConfig.CosmosDBEndpointUrl));
            var key = this.configProvider.GetSetting(ApplicationConfig.CosmosDBKey);
            var databaseId = this.configProvider.GetSetting(ApplicationConfig.CosmosDBDatabaseName);
            this.documentClient = new DocumentClient(endpointUrl, key);

            var requestOptions = new RequestOptions { OfferThroughput = DefaultRequestThroughput };
            bool useSharedOffer = true;

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

            // Get a reference to the Events collection, creating it if needed
            var eventsCollectionDefinition = new DocumentCollection
            {
                Id = EventsCollectionId,
            };

            eventsCollectionDefinition.PartitionKey.Paths.Add("/ownerAadObjectId");
            this.eventsCollection = await this.documentClient.CreateDocumentCollectionIfNotExistsAsync(this.database.SelfLink, eventsCollectionDefinition, useSharedOffer ? null : requestOptions);

            // Get a reference to the Occurrences collection, creating it if needed
            var ocurrencesCollectionDefinition = new DocumentCollection
            {
                Id = OccurrencesCollectionId,
                DefaultTimeToLive = -1,
            };

            ocurrencesCollectionDefinition.PartitionKey.Paths.Add("/eventId");
            this.occurencesCollection = await this.documentClient.CreateDocumentCollectionIfNotExistsAsync(this.database.SelfLink, ocurrencesCollectionDefinition, useSharedOffer ? null : requestOptions);

            // Get a reference to the EventMessages collection, creating it if needed
            var eventMessagesCollectionDefinition = new DocumentCollection
            {
                Id = EventMessagesCollectionId,
                DefaultTimeToLive = (int)TimeSpan.FromDays(1).TotalSeconds, // Try to deliver messages for 1 day by default
            };

            eventMessagesCollectionDefinition.PartitionKey.Paths.Add("/messageType");
            this.eventMessagesCollection = await this.documentClient.CreateDocumentCollectionIfNotExistsAsync(this.database.SelfLink, eventMessagesCollectionDefinition, useSharedOffer ? null : requestOptions);

            this.logProvider.LogInfo("Data store initialized");
        }

        private async Task EnsureInitializedAsync()
        {
            await this.initializeTask.Value;
        }

        private int GetTimeToLive(DateTimeOffset expireAt)
        {
            var ttl = expireAt - DateTimeOffset.UtcNow;
            return Math.Max((int)ttl.TotalSeconds, MinimumTimeToLiveInSeconds);
        }
    }
}