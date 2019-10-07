// <copyright file="IEventDataProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.Celebration.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.Teams.Apps.Celebration.Models;

    /// <summary>
    /// Data provider for events
    /// </summary>
    public interface IEventDataProvider
    {
        /// <summary>
        /// Returns list of all events
        /// </summary>
        /// <returns>List of all events</returns>
        Task<IList<CelebrationEvent>> GetAllEventsAsync();

        /// <summary>
        /// Returns list of all events that belong to the given owner object ID.
        /// </summary>
        /// <param name="aadObjectId">AadObjectId of owner</param>
        /// <returns>List of events</returns>
        Task<IList<CelebrationEvent>> GetEventsByOwnerObjectIdAsync(string aadObjectId);

        /// <summary>
        /// Returns events that match the given query
        /// </summary>
        /// <param name="query">Query</param>
        /// <returns>List of events</returns>
        Task<IList<CelebrationEvent>> GetEventsAsync(string query);

        /// <summary>
        /// Gets a <see cref="CelebrationEvent"/> by id.
        /// </summary>
        /// <param name="eventId">Event Id</param>
        /// <param name="ownerAadObjectId">AadObjectId of owner</param>
        /// <returns>Event object</returns>
        Task<CelebrationEvent> GetEventByIdAsync(string eventId, string ownerAadObjectId);

        /// <summary>
        /// Add a new <see cref="CelebrationEvent"/>.
        /// </summary>
        /// <param name="celebrationEvent">CelebrationEvent object</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation, with the new event as a result</returns>
        Task<CelebrationEvent> AddEventAsync(CelebrationEvent celebrationEvent);

        /// <summary>
        /// Updates an existing <see cref="CelebrationEvent"/>.
        /// </summary>
        /// <param name="celebrationEvent">CelebrationEvent object</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation</returns>
        Task UpdateEventAsync(CelebrationEvent celebrationEvent);

        /// <summary>
        /// Delete event
        /// </summary>
        /// <param name="eventId">Event Id</param>
        /// <param name="ownerAadObjectId">AadObject id of owner</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation</returns>
        Task DeleteEventAsync(string eventId, string ownerAadObjectId);

        /// <summary>
        /// Get recurring events
        /// </summary>
        /// <param name="eventIds">List of event id</param>
        /// <returns>List of event occurrences</returns>
        Task<IList<EventOccurrence>> GetEventOccurrencesByEventIdsAsync(List<string> eventIds);

        /// <summary>
        /// Get recurring events
        /// </summary>
        /// <param name="currentDateTime">represents current dateTime instance</param>
        /// <returns>Due event occurrences</returns>
        Task<IList<EventOccurrence>> GetDueEventOccurrencesAsync(DateTimeOffset currentDateTime);

        /// <summary>
        /// Add a new event occurrence
        /// </summary>
        /// <param name="eventOccurrence">EventOccurrence instance</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation</returns>
        Task<EventOccurrence> AddEventOccurrenceAsync(EventOccurrence eventOccurrence);

        /// <summary>
        /// Update an event occurrence
        /// </summary>
        /// <param name="recurringEvent">EventOccurrence instance</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation</returns>
        Task UpdateEventOccurrenceAsync(EventOccurrence recurringEvent);

        /// <summary>
        /// Get an event occurrence by ID.
        /// </summary>
        /// <param name="occurrenceId">Occurrence id</param>
        /// <param name="eventId">Event id</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation</returns>
        Task<EventOccurrence> GetEventOccurrenceByIdAsync(string occurrenceId, string eventId);

        /// <summary>
        /// Delete recurring event
        /// </summary>
        /// <param name="id">Event occurrence id</param>
        /// <param name="eventId">EventId</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation</returns>
        Task DeleteEventOccurrenceAsync(string id, string eventId);

        /// <summary>
        /// Delete Recurring event for given eventId
        /// </summary>
        /// <param name="eventId">Event Id</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation</returns>
        Task DeleteEventOccurrencesByEventIdAsync(string eventId);

        /// <summary>
        /// Get EventMessages by StatusCode
        /// </summary>
        /// <param name="statusCode">HTTP Status code</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation</returns>
        Task<IList<EventMessage>> GetEventMessagesByDeliveryStatusCodeAsync(List<int> statusCode);

        /// <summary>
        /// Add message to send preview/event card
        /// </summary>
        /// <param name="eventMessage">EventMessage instance</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation</returns>
        Task<EventMessage> AddEventMessageAsync(EventMessage eventMessage);

        /// <summary>
        /// Update last message send result
        /// </summary>
        /// <param name="eventMessage">EventMessage instance</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation</returns>
        Task UpdateEventMessageAsync(EventMessage eventMessage);

        /// <summary>
        /// Delete record from EventMessages collection for given eventId
        /// </summary>
        /// <param name="eventId">Event Id</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation</returns>
        Task DeleteEventMessagesByEventIdAsync(string eventId);
    }
}