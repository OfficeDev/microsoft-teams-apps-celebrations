// <copyright file="SkipEventDialog.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.Celebration.Dialog
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.Bot.Builder.Dialogs;
    using Microsoft.Bot.Connector;
    using Microsoft.Teams.Apps.Celebration.Helpers;
    using Microsoft.Teams.Apps.Celebration.Models;
    using Microsoft.Teams.Apps.Celebration.Resources;
    using Microsoft.Teams.Apps.Common.Extensions;
    using Microsoft.Teams.Apps.Common.Logging;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Handle the event skip action
    /// </summary>
    [Serializable]
    public class SkipEventDialog : IDialog<object>
    {
        private readonly IEventDataProvider eventDataProvider;
        private readonly IConnectorClient connectorClient;
        private readonly ILogProvider logProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="SkipEventDialog"/> class.
        /// </summary>
        /// <param name="connectorClient">Connector client</param>
        /// <param name="eventDataProvider">Event data provider instance</param>
        /// <param name="logProvider">Logging component</param>
        public SkipEventDialog(IConnectorClient connectorClient, IEventDataProvider eventDataProvider, ILogProvider logProvider)
        {
            this.connectorClient = connectorClient;
            this.eventDataProvider = eventDataProvider;
            this.logProvider = logProvider;
        }

        /// <inheritdoc/>
        public Task StartAsync(IDialogContext context)
        {
            context.Wait(this.OnMessageReceivedAsync);
            return Task.CompletedTask;
        }

        // Handle the incoming message
        private async Task OnMessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> activity)
        {
            var message = await activity;
            await this.HandleSkipEventAsync(context, message);

            // Ensure that we exit from the dialog
            context.Done<object>(null);
        }

        // Handles event skip action
        private async Task HandleSkipEventAsync(IDialogContext context, IMessageActivity message)
        {
            var payload = ((JObject)message?.Value)?.ToObject<PreviewCardPayload>();
            if (payload == null)
            {
                this.logProvider.LogInfo("Received message with no payload");
                return;
            }

            // Get the event
            var celebrationEvent = await this.eventDataProvider.GetEventByIdAsync(payload.EventId, payload.OwnerAadObjectId);
            if (celebrationEvent == null)
            {
                this.logProvider.LogInfo($"Could not find event {payload.EventId} for user {payload.OwnerAadObjectId}");
                await context.PostAsync(Strings.EventDoesNotExistMessage);
                return;
            }

            // Get the occurrence
            var occurrence = await this.eventDataProvider.GetEventOccurrenceByIdAsync(payload.OccurrenceId, payload.EventId);
            if (occurrence == null)
            {
                this.logProvider.LogInfo($"Could not find occurrence {payload.OccurrenceId} of event {payload.EventId} for user {payload.OwnerAadObjectId} (likely out of date card)");
                await context.PostAsync(Strings.SkippedStaleEventMessage);
                return;
            }

            // Check that the occurrence is still in the future
            if (occurrence.DateTime < DateTimeOffset.UtcNow)
            {
                await context.PostAsync(Strings.EventPassedMessage);
                return;
            }

            // Mark the occurrence as skipped
            occurrence.Status = EventStatus.Skipped;
            await this.eventDataProvider.UpdateEventOccurrenceAsync(occurrence);

            // Update the card
            var updatedMessage = context.MakeMessage();
            updatedMessage.Attachments.Add(CelebrationCard.GetPreviewCard(celebrationEvent, payload.OccurrenceId, payload.OwnerName, false).ToAttachment());
            await this.connectorClient.Conversations.UpdateActivityAsync(message.Conversation.Id, message.ReplyToId, (Activity)updatedMessage);

            await context.PostAsync(string.Format(Strings.EventSkippedMessage, celebrationEvent.Title));
        }
    }
}
