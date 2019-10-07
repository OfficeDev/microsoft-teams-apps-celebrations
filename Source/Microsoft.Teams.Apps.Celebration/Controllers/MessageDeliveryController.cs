// <copyright file="MessageDeliveryController.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.Celebration.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Threading.Tasks;
    using System.Web.Hosting;
    using System.Web.Http;
    using Microsoft.Teams.Apps.Celebration.Helpers;
    using Microsoft.Teams.Apps.Celebration.Models;
    using Microsoft.Teams.Apps.Common.Extensions;
    using Microsoft.Teams.Apps.Common.Logging;

    /// <summary>
    /// Controller that implements reliable message delivery
    /// </summary>
    [SharedSecretAuthentication(SharedSecretSettingName = "AppApiKey")]
    public class MessageDeliveryController : ApiController
    {
        private static readonly List<int> StatusCodesToRetry = new List<int> { 429, 500, 502, 503, 504 };

        private readonly IEventDataProvider eventDataProvider;
        private readonly ILogProvider logProvider;
        private readonly IConnectorClientFactory connectorClientFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageDeliveryController"/> class.
        /// </summary>
        /// <param name="eventDataProvider">Event data provider instance</param>
        /// <param name="logProvider">The instance of <see cref="ILogProvider"/></param>
        /// <param name="connectorClientFactory">Connector client factory</param>
        public MessageDeliveryController(IEventDataProvider eventDataProvider, ILogProvider logProvider, IConnectorClientFactory connectorClientFactory)
        {
            this.eventDataProvider = eventDataProvider;
            this.logProvider = logProvider;
            this.connectorClientFactory = connectorClientFactory;
        }

        /// <summary>
        /// Process incoming request to send the reminder for upcoming events
        /// </summary>
        /// <returns>A <see cref="Task"/>Representing the asynchronous operation</returns>
        public IHttpActionResult Post()
        {
            HostingEnvironment.QueueBackgroundWorkItem(ct => this.DeliverMessagesAsync());
            return this.StatusCode(HttpStatusCode.OK);
        }

        /// <summary>
        /// Reliable message delivery
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        private async Task DeliverMessagesAsync()
        {
            try
            {
                this.logProvider.LogInfo("Starting background work item to deliver messages");

                var eventMessages = await this.eventDataProvider.GetEventMessagesByDeliveryStatusCodeAsync(StatusCodesToRetry);
                if (eventMessages.Count > 0)
                {
                    this.logProvider.LogInfo($"Found {eventMessages.Count} messages to deliver");
                }
                else
                {
                    this.logProvider.LogInfo($"No messages need to be delivered, exiting");
                    return;
                }

                await this.SendEventMessagesAsync(eventMessages);
            }
            catch (Exception ex)
            {
                this.logProvider.LogError($"Error delivering messages: {ex.Message}", ex);
                throw;
            }
            finally
            {
                this.logProvider.LogInfo("Finished background work item to deliver messages");
            }
        }

        /// <summary>
        /// Send event card
        /// </summary>
        /// <param name="eventMessages">List of EventMessage</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation</returns>
        private async Task SendEventMessagesAsync(IList<EventMessage> eventMessages)
        {
            var tasks = eventMessages.Select(message => this.SendEventMessageAsync(message));
            await Task.WhenAll(tasks);
        }

        private async Task SendEventMessageAsync(EventMessage eventMessage)
        {
            // Send the message
            try
            {
                await eventMessage.SendAsync(this.connectorClientFactory);
                this.logProvider.LogInfo($"Message {eventMessage.Id} sent successfully");
            }
            catch (Exception ex)
            {
                this.logProvider.LogError($"Failed to send message {eventMessage.Id}", ex, new Dictionary<string, string>
                    {
                        { "EventId", eventMessage.EventId },
                        { "OccurrenceId", eventMessage.OccurrenceId },
                        { "ConversationId", eventMessage.Activity.Conversation?.Id },
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
    }
}
