// <copyright file="PreviewController.cs" company="Microsoft">
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
    using Microsoft.Teams.Apps.Common.Configuration;
    using Microsoft.Teams.Apps.Common.Extensions;
    using Microsoft.Teams.Apps.Common.Logging;

    /// <summary>
    /// Controller that handles the request to send the reminder for upcoming event
    /// </summary>
    [SharedSecretAuthentication(SharedSecretSettingName = "AppApiKey")]
    public class PreviewController : ApiController
    {
        private readonly IEventDataProvider eventDataProvider;
        private readonly IUserManagementHelper userManagementHelper;
        private readonly ILogProvider logProvider;
        private readonly IConnectorClientFactory connectorClientFactory;
        private readonly TimeSpan timeToPostPreview;
        private readonly int daysInAdvanceToSendEventPreview;
        private readonly TimeSpan minimumTimeToProcessEvent;

        /// <summary>
        /// Initializes a new instance of the <see cref="PreviewController"/> class.
        /// </summary>
        /// <param name="eventDataProvider">Event data provider instance.</param>
        /// <param name="userManagementHelper">UserManagementHelper instance</param>
        /// <param name="logProvider">Log provider instance</param>
        /// <param name="connectorClientFactory">The connector client factory</param>
        /// <param name="configProvider">Configuration provider instance</param>
        public PreviewController(IEventDataProvider eventDataProvider, IUserManagementHelper userManagementHelper, ILogProvider logProvider, IConnectorClientFactory connectorClientFactory, IConfigProvider configProvider)
        {
            this.eventDataProvider = eventDataProvider;
            this.userManagementHelper = userManagementHelper;
            this.logProvider = logProvider;
            this.connectorClientFactory = connectorClientFactory;
            this.timeToPostPreview = TimeSpan.Parse(configProvider.GetSetting(ApplicationConfig.TimeToPostCelebration), CultureInfo.InvariantCulture);
            this.daysInAdvanceToSendEventPreview = Convert.ToInt32(configProvider.GetSetting(ApplicationConfig.DaysInAdvanceToSendEventPreview));
            this.minimumTimeToProcessEvent = TimeSpan.FromHours(Convert.ToInt32(configProvider.GetSetting(ApplicationConfig.MinTimeToProcessEventInHours)));
        }

        /// <summary>
        /// Process incoming request to send the reminder for upcoming events
        /// </summary>
        /// <param name="effectiveDateTime">The effective current date and time</param>
        /// <returns>A <see cref="Task"/>Representing the asynchronous operation</returns>
        public IHttpActionResult Post([FromUri] string effectiveDateTime = "")
        {
            this.logProvider.LogInfo($"Processing trigger to send event reminders. effectiveDateTime: {effectiveDateTime}");

            if (!DateTimeOffset.TryParse(effectiveDateTime, null, DateTimeStyles.AdjustToUniversal, out DateTimeOffset currentDateTime))
            {
                currentDateTime = DateTimeOffset.UtcNow;
            }

            HostingEnvironment.QueueBackgroundWorkItem(ct => this.SendEventRemindersAsync(currentDateTime));

            return this.StatusCode(HttpStatusCode.OK);
        }

        // 1. Get all the events from Events collection which are coming in next 72 hours then filter out the events for which reminder has already been sent
        // 2. Add an entry in Occurrences collection for upcoming event(This collection has entry for all the events which are coming in next 72 hour and exist until the notification is sent in selected teams)
        // 3. Add an entry to EventMessages collection to track the status of message sent
        // 3. Send the reminder to owner of the event in the form of card with skip and edit action button(if upcoming event date is (> 24 hrs and <72 hrs))
        // 4. Update the Message sent status in EventMessages collection
        private async Task SendEventRemindersAsync(DateTimeOffset currentDateTimeOffset)
        {
            try
            {
                this.logProvider.LogInfo("Starting background work item to send event reminders");

                // Get the events with occurrences coming up in the next 72 hours
                var upcomingEvents = (await this.eventDataProvider.GetEventsAsync(this.GetEventQuery(currentDateTimeOffset.Date))).ToList();

                // Remove events that are not shared with anyone
                upcomingEvents = upcomingEvents.Where(x => x.Teams?.Count() > 0).ToList();

                if (upcomingEvents.Count > 0)
                {
                    this.logProvider.LogInfo($"Found {upcomingEvents.Count} shared events occurring in the next 72 hours");
                }
                else
                {
                    this.logProvider.LogInfo($"No shared events are occurring in the next 72 hours, exiting");
                    return;
                }

                // Remove events whose occurrences have already been processed
                var trackedOccurrences = await this.eventDataProvider.GetEventOccurrencesByEventIdsAsync(upcomingEvents.Select(x => x.Id).ToList());
                this.logProvider.LogInfo($"Found {trackedOccurrences.Count} events for which reminder has already sent");

                upcomingEvents.RemoveAll(x => trackedOccurrences.Any(occurrence => occurrence.EventId == x.Id));
                if (upcomingEvents.Count == 0)
                {
                    this.logProvider.LogInfo("Reminders have been sent for all events, exiting");
                    return;
                }

                // Process each event and make an entry in Occurrences collection to send reminder
                var reminderTasks = upcomingEvents.Select(@event => this.SendEventReminderAsync(@event, currentDateTimeOffset));
                await Task.WhenAll(reminderTasks);
            }
            catch (Exception ex)
            {
                this.logProvider.LogError($"Error sending event reminders: {ex.Message}", ex);
                throw;
            }
            finally
            {
                this.logProvider.LogInfo("Finished background work item to send event reminders");
            }
        }

        // Send a messsage to the owner of the given event, reminding them that their event is coming up
        private async Task SendEventReminderAsync(CelebrationEvent celebrationEvent, DateTimeOffset currentDateTimeOffset)
        {
            this.logProvider.LogInfo($"Sending reminder for event {celebrationEvent.Id} (owner={celebrationEvent.OwnerAadObjectId}, date={celebrationEvent.Date.ToShortDateString()})");

            // Determine the next occurrence of the event
            var deliveryTimeZone = TimeZoneInfo.FindSystemTimeZoneById(celebrationEvent.TimeZoneId);
            var currentTimeInDeliveryTimeZone = TimeZoneInfo.ConvertTimeFromUtc(currentDateTimeOffset.UtcDateTime, deliveryTimeZone);
            var upcomingEventDateTime = Common.GetNextOccurrenceAfterDateTime(celebrationEvent.Date.Add(this.timeToPostPreview), currentTimeInDeliveryTimeZone);
            var upcomingEventDateTimeInUTC = TimeZoneInfo.ConvertTimeToUtc(upcomingEventDateTime, deliveryTimeZone);

            // Do not send reminder if the next occurrence is not in the window
            var timeUntilNextOccurrence = upcomingEventDateTimeInUTC - currentDateTimeOffset;
            if ((timeUntilNextOccurrence.TotalMinutes < 0) ||
                (upcomingEventDateTimeInUTC - currentDateTimeOffset) > TimeSpan.FromDays(this.daysInAdvanceToSendEventPreview))
            {
                this.logProvider.LogInfo($"Next occurrence of event {celebrationEvent.Id} is not in the next {this.daysInAdvanceToSendEventPreview} days");
                return;
            }

            // Add an entry to Occurrence collection for the event, so we know that we processed it
            var eventOccurrence = new EventOccurrence
            {
                EventId = celebrationEvent.Id,
                OwnerAadObjectId = celebrationEvent.OwnerAadObjectId,
                DateTime = upcomingEventDateTimeInUTC,
            };
            eventOccurrence = await this.eventDataProvider.AddEventOccurrenceAsync(eventOccurrence);

            // Do not send reminder if we are within the period specified in the configuration
            if ((upcomingEventDateTimeInUTC - currentDateTimeOffset) < this.minimumTimeToProcessEvent)
            {
                this.logProvider.LogInfo($"Not sending reminder for event {celebrationEvent.Id} which is due in less than {(int)this.minimumTimeToProcessEvent.TotalHours} hours");
                return;
            }

            // Get event owner information
            var user = await this.userManagementHelper.GetUserByAadObjectIdAsync(celebrationEvent.OwnerAadObjectId);
            await this.EnsureConversationWithUserAsync(user);

            // Send reminder of event to the owner
            var previewCard = CelebrationCard.GetPreviewCard(celebrationEvent, eventOccurrence.Id, user.DisplayName);
            var message = string.Format(Strings.EventPreviewMessageText, user.DisplayName);
            var activity = new Activity(ActivityTypes.Message)
            {
                Conversation = new ConversationAccount { Id = user.ConversationId },
                Recipient = new ChannelAccount { Id = user.TeamsId },
                Text = message,
                Summary = message,
                Attachments = new List<Attachment> { previewCard.ToAttachment() },
                ServiceUrl = user.ServiceUrl,
            };

            // Add new entry to EventMessages collection for message type "preview" to track the status of message sent
            var eventMessage = new EventMessage
            {
                EventId = celebrationEvent.Id,
                OccurrenceId = eventOccurrence.Id,
                Activity = activity,
                TenantId = user.TenantId,
                MessageType = MessageType.Preview,
                ExpireAt = upcomingEventDateTimeInUTC.AddHours(24),
            };
            eventMessage = await this.eventDataProvider.AddEventMessageAsync(eventMessage);

            // Send the message
            try
            {
                await eventMessage.SendAsync(this.connectorClientFactory);
                this.logProvider.LogInfo($"Reminder message sent to the owner of event {celebrationEvent.Id}");
            }
            catch (Exception ex)
            {
                this.logProvider.LogError($"Failed to send reminder for event {celebrationEvent.Id}", ex, new Dictionary<string, string>
                    {
                        { "EventId", eventMessage.EventId },
                        { "OccurrenceId", eventMessage.OccurrenceId },
                        { "ConversationId", user.ConversationId },
                        { "OccurrenceDateTime", eventOccurrence.DateTime.ToString() },
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

        // Prepare and return query to events collection to get upcoming events
        private string GetEventQuery(DateTime currentDateTime)
        {
            return "select * from Events where " +
                string.Join(" or ", this.GetReferenceDateCollection(currentDateTime).Select(x => $"(Events.eventMonth={x.Item1} and Events.eventDay={x.Item2})"));
        }

        // Create reference set to get the upcoming events (in next 72 hours)
        private List<Tuple<int, int>> GetReferenceDateCollection(DateTime currentDateTime)
        {
            var monthDayReferenceSet = new List<Tuple<int, int>>();

            // Add month and day part in reference set for events which are coming in next 72 hours
            for (int i = 0; i < this.daysInAdvanceToSendEventPreview; i++)
            {
                var potentialReferenceDate = currentDateTime.AddDays(i);
                monthDayReferenceSet.Add(Tuple.Create(potentialReferenceDate.Month, potentialReferenceDate.Day));
            }

            // Add 29th Feb in reference set if the current year is not leap year. so, the events which occurs on 29th Feb would not get skipped this year
            if (!DateTime.IsLeapYear(currentDateTime.Year)
                && currentDateTime.Month == 2
                && currentDateTime.Day <= 29
                && 29 - currentDateTime.Day < this.daysInAdvanceToSendEventPreview)
            {
                monthDayReferenceSet.Add(Tuple.Create(2, 29));
            }

            return monthDayReferenceSet;
        }

        // Ensure that a conversation exists with the given user
        private async Task EnsureConversationWithUserAsync(User user)
        {
            if (string.IsNullOrWhiteSpace(user.ConversationId))
            {
                this.logProvider.LogInfo($"Creating 1:1 conversation with user {user.Id}");
                var client = this.connectorClientFactory.GetConnectorClient(user.ServiceUrl);
                user.ConversationId = await client.Conversations.CreateOrGetDirectConversationAsync(user.TenantId, user.TeamsId);
                await this.userManagementHelper.SaveUserAsync(user);
            }
        }
    }
}
