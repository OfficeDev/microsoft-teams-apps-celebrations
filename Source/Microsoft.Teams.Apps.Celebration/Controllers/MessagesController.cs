// <copyright file="MessagesController.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.Celebration
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;
    using System.Web;
    using System.Web.Http;
    using Autofac;
    using Microsoft.Bot.Builder.Dialogs;
    using Microsoft.Bot.Builder.Dialogs.Internals;
    using Microsoft.Bot.Connector;
    using Microsoft.Bot.Connector.Teams;
    using Microsoft.Bot.Connector.Teams.Models;
    using Microsoft.Teams.Apps.Celebration.Dialog;
    using Microsoft.Teams.Apps.Celebration.Helpers;
    using Microsoft.Teams.Apps.Celebration.Models;
    using Microsoft.Teams.Apps.Common.Extensions;
    using Microsoft.Teams.Apps.Common.Logging;
    using Microsoft.Teams.Apps.Common.Telemetry;

    /// <summary>
    /// Controller that handles all the incoming request sent to bot
    /// </summary>
    [BotAuthentication]
    public class MessagesController : ApiController
    {
        private readonly ILogProvider logProvider;
        private readonly IUserManagementHelper userManagementHelper;
        private readonly IEventDataProvider eventDataProvider;
        private IConnectorClient connectorClient;
        private IList<ChannelAccount> teamMembers;

        /// <summary>
        /// Initializes a new instance of the <see cref="MessagesController"/> class.
        /// </summary>
        /// <param name="logProvider">The instance of <see cref="ILogProvider"/></param>
        /// <param name="userManagementHelper">UserManagementHelper instance</param>
        /// <param name="eventDataProvider">Event data provider instance</param>
        public MessagesController(ILogProvider logProvider, IUserManagementHelper userManagementHelper, IEventDataProvider eventDataProvider)
        {
            this.logProvider = logProvider;
            this.userManagementHelper = userManagementHelper;
            this.eventDataProvider = eventDataProvider;
        }

        /// <summary>
        /// Receives message from user and reply to it
        /// </summary>
        /// <param name="activity">Activity object</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation</returns>
        public async Task<HttpResponseMessage> Post([FromBody]Activity activity)
        {
            try
            {
                UserTelemetryInitializer.SetTelemetryUserId(HttpContext.Current, activity.From.Id);
                this.LogUserActivity(activity);

                using (var dialogScope = DialogModule.BeginLifetimeScope(Conversation.Container, activity))
                {
                    this.connectorClient = dialogScope.Resolve<IConnectorClient>();

                    if (activity.Type == ActivityTypes.Message)
                    {
                        if (activity.Value != null)
                        {
                            // Process messageBack events using the dialog framework
                            var dialog = dialogScope.Resolve<RootDialog>();
                            await Conversation.SendAsync(activity, () => dialog);
                        }
                        else
                        {
                            // Send welcome card if user send any message to bot
                            var reply = activity.CreateReply();
                            reply.Attachments.Add(CelebrationCard.GetWelcomeCardInResponseToUserMessage().ToAttachment());
                            await this.connectorClient.Conversations.SendToConversationWithRetriesAsync(reply);
                        }
                    }
                    else if (activity.Type == ActivityTypes.ConversationUpdate)
                    {
                        this.logProvider.LogInfo("Processing conversationUpdate activity");
                        switch (activity.Conversation.ConversationType)
                        {
                            case "personal":
                                await this.HandlePersonalConversationUpdateAsync(activity);
                                break;
                            case "channel":
                                await this.HandleTeamConversationUpdateAsync(activity);
                                break;
                            default:
                                this.logProvider.LogWarning($"Received unexpected conversationUpdate activity with conversationType {activity.Conversation.ConversationType}");
                                break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                this.logProvider.LogError($"Failed to process activity {activity.Type}", ex);
                throw;
            }

            return this.Request.CreateResponse(HttpStatusCode.OK);
        }

        // Handle conversation update event (in team scope)
        private async Task HandleTeamConversationUpdateAsync(Activity activity)
        {
            var channelData = activity.GetChannelData<TeamsChannelData>();

            if (activity.MembersAdded?.Count > 0)
            {
                await this.HandleTeamMembersAddedAsync(activity, channelData);
            }
            else if (activity.MembersRemoved?.Count > 0)
            {
                await this.HandleTeamMembersRemovedAsync(activity, channelData);
            }
            else
            {
                var teamEvent = activity.GetConversationUpdateData();
                switch (teamEvent.EventType)
                {
                    case TeamEventType.TeamRenamed:
                        await this.HandleTeamRenamedAsync(activity, teamEvent);
                        break;
                }
            }
        }

        // Handle team members added event (in team scope)
        private async Task HandleTeamMembersAddedAsync(Activity activity, TeamsChannelData channelData)
        {
            string teamId = channelData.Team.Id;
            this.logProvider.LogInfo($"Handling team members added event in team {teamId}");

            // Determine if the bot was installed to the team
            bool isBotAdded = activity.MembersAdded.Any(member => member.Id == activity.Recipient.Id);
            if (isBotAdded)
            {
                this.logProvider.LogInfo($"Bot was installed to team {teamId}");
                var properties = new Dictionary<string, string>
                {
                    { "Scope", activity.Conversation?.ConversationType },
                    { "TeamId", teamId },
                    { "InstallerId", activity.From.Id },
                };
                this.logProvider.LogEvent("AppInstalled", properties);
            }

            var membersAdded = activity.MembersAdded;

            // Ensure that we have an installation record for this team
            var isBackfill = false;
            var teamInfo = await this.userManagementHelper.GetTeamsDetailsByTeamIdAsync(teamId);
            if ((teamInfo == null) && !isBotAdded)
            {
                this.logProvider.LogInfo($"Detected a missed installation to team {teamId}, will attempt to backfill");

                // We must have missed an event from this team-- attempt to backfill
                isBotAdded = true;
                isBackfill = true;
            }

            if (isBotAdded)
            {
                // Try to determine the name of the person that installed the app, which is usually the sender of the message (From.Id)
                // Note that in some cases we cannot resolve it to a team member, because the app was installed to the team programmatically via Graph
                string installerName = null;
                if (!isBackfill)
                {
                    installerName = activity.From?.Name;
                    if (installerName == null)
                    {
                        installerName = await this.GetUserNameAsync(activity.From, teamId);
                    }
                }

                // Get team details
                this.logProvider.LogInfo("Getting team details");
                var teamDetails = await this.connectorClient.GetTeamsConnectorClient().Teams.FetchTeamDetailsAsync(teamId);

                // Add team installation record
                this.logProvider.LogInfo("Recording team installation");
                teamInfo = new Team
                {
                    Id = teamId,
                    Name = teamDetails.Name,
                    ServiceUrl = activity.ServiceUrl,
                    TenantId = channelData.Tenant.Id,
                    InstallerName = installerName,
                };
                await this.userManagementHelper.SaveTeamDetailsAsync(teamInfo);

                // Send welcome message to the General channel
                this.logProvider.LogInfo("Sending welcome message to general channel");
                Activity reply = activity.CreateReply();
                reply.Attachments.Add(CelebrationCard.GetWelcomeMessageForGeneralChannelAndTeamMembers(installerName, teamDetails.Name).ToAttachment());
                await this.connectorClient.Conversations.SendToConversationWithRetriesAsync(reply);

                // Get all team members to welcome them
                this.logProvider.LogInfo("Getting all team members to send them a welcome message");
                membersAdded = await this.connectorClient.Conversations.GetConversationMembersAsync(teamId);
            }
            else
            {
                this.logProvider.LogInfo($"Members added to team {teamId} ({membersAdded?.Count} new members)");
            }

            // Process new team members
            await this.ProcessNewTeamMembersAsync(membersAdded.AsTeamsChannelAccounts(), teamInfo);
        }

        // Handle team members removed event (in team scope)
        private async Task HandleTeamMembersRemovedAsync(Activity activity, TeamsChannelData channelData)
        {
            var teamId = channelData.Team.Id;
            this.logProvider.LogInfo($"Handling team members removed event in team {teamId}");

            var isBotRemoved = activity.MembersRemoved.Any(member => member.Id == activity.Recipient.Id);
            if (isBotRemoved)
            {
                this.logProvider.LogInfo($"Bot removed from team {teamId}");

                // Delete team details and membership records
                await this.userManagementHelper.DeleteTeamDetailsAsync(teamId);
                await this.userManagementHelper.DeleteUserTeamMembershipByTeamIdAsync(teamId);
            }
            else
            {
                foreach (var member in activity.MembersRemoved)
                {
                    this.logProvider.LogInfo($"User {member.Id} removed from team {teamId}.");

                    // Delete membership records for the users who were removed
                    await this.userManagementHelper.DeleteUserTeamMembershipAsync(member.Id, teamId);
                }
            }
        }

        // Handle team rename event (in team scope)
        private async Task HandleTeamRenamedAsync(Activity activity, TeamEventBase teamEvent)
        {
            this.logProvider.LogInfo($"Team {teamEvent.Team.Id} was renamed");

            var teamInfo = await this.userManagementHelper.GetTeamsDetailsByTeamIdAsync(teamEvent.Team.Id);
            if (teamInfo == null)
            {
                teamInfo = new Team
                {
                    Id = teamEvent.Team.Id,
                    ServiceUrl = activity.ServiceUrl,
                    TenantId = teamEvent.Tenant.Id,
                };
            }

            teamInfo.Name = teamEvent.Team.Name;
            await this.userManagementHelper.SaveTeamDetailsAsync(teamInfo);
        }

        // Handle conversation update event (in personal scope)
        private async Task HandlePersonalConversationUpdateAsync(Activity activity)
        {
            this.logProvider.LogInfo($"Handling personal conversationUpdate in thread {activity.Conversation.Id}");

            // In personal scope we only handle events with MembersAdded
            var isBotAdded = activity.MembersAdded?.Any(member => member.Id == activity.Recipient.Id);
            if (isBotAdded == true)
            {
                var user = activity.From;
                this.logProvider.LogInfo($"Conversation was created with user {user.Id}");

                var objectId = user.Properties["aadObjectId"].ToString();
                var userInfo = await this.userManagementHelper.GetUserByAadObjectIdAsync(objectId);
                if (userInfo?.InstallationMethod != BotScope.Team)
                {
                    // Only send a welcome message to the users who were not previously seen in team scope
                    var reply = activity.CreateReply();
                    reply.Attachments.Add(CelebrationCard.GetWelcomeCardForInstaller().ToAttachment());
                    await this.connectorClient.Conversations.SendToConversationWithRetriesAsync(reply);
                }
                else
                {
                    this.logProvider.LogInfo($"Welcome message for {user.Id} was already sent in team scope");
                }
            }
        }

        // Process the new members added to the team
        private async Task ProcessNewTeamMembersAsync(IEnumerable<TeamsChannelAccount> teamMembers, Team teamInfo)
        {
            this.logProvider.LogInfo($"Processing new members in team {teamInfo.Id}");

            var welcomeCard = CelebrationCard.GetWelcomeMessageForGeneralChannelAndTeamMembers(teamInfo.InstallerName, teamInfo.Name).ToAttachment();
            await Task.WhenAll(teamMembers.Select(member => this.ProcessNewTeamMemberAsync(member, teamInfo, welcomeCard)));
        }

        // Process the new member added to the team
        // This means recording the team membership, user information, and sending the user a welcome card
        private async Task ProcessNewTeamMemberAsync(TeamsChannelAccount member, Team teamInfo, Attachment welcomeCard)
        {
            this.logProvider.LogInfo($"Processing member {member.Id} in team {teamInfo.Id}");

            try
            {
                var attachments = new List<Attachment> { welcomeCard };
                var aadObjectId = (member.ObjectId ?? member.Properties["aadObjectId"]).ToString();

                // Record the user's membership in the team
                this.logProvider.LogInfo($"Recording team membership");
                var userTeamMembership = new UserTeamMembership
                {
                    TeamId = teamInfo.Id,
                    UserTeamsId = member.Id,
                };
                await this.userManagementHelper.AddUserTeamMembershipAsync(userTeamMembership);

                // See if the user has events to share
                var events = await this.eventDataProvider.GetEventsByOwnerObjectIdAsync(aadObjectId);
                if (events.Count > 0)
                {
                    this.logProvider.LogInfo($"User has {events.Count} existing events, will send invitation to share");
                    attachments.Add(CelebrationCard.GetShareEventAttachment(teamInfo.Id, teamInfo.Name, aadObjectId));
                }

                // Get the user record
                var userInfo = await this.userManagementHelper.GetUserByAadObjectIdAsync(aadObjectId);

                // Create conversation if needed
                var conversationId = userInfo?.ConversationId;
                if (conversationId == null)
                {
                    conversationId = await this.connectorClient.Conversations.CreateOrGetDirectConversationAsync(teamInfo.TenantId, member.Id);
                }

                // Send the personal welcome message
                this.logProvider.LogInfo($"Sending personal welcome message");
                await this.connectorClient.Conversations.SendCardListAsync(conversationId, attachments);

                this.logProvider.LogInfo("Saving member details");
                await this.StoreUserDetailsIfNeededAsync(member, teamInfo, conversationId);
            }
            catch (Exception ex)
            {
                this.logProvider.LogError($"Failed to process new member {member.Id} in {teamInfo.Id}", ex);
                throw;
            }
        }

        // Store user details in the database if we don't have a record yet
        private async Task StoreUserDetailsIfNeededAsync(TeamsChannelAccount member, Team teamInfo, string conversationId)
        {
            var objectId = member.ObjectId ?? member.Properties["aadObjectId"]?.ToString();

            // Add user information if we don't have a record for this user
            if (await this.userManagementHelper.GetUserByAadObjectIdAsync(objectId) == null)
            {
                var user = new User
                {
                    AadObjectId = objectId.ToString(),
                    TeamsId = member.Id,
                    InstallationMethod = BotScope.Team,
                    ServiceUrl = teamInfo.ServiceUrl,
                    DisplayName = await this.GetUserNameAsync(member, teamInfo.Id),
                    TenantId = teamInfo.TenantId,
                    ConversationId = conversationId,
                };
                await this.userManagementHelper.SaveUserAsync(user);
            }
        }

        // Get the display name of the given user, looking it up in the given team if needed
        private async Task<string> GetUserNameAsync(ChannelAccount member, string teamId)
        {
            if (!string.IsNullOrWhiteSpace(member.Name))
            {
                return member.Name;
            }

            // Get the team member list and cache for lookups of other members
            if (this.teamMembers == null)
            {
                this.teamMembers = await this.connectorClient.Conversations.GetConversationMembersAsync(teamId);
            }

            // Find the member with the matching id
            var userInfo = this.teamMembers?.Where(x => x.Id == member.Id)?.FirstOrDefault();
            return userInfo?.Name;
        }

        // Log information about the received user activity
        private void LogUserActivity(Activity activity)
        {
            // Log the user activity
            var channelData = activity.GetChannelData<TeamsChannelData>();
            var fromTeamsAccount = activity.From.AsTeamsChannelAccount();
            var fromObjectId = fromTeamsAccount.ObjectId ?? activity.From.Properties["aadObjectId"]?.ToString();
            var clientInfoEntity = activity.Entities.Where(e => e.Type == "clientInfo").FirstOrDefault();

            var properties = new Dictionary<TelemetryProperty, string>
            {
                { TelemetryProperty.ActivityType, activity.Type },
                { TelemetryProperty.ActivityId, activity.Id },
                { TelemetryProperty.UserId, activity.From.Id },
                { TelemetryProperty.UserAadObjectId, fromObjectId },
                { TelemetryProperty.ConversationId, activity.Conversation.Id },
                { TelemetryProperty.ConversationType, activity.Conversation.ConversationType ?? "personal" },
                { TelemetryProperty.Locale, clientInfoEntity?.Properties["locale"]?.ToString() },
                { TelemetryProperty.Platform, clientInfoEntity?.Properties["platform"]?.ToString() },
            };

            if (!string.IsNullOrEmpty(channelData?.EventType))
            {
                properties[TelemetryProperty.TeamsEventType] = channelData.EventType;
            }

            this.logProvider.LogEvent(TelemetryEvent.UserActivity, properties);
        }
    }
}