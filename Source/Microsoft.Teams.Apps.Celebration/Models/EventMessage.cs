// <copyright file="EventMessage.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.Celebration.Models
{
    using System;
    using System.ComponentModel;
    using System.Net;
    using System.Threading.Tasks;
    using System.Web;
    using Microsoft.Azure.Documents;
    using Microsoft.Bot.Connector;
    using Microsoft.Teams.Apps.Celebration.Helpers;
    using Newtonsoft.Json;

    /// <summary>
    /// Represents a message to be sent to a team or user
    /// </summary>
    public class EventMessage : Resource
    {
        /// <summary>
        /// Gets or sets event id, which is Id(Guid) in events collection
        /// </summary>
        [JsonProperty("eventId")]
        public string EventId { get; set; }

        /// <summary>
        /// Gets or sets occurrenceId, which is Id(Guid) in Occurrence collection
        /// </summary>
        [JsonProperty("occurrenceId")]
        public string OccurrenceId { get; set; }

        /// <summary>
        /// Gets or sets UTC time of upcoming occurrence
        /// </summary>
        [JsonProperty("occurrenceDateTime")]
        public DateTimeOffset OccurrenceDateTime { get; set; }

        /// <summary>
        /// Gets or sets activity which requires to construct the celebration card
        /// </summary>
        [JsonProperty("activity")]
        public Activity Activity { get; set; }

        /// <summary>
        /// Gets or sets tenant id
        /// </summary>
        [JsonProperty("tenantId")]
        public string TenantId { get; set; }

        /// <summary>
        /// Gets or sets messageType
        /// </summary>
        [JsonProperty("messageType")]
        [DefaultValue(MessageType.Unknown)]
        public MessageType MessageType { get; set; }

        /// <summary>
        /// Gets or sets sent message result
        /// </summary>
        [JsonProperty("messageSendResult")]
        public MessageSendResult MessageSendResult { get; set; }

        /// <summary>
        /// Gets or sets expiration time at which bot should give up retry to send card
        /// </summary>
        [JsonProperty("expireAt")]
        public DateTimeOffset ExpireAt { get; set; }

        /// <summary>
        /// Gets or sets the time to live in seconds. See https://docs.microsoft.com/en-us/azure/cosmos-db/time-to-live.
        /// </summary>
        [JsonProperty(PropertyName = "ttl", NullValueHandling = NullValueHandling.Ignore)]
        public int? TimeToLive { get; set; }

        /// <summary>
        /// Sends the message.
        /// </summary>
        /// <param name="connectorClientFactory">Connector client factory</param>
        /// <returns>Tracking task</returns>
        public async Task<ResourceResponse> SendAsync(IConnectorClientFactory connectorClientFactory)
        {
            if (string.IsNullOrEmpty(this.Activity.ServiceUrl))
            {
                throw new InvalidOperationException("The service URL must be set");
            }

            var client = connectorClientFactory.GetConnectorClient(this.Activity.ServiceUrl);

            try
            {
                var response = await this.SendAsyncWorker(client.Conversations);
                this.MessageSendResult = new MessageSendResult()
                {
                    LastAttemptTime = DateTime.UtcNow,
                    StatusCode = (int)HttpStatusCode.OK,
                    ResponseBody = string.Empty,
                };
                return response;
            }
            catch (HttpException httpException)
            {
                this.MessageSendResult = new MessageSendResult()
                {
                    LastAttemptTime = DateTime.UtcNow,
                    StatusCode = httpException.GetHttpCode(),
                    ResponseBody = httpException.GetHtmlErrorMessage(),
                };
                throw;
            }
            catch (ErrorResponseException errorResponseException)
            {
                this.MessageSendResult = new MessageSendResult()
                {
                    LastAttemptTime = DateTime.UtcNow,
                    StatusCode = (int)errorResponseException.Response.StatusCode,
                    ResponseBody = errorResponseException.Response.Content,
                };
                throw;
            }
            catch (Exception ex)
            {
                this.MessageSendResult = new MessageSendResult()
                {
                    LastAttemptTime = DateTime.UtcNow,
                    StatusCode = -1,
                    ResponseBody = ex.ToString(),
                };
                throw;
            }
        }

        private async Task<ResourceResponse> SendAsyncWorker(IConversations conversations)
        {
            // Create conversation if needed
            if (this.Activity.Conversation?.Id == null)
            {
                var conversationId = await conversations.CreateOrGetDirectConversationAsync(this.TenantId, this.Activity.Recipient.Id);
                this.Activity.Conversation = new ConversationAccount { Id = conversationId };
            }

            // Is this activity going to split? If so, send it as a reply chain
            if ((this.MessageType == MessageType.Event) && this.IsActivitySubjectToSplitting(this.Activity))
            {
                // Create the reply chain
                var rootMessage = new Activity(ActivityTypes.Message)
                {
                    Text = this.Activity.Text,
                    Entities = this.Activity.Entities,
                };
                var conversationResource = await conversations.CreateReplyChainAsync(this.Activity.Conversation.Id, rootMessage);

                // Post the cards as a messages in the reply chain
                ResourceResponse response = null;
                foreach (var attachment in this.Activity.Attachments)
                {
                    response = await conversations.SendCardAsync(conversationResource.Id, attachment);
                }

                // There's no single resource that can be returned, so just return the last one
                return response;
            }
            else
            {
                return await conversations.SendToConversationWithRetriesAsync(this.Activity);
            }
        }

        private bool IsActivitySubjectToSplitting(Activity activity)
        {
            if (activity.Attachments?.Count == 0)
            {
                // Activity with no attachments is never split
                return false;
            }

            switch (activity.AttachmentLayout)
            {
                case AttachmentLayoutTypes.Carousel:
                    return (activity.Text != null) && (activity.Attachments.Count == 1);

                case AttachmentLayoutTypes.List:
                default:
                    return (activity.Text != null) || (activity.Attachments.Count > 0);
            }
        }
    }
}