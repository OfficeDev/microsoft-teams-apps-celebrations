// <copyright file="ShareEventDialog.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.Celebration.Dialog
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Bot.Builder.Dialogs;
    using Microsoft.Bot.Connector;
    using Microsoft.Bot.Connector.Teams;
    using Microsoft.Teams.Apps.Celebration.Helpers;
    using Microsoft.Teams.Apps.Celebration.Models;
    using Microsoft.Teams.Apps.Celebration.Resources;
    using Microsoft.Teams.Apps.Common.Extensions;
    using Microsoft.Teams.Apps.Common.Logging;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Share event with team
    /// </summary>
    [Serializable]
    public class ShareEventDialog : IDialog<object>
    {
        [NonSerialized]
        private readonly IEventDataProvider eventDataProvider;
        private readonly IConnectorClient connectorClient;
        private readonly ILogProvider logProvider;
        [NonSerialized]
        private readonly IUserManagementHelper userManagementHelper;

        /// <summary>
        /// Initializes a new instance of the <see cref="ShareEventDialog"/> class.
        /// </summary>
        /// <param name="connectorClient">Connector client</param>
        /// <param name="eventDataProvider">Event data provider instance</param>
        /// <param name="userManagementHelper">UserManagementHelper instance</param>
        /// <param name="logProvider">Logging component</param>
        public ShareEventDialog(IConnectorClient connectorClient, IEventDataProvider eventDataProvider, IUserManagementHelper userManagementHelper, ILogProvider logProvider)
        {
            this.eventDataProvider = eventDataProvider;
            this.connectorClient = connectorClient;
            this.logProvider = logProvider;
            this.userManagementHelper = userManagementHelper;
        }

        /// <summary>
        /// The start of the code that represents the conversational dialog
        /// </summary>
        /// <param name="context">The dialog context</param>
        /// <returns> A task that represents the dialog start</returns>
        public Task StartAsync(IDialogContext context)
        {
            context.Wait(this.HandleEventShareAction);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Handles event Share action
        /// </summary>
        /// <param name="context">IDialogContext object</param>
        /// <param name="activity">IAwaitable message activity</param>
        /// <returns>Task.</returns>
        public async Task HandleEventShareAction(IDialogContext context, IAwaitable<IMessageActivity> activity)
        {
            var message = (Activity)await activity;
            var replyMessage = string.Empty;
            if (message.Value != null)
            {
                ShareEventPayload shareEventPayload = ((JObject)message.Value).ToObject<ShareEventPayload>();

                try
                {
                    var teamMembers = await this.connectorClient.Conversations.GetConversationMembersAsync(shareEventPayload.TeamId);
                    var user = teamMembers.Select(x => x.AsTeamsChannelAccount()).Where(x => x.ObjectId == shareEventPayload.UserAadObjectId).FirstOrDefault();

                    // Fetch teamDetails from DB to check if bot is still present or un-installed from team
                    var document = await this.userManagementHelper.GetTeamsDetailsByTeamIdAsync(shareEventPayload.TeamId);

                    if (user == null)
                    {
                        replyMessage = string.Format(Strings.ShareWithTeamNotAMemberError, shareEventPayload.TeamName);
                    }
                    else if (document == null)
                    {
                        replyMessage = string.Format(Strings.ShareWithTeamsNotInstalledError, shareEventPayload.TeamName);
                    }
                    else
                    {
                        // Fetch all the events of user and share with team
                        var celebrationEvents = await this.eventDataProvider.GetEventsByOwnerObjectIdAsync(shareEventPayload.UserAadObjectId);
                        if (celebrationEvents.Count > 0)
                        {
                            foreach (var celebrationEvent in celebrationEvents)
                            {
                                celebrationEvent.Teams.Add(new Team { Id = shareEventPayload.TeamId });
                                await this.eventDataProvider.UpdateEventAsync(celebrationEvent);
                            }
                        }

                        replyMessage = Strings.ShareWithTeamSuccessMessage;

                        // Update the card
                        IMessageActivity updatedMessage = context.MakeMessage();
                        updatedMessage.Attachments.Add(CelebrationCard.GetShareEventAttachmentWithoutActionButton(shareEventPayload.TeamName));
                        updatedMessage.ReplyToId = message.ReplyToId;
                        await this.connectorClient.Conversations.UpdateActivityAsync(message.Conversation.Id, message.ReplyToId, (Activity)updatedMessage);
                    }
                }
                catch (Exception ex)
                {
                    this.logProvider.LogError("Failed to share the existing event with team", ex, new Dictionary<string, string>()
                    {
                        { "TeamId", shareEventPayload.TeamId },
                        { "UserAadObjectId", shareEventPayload.UserAadObjectId },
                    });
                    replyMessage = Strings.ShareWithTeamGenericError;
                }

                await context.PostAsync(replyMessage);
                context.Done<object>(null);
            }
        }
    }
}