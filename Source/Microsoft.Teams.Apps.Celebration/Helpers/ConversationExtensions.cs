// <copyright file="ConversationExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.Celebration.Helpers
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.Bot.Connector;
    using Microsoft.Bot.Connector.Teams.Models;

    /// <summary>
    /// Useful extension methods on <see cref="IConversations"/>.
    /// </summary>
    public static class ConversationExtensions
    {
        /// <summary>
        /// Send attachments in list format.
        /// </summary>
        /// <param name="conversations">The <see cref="IConversations"/> instance</param>
        /// <param name="conversationId">Thread id where the message will be sent</param>
        /// <param name="attachments">Attachments</param>
        /// <param name="text">Message text</param>
        /// <param name="summary">Message summary</param>
        /// <param name="entities">Entities to attach to the message</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation</returns>
        public static Task SendCardListAsync(this IConversations conversations, string conversationId, List<Attachment> attachments, string text = null, string summary = null, List<Entity> entities = null)
        {
            var activity = new Activity(ActivityTypes.Message)
            {
                Conversation = new ConversationAccount { Id = conversationId },
                AttachmentLayout = AttachmentLayoutTypes.List,
                Text = text,
                Summary = summary,
                Attachments = attachments,
                Entities = entities,
            };
            return conversations.SendToConversationWithRetriesAsync(activity);
        }

        /// <summary>
        /// Send an attachment.
        /// </summary>
        /// <param name="conversations">The <see cref="IConversations"/> instance</param>
        /// <param name="conversationId">Thread id where the message will be sent</param>
        /// <param name="attachment">Attachment to send</param>
        /// <param name="text">Message text</param>
        /// <param name="summary">Message summary</param>
        /// <param name="entities">Entities to attach to the message</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation</returns>
        public static Task<ResourceResponse> SendCardAsync(this IConversations conversations, string conversationId, Attachment attachment, string text = null, string summary = null, List<Entity> entities = null)
        {
            var activity = new Activity(ActivityTypes.Message)
            {
                Conversation = new ConversationAccount { Id = conversationId },
                Text = text,
                Summary = summary,
                Attachments = new List<Attachment> { attachment },
                Entities = entities,
            };
            return conversations.SendToConversationWithRetriesAsync(activity);
        }

        /// <summary>
        /// Send a mesage.
        /// </summary>
        /// <param name="conversations">The <see cref="IConversations"/> instance</param>
        /// <param name="conversationId">Thread id where the message will be sent</param>
        /// <param name="text">Message text</param>
        /// <param name="entities">Entities to attach to the message</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation</returns>
        public static Task<ResourceResponse> SendMessageAsync(this IConversations conversations, string conversationId, string text, List<Entity> entities = null)
        {
            var activity = new Activity(ActivityTypes.Message)
            {
                Conversation = new ConversationAccount { Id = conversationId },
                Text = text,
                Entities = entities,
            };
            return conversations.SendToConversationWithRetriesAsync(activity);
        }

        /// <summary>
        /// Creates a conversation between the user and the bot.
        /// </summary>
        /// <param name="conversations">The <see cref="IConversations"/> instance</param>
        /// <param name="tenantId">Tenant Id</param>
        /// <param name="userTeamsId">teamsId of user</param>
        /// <returns>conversationId.</returns>
        public static async Task<string> CreateOrGetDirectConversationAsync(this IConversations conversations, string tenantId, string userTeamsId)
        {
            var parameters = new ConversationParameters
            {
                Members = new List<ChannelAccount> { new ChannelAccount { Id = userTeamsId } },
                ChannelData = new TeamsChannelData { Tenant = new TenantInfo { Id = tenantId } },
            };
            var result = await conversations.CreateConversationAsync(parameters);
            return result.Id;
        }

        /// <summary>
        /// Starts a reply chain in the given channel.
        /// </summary>
        /// <param name="conversations">The <see cref="IConversations"/> instance</param>
        /// <param name="channelId">Channel to post to</param>
        /// <param name="activity">Activity to use as root post</param>
        /// <returns>Conversation resource</returns>
        public static Task<ConversationResourceResponse> CreateReplyChainAsync(this IConversations conversations, string channelId, Activity activity)
        {
            var parameters = new ConversationParameters
            {
                Activity = activity,
                ChannelData = new TeamsChannelData { Channel = new ChannelInfo { Id = channelId } },
            };
            return conversations.CreateConversationAsync(parameters);
        }
    }
}