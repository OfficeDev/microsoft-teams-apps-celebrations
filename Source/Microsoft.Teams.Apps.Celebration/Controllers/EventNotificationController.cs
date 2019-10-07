// <copyright file="EventNotificationController.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.Celebration.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Net;
    using System.Threading.Tasks;
    using System.Web.Hosting;
    using System.Web.Http;
    using Microsoft.Bot.Connector;
    using Microsoft.Teams.Apps.Celebration.Helpers;
    using Microsoft.Teams.Apps.Celebration.Models;
    using Microsoft.Teams.Apps.Celebration.Resources;
    using Microsoft.Teams.Apps.Common.Extensions;
    using Microsoft.Teams.Apps.Common.Logging;

    /// <summary>
    /// Controller that handles request to send event card on the day of event
    /// </summary>
    [SharedSecretAuthentication(SharedSecretSettingName = "AppApiKey")]
    public class EventNotificationController : ApiController
    {
        private const int MaxEventsPerCarousel = 6;
        private const int MaxEventsToSendIndividually = 1;

        private readonly IEventDataProvider eventDataProvider;
        private readonly IUserManagementHelper userManagementHelper;
        private readonly IConnectorClientFactory connectorClientFactory;
        private readonly ILogProvider logProvider;
        private readonly Dictionary<string, List<EventNotificationData>> teamToEventNotificationsMap;

        /// <summary>
        /// Initializes a new instance of the <see cref="EventNotificationController"/> class.
        /// </summary>
        /// <param name="eventDataProvider">Event data provider instance</param>
        /// <param name="userManagementHelper">UserManagementHelper instance</param>
        /// <param name="connectorClientFactory">Connector client factory instance</param>
        /// <param name="logProvider">The instance of <see cref="ILogProvider"/></param>
        public EventNotificationController(IEventDataProvider eventDataProvider, IUserManagementHelper userManagementHelper, IConnectorClientFactory connectorClientFactory, ILogProvider logProvider)
        {
            this.eventDataProvider = eventDataProvider;
            this.userManagementHelper = userManagementHelper;
            this.connectorClientFactory = connectorClientFactory;
            this.logProvider = logProvider;
            this.teamToEventNotificationsMap = new Dictionary<string, List<EventNotificationData>>();
        }

        /// <summary>
        /// Process incoming request to send the due events
        /// </summary>
        /// <param name="effectiveDateTime">The effective current date and time</param>
        /// <returns>A <see cref="Task"/>Representing the asynchronous operation</returns>
        public IHttpActionResult Post([FromUri] string effectiveDateTime = "")
        {
            this.logProvider.LogInfo($"Processing trigger to send events. effectiveDateTime: {effectiveDateTime}");

            if (!DateTimeOffset.TryParse(effectiveDateTime, null, DateTimeStyles.AdjustToUniversal, out DateTimeOffset currentDateTime))
            {
                currentDateTime = DateTimeOffset.UtcNow;
            }

            HostingEnvironment.QueueBackgroundWorkItem(ct => this.SendEventNotificationsAsync(currentDateTime));

            return this.StatusCode(HttpStatusCode.OK);
        }

        // Send the event notifications that are due this hour
        private async Task SendEventNotificationsAsync(DateTimeOffset currentDateTimeOffset)
        {
            try
            {
                this.logProvider.LogInfo("Starting background work item to send event notifications");

                // Get all the due occurrences for Occurrences collection where event dateTime matches with current dateTime
                var dueOccurrences = await this.eventDataProvider.GetDueEventOccurrencesAsync(currentDateTimeOffset);
                if (dueOccurrences.Count > 0)
                {
                    this.logProvider.LogInfo($"Found {dueOccurrences.Count} to share with teams");
                }
                else
                {
                    this.logProvider.LogInfo($"No events are due to share, exiting");
                    return;
                }

                // Resolve the occurrences to their corresponding events
                var eventNotificationDataList = await Task.WhenAll(
                    dueOccurrences.Select(async (occurrence) =>
                    {
                        var celebrationEvent = await this.eventDataProvider.GetEventByIdAsync(occurrence.EventId, occurrence.OwnerAadObjectId);
                        var user = await this.userManagementHelper.GetUserByAadObjectIdAsync(occurrence.OwnerAadObjectId);
                        return new EventNotificationData { Event = celebrationEvent, Occurrence = occurrence, User = user };
                    }));

                // Group the events by team
                foreach (var eventNotificationData in eventNotificationDataList)
                {
                    if (eventNotificationData.Event == null)
                    {
                        this.logProvider.LogInfo($"Could not find event {eventNotificationData.Occurrence.EventId}, skipping");
                        continue;
                    }

                    // Update mapping of team ID -> notifications to send to the team
                    foreach (var team in eventNotificationData.Event.Teams)
                    {
                        this.AddEventNotificationForTeam(team.Id, eventNotificationData);
                    }
                }

                // Send notifications to the teams
                this.logProvider.LogInfo($"Sending messages to {this.teamToEventNotificationsMap.Keys.Count} teams");
                await this.SendEventNotifications();

                // Mark the occurrences as delivered
                await Task.WhenAll(dueOccurrences.Select(occurrence => this.MarkEventOccurrenceAsSentAsync(occurrence)));
            }
            catch (Exception ex)
            {
                this.logProvider.LogError($"Error sending event notifications: {ex.Message}", ex);
                throw;
            }
            finally
            {
                this.logProvider.LogInfo("Finished background work item to send event notifications");
            }
        }

        // 1. Loop through each key in teamsEventsDictionary where key = teamId, value = List of event id's
        // 2. Loop each event of a team and then get event data from Occurrences and events collection along with user information
        // 3. Make an entry in EventMessage collection to track the status of message sent
        // 4. Create a Hero card to be sent in a team
        // 5. Add entry in a list of EventNotificationCardPayload. This list is used to alphabetically order the user's name
        // 6. If there are multiple events to send to a team, send them in a carousel of up to 6 events
        // 7. Send Event card in team
        // 8. Delete entry from occurrences collection
        private async Task SendEventNotifications()
        {
            foreach (var teamId in this.teamToEventNotificationsMap.Keys)
            {
                var eventsToSend = this.teamToEventNotificationsMap[teamId];

                // Check if this team ID is known
                var teamInfo = this.userManagementHelper.GetTeamsDetailsByTeamIdAsync(teamId);
                if (teamInfo == null)
                {
                    // Skip this team
                    continue;
                }

                // Batch the events in groups of MaxEventsPerCarousel
                var eventBatches = eventsToSend.Select((item, inx) => new { item, inx })
                    .GroupBy(x => x.inx / MaxEventsPerCarousel)
                    .Select(g => g.Select(x => x.item).ToList());

                // Send each batch, either as a carousel or as individual cards, depending on the count
                foreach (var eventBatch in eventBatches)
                {
                    try
                    {
                        if (eventBatch.Count > MaxEventsToSendIndividually)
                        {
                            // Send the events in a carousel
                            await this.SendMultipleEventNotificationsAsync(teamId, eventBatch);
                        }
                        else
                        {
                            // Send the events individually
                            await Task.WhenAll(
                                eventBatch.Select(notification => this.SendSingleEventNotificationAsync(teamId, notification)));
                        }
                    }
                    catch (ErrorResponseException ex)
                    {
                        // We might get a 404 if the team has been deleted
                        if (ex.Response.StatusCode == HttpStatusCode.NotFound)
                        {
                            await this.userManagementHelper.DeleteTeamDetailsAsync(teamId);
                            await this.userManagementHelper.DeleteUserTeamMembershipByTeamIdAsync(teamId);
                        }
                    }
                }
            }
        }

        // Send the list of events as a carousel card
        private async Task SendMultipleEventNotificationsAsync(string teamId, List<EventNotificationData> notifications)
        {
            var firstNotification = notifications.First();

            // Build the attachments and entities
            notifications.Sort((left, right) => StringComparer.InvariantCulture.Compare(left.User.DisplayName, right.User.DisplayName));
            var attachments = notifications
                .Select(x => x.GetCard())
                .ToList();
            var entities = notifications
                .Select(x => x.GetMention())
                .Distinct(new MentionEqualityComparer())
                .AsEnumerable<Entity>()
                .ToList();

            // Build the text message
            string allButLastEvent = string.Join(
                ", ",
                notifications.Take(notifications.Count - 1).Select(x => x.GetMessage()));
            var message = string.Format(Strings.MultipleEventsMessage, allButLastEvent, notifications[notifications.Count - 1].GetMessage());

            // Create the activity
            var activity = new Activity(ActivityTypes.Message)
            {
                Conversation = new ConversationAccount { Id = teamId },
                AttachmentLayout = AttachmentLayoutTypes.Carousel,
                Text = message,
                Summary = Strings.MultipleEventsSummary,
                Attachments = attachments,
                Entities = entities,
                ServiceUrl = firstNotification.User.ServiceUrl,
            };

            // Add new entry to EventMessages collection for message type "preview" to track the status of message sent
            var eventMessage = new EventMessage
            {
                EventId = null,         // This is a collection of events, so there is no single event or occurrence ID
                OccurrenceId = null,
                Activity = activity,
                TenantId = firstNotification.User.TenantId,
                MessageType = MessageType.Event,
                ExpireAt = firstNotification.Occurrence.GetLastAllowableTimeToSendNotification(),
            };
            eventMessage = await this.eventDataProvider.AddEventMessageAsync(eventMessage);

            // Send the message
            try
            {
                await eventMessage.SendAsync(this.connectorClientFactory);
                this.logProvider.LogInfo($"Event notifications sent to {teamId} for events {string.Join(",", notifications.Select(x => x.Event.Id))}");
            }
            catch (Exception ex)
            {
                this.logProvider.LogError($"Failed to send notifications to team {teamId}", ex, new Dictionary<string, string>
                    {
                        { "EventId", eventMessage.EventId },
                        { "OccurrenceId", eventMessage.OccurrenceId },
                        { "TeamId", teamId },
                        { "LastAttemptTime", DateTimeOffset.UtcNow.ToString() },
                        { "LastAttemptStatusCode", eventMessage.MessageSendResult?.StatusCode.ToString() },
                        { "ResponseBody", eventMessage.MessageSendResult?.ResponseBody },
                    });
                throw;
            }
            finally
            {
                // Record the result of the send
                await this.eventDataProvider.UpdateEventMessageAsync(eventMessage);
            }
        }

        // Send the event individually
        private async Task SendSingleEventNotificationAsync(string teamId, EventNotificationData notification)
        {
            // Ideally this should send a single message with text and card, but because of message splitting,
            // such a message is split into two (text-only and card-only). As a workaround, we start a reply chain
            // with the text message, then send the card as a reply to this post.

            // Create the activity
            var activity = new Activity(ActivityTypes.Message)
            {
                Conversation = new ConversationAccount { Id = teamId },
                AttachmentLayout = AttachmentLayoutTypes.Carousel,
                Text = notification.GetMessage(),
                Summary = Strings.MultipleEventsSummary,
                Attachments = new List<Attachment> { notification.GetCard() },
                Entities = new List<Entity> { notification.GetMention() },
                ServiceUrl = notification.User.ServiceUrl,
            };

            // Add new entry to EventMessages collection for message type "preview" to track the status of message sent
            var eventMessage = new EventMessage
            {
                EventId = notification.Event.Id,
                OccurrenceId = notification.Occurrence.Id,
                Activity = activity,
                TenantId = notification.User.TenantId,
                MessageType = MessageType.Event,
                ExpireAt = notification.Occurrence.GetLastAllowableTimeToSendNotification(),
            };
            eventMessage = await this.eventDataProvider.AddEventMessageAsync(eventMessage);

            // Send the message
            try
            {
                await eventMessage.SendAsync(this.connectorClientFactory);
                this.logProvider.LogInfo($"Event notifications sent to {teamId} for event {notification.Event.Id}");
            }
            catch (Exception ex)
            {
                this.logProvider.LogError($"Failed to send notifications to team {teamId}", ex, new Dictionary<string, string>
                    {
                        { "EventId", eventMessage.EventId },
                        { "OccurrenceId", eventMessage.OccurrenceId },
                        { "TeamId", teamId },
                        { "LastAttemptTime", DateTimeOffset.UtcNow.ToString() },
                        { "LastAttemptStatusCode", eventMessage.MessageSendResult?.StatusCode.ToString() },
                        { "ResponseBody", eventMessage.MessageSendResult?.ResponseBody },
                    });
                throw;
            }
            finally
            {
                // Record the result of the send
                await this.eventDataProvider.UpdateEventMessageAsync(eventMessage);
            }
        }

        // Update event list for given team id
        private void AddEventNotificationForTeam(string teamId, EventNotificationData eventNotificationData)
        {
            if (this.teamToEventNotificationsMap.TryGetValue(teamId, out List<EventNotificationData> events))
            {
                events.Add(eventNotificationData);
            }
            else
            {
                this.teamToEventNotificationsMap.Add(teamId, new List<EventNotificationData> { eventNotificationData });
            }
        }

        // Mark the given occurrence as already sent
        private Task MarkEventOccurrenceAsSentAsync(EventOccurrence occurrence)
        {
            occurrence.Status = EventStatus.Sent;
            return this.eventDataProvider.UpdateEventOccurrenceAsync(occurrence);
        }

        // Equality comparer for Mentions
        private class MentionEqualityComparer : IEqualityComparer<Mention>
        {
            public bool Equals(Mention x, Mention y)
            {
                return x.Mentioned.Id == y.Mentioned.Id;
            }

            public int GetHashCode(Mention obj)
            {
                return obj.Mentioned.Id.GetHashCode();
            }
        }
    }
}
