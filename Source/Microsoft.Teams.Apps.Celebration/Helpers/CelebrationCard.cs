// <copyright file="CelebrationCard.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.Celebration.Helpers
{
    using System;
    using System.Collections.Generic;
    using AdaptiveCards;
    using Microsoft.Bot.Connector;
    using Microsoft.Teams.Apps.Celebration.Helpers;
    using Microsoft.Teams.Apps.Celebration.Models;
    using Microsoft.Teams.Apps.Celebration.Resources;
    using Newtonsoft.Json;

    /// <summary>
    /// utility class for celebration bot cards
    /// </summary>
    public static class CelebrationCard
    {
        /// <summary>
        /// Create and return preview card
        /// </summary>
        /// <param name="celebrationEvent">The event</param>
        /// <param name="occurrenceId">The event occurrence</param>
        /// <param name="ownerDisplayName">The event owner</param>
        /// <param name="isSkipAllowed">true/false</param>
        /// <returns>HeroCard</returns>
        public static HeroCard GetPreviewCard(CelebrationEvent celebrationEvent, string occurrenceId, string ownerDisplayName, bool isSkipAllowed = true)
        {
            var cardActions = new List<CardAction>()
            {
                new CardAction()
                {
                    Title = Strings.EditButtonTitle,
                    Type = ActionTypes.OpenUrl,
                    Value = Common.GetDeeplinkToEventsTab(celebrationEvent.Id),
                },
            };

            if (isSkipAllowed)
            {
                cardActions.Insert(0, new CardAction()
                {
                    Title = Strings.SkipButtonTitle,
                    Type = ActionTypes.MessageBack,
                    Value = new PreviewCardPayload
                    {
                        Action = "SkipEvent",
                        EventId = celebrationEvent.Id,
                        OccurrenceId = occurrenceId,
                        OwnerAadObjectId = celebrationEvent.OwnerAadObjectId,
                        OwnerName = ownerDisplayName,
                    },
                });
            }

            var previewCard = new HeroCard()
            {
                Title = string.Format(Strings.EventPreviewCardHeader, ownerDisplayName, celebrationEvent.Title),
                Text = celebrationEvent.Message,
                Buttons = cardActions,
                Images = new List<CardImage>() { new CardImage(url: Common.GetImageUrlFromPath(celebrationEvent.ImageUrl)) },
            };

            return previewCard;
        }

        /// <summary>
        /// Create and return Celebration Event card
        /// </summary>
        /// <param name="celebrationEvent">Celebration event</param>
        /// <param name="ownerDisplayName">Owner display name</param>
        /// <returns>HeroCard</returns>
        public static HeroCard GetEventCard(CelebrationEvent celebrationEvent, string ownerDisplayName)
        {
            return new HeroCard()
            {
                Title = string.Format(Strings.EventCardTitle, ownerDisplayName, celebrationEvent.Title),
                Text = celebrationEvent.Message,
                Images = new List<CardImage>() { new CardImage(url: Common.GetImageUrlFromPath(celebrationEvent.ImageUrl)) },
            };
        }

        /// <summary>
        /// Create and return welcome card for installer of bot
        /// </summary>
        /// <returns>AdaptiveCard</returns>
        public static AdaptiveCard GetWelcomeCardForInstaller()
        {
            AdaptiveCard welcomeCard = new AdaptiveCard("1.0")
            {
                Body = new List<AdaptiveElement>()
                {
                    new AdaptiveContainer()
                    {
                        Items = new List<AdaptiveElement>()
                        {
                            new AdaptiveColumnSet()
                            {
                                Columns = new List<AdaptiveColumn>()
                                {
                                    new AdaptiveColumn()
                                    {
                                        Width = "60",
                                        Items = new List<AdaptiveElement>()
                                        {
                                            new AdaptiveImage()
                                            {
                                                Url = new Uri(Common.GetImageUrlFromName("celebration_bot_full-color.png")),
                                                Size = AdaptiveImageSize.Medium,
                                                Style = AdaptiveImageStyle.Default,
                                            },
                                        },
                                    },
                                    new AdaptiveColumn()
                                    {
                                        Width = "400",
                                        Items = new List<AdaptiveElement>()
                                        {
                                            new AdaptiveTextBlock
                                            {
                                                Text = Strings.WelcomeMessagePart1,
                                                Size = AdaptiveTextSize.Default,
                                                Wrap = true,
                                                Weight = AdaptiveTextWeight.Default,
                                            },
                                            new AdaptiveTextBlock()
                                            {
                                              Text = Strings.WelcomeMessagePart2,
                                              Size = AdaptiveTextSize.Default,
                                              Wrap = true,
                                            },
                                            new AdaptiveTextBlock()
                                            {
                                              Text = Strings.WelcomeMessagePart3,
                                              Size = AdaptiveTextSize.Default,
                                              Wrap = true,
                                            },
                                        },
                                    },
                                },
                            },
                        },
                    },
                },
                Actions = new List<AdaptiveAction>()
                {
                    new AdaptiveOpenUrlAction()
                    {
                        Title = Strings.GetStartedButtonText,
                        Url = Common.GetDeeplinkToEventsTab(),
                    },
                    new AdaptiveOpenUrlAction()
                    {
                        Title = Strings.TakeATourButtonText,
                        Url = new Uri(GetTourUrl()),
                    },
                },
            };

            return welcomeCard;
        }

        /// <summary>
        /// Create and return welcome card as a reply
        /// </summary>
        /// <returns>AdaptiveCard</returns>
        public static AdaptiveCard GetWelcomeCardInResponseToUserMessage()
        {
            var welcomeCard = new AdaptiveCard("1.0")
            {
                Body = new List<AdaptiveElement>()
                {
                    new AdaptiveContainer()
                    {
                        Items = new List<AdaptiveElement>()
                        {
                            new AdaptiveColumnSet()
                            {
                                Columns = new List<AdaptiveColumn>()
                                {
                                    new AdaptiveColumn()
                                    {
                                        Width = "60",
                                        Items = new List<AdaptiveElement>()
                                        {
                                            new AdaptiveImage()
                                            {
                                                Url = new Uri(Common.GetImageUrlFromName("celebration_bot_full-color.png")),
                                                Size = AdaptiveImageSize.Medium,
                                                Style = AdaptiveImageStyle.Default,
                                            },
                                        },
                                    },
                                    new AdaptiveColumn()
                                    {
                                        Width = "400",
                                        Items = new List<AdaptiveElement>()
                                        {
                                            new AdaptiveTextBlock()
                                            {
                                                Text = Strings.WelcomeMessageForUserTitle,
                                                Size = AdaptiveTextSize.Large,
                                                Weight = AdaptiveTextWeight.Bolder,
                                            },
                                            new AdaptiveTextBlock()
                                            {
                                                Text = Strings.WelcomeMessagePart4,
                                                Size = AdaptiveTextSize.Default,
                                                Wrap = true,
                                                Spacing = AdaptiveSpacing.None,
                                            },
                                        },
                                    },
                                },
                            },
                        },
                    },
                },
                Actions = new List<AdaptiveAction>()
                {
                    new AdaptiveOpenUrlAction()
                    {
                        Title = Strings.GetStartedButtonText,
                        Url = Common.GetDeeplinkToEventsTab(),
                    },
                    new AdaptiveOpenUrlAction()
                    {
                        Title = Strings.TakeATourButtonText,
                        Url = new Uri(GetTourUrl()),
                    },
                },
            };

            return welcomeCard;
        }

        /// <summary>
        /// Create and return welcome card for team members and general channel
        /// </summary>
        /// <param name="botInstallerName">bot installer name</param>
        /// <param name="teamName">TeamName</param>
        /// <returns>AdaptiveCard</returns>
        public static AdaptiveCard GetWelcomeMessageForGeneralChannelAndTeamMembers(string botInstallerName, string teamName)
        {
            var welcomeCard = new AdaptiveCard("1.0")
            {
                Body = new List<AdaptiveElement>()
                {
                    new AdaptiveContainer()
                    {
                        Items = new List<AdaptiveElement>()
                        {
                            new AdaptiveColumnSet()
                            {
                                Columns = new List<AdaptiveColumn>()
                                {
                                    new AdaptiveColumn()
                                    {
                                        Width = "60",
                                        Items = new List<AdaptiveElement>()
                                        {
                                            new AdaptiveImage()
                                            {
                                                Url = new Uri(Common.GetImageUrlFromName("celebration_bot_full-color.png")),
                                                Size = AdaptiveImageSize.Medium,
                                                Style = AdaptiveImageStyle.Default,
                                            },
                                        },
                                    },
                                    new AdaptiveColumn()
                                    {
                                        Width = "400",
                                        Items = new List<AdaptiveElement>()
                                        {
                                            new AdaptiveTextBlock()
                                            {
                                                Text = Strings.WelcomeMessageForTeamTitle,
                                                Size = AdaptiveTextSize.Large,
                                                Weight = AdaptiveTextWeight.Bolder,
                                            },
                                            new AdaptiveTextBlock()
                                            {
                                                Text = string.Format(Strings.WelcomeMessageForTeam, botInstallerName, teamName),
                                                Size = AdaptiveTextSize.Default,
                                                Wrap = true,
                                                Spacing = AdaptiveSpacing.None,
                                            },
                                        },
                                    },
                                },
                            },
                        },
                    },
                },
                Actions = new List<AdaptiveAction>()
                {
                    new AdaptiveOpenUrlAction()
                    {
                        Title = Strings.GetStartedButtonText,
                        Url = Common.GetDeeplinkToEventsTab(),
                    },
                    new AdaptiveOpenUrlAction()
                    {
                        Title = Strings.TakeATourButtonText,
                        Url = new Uri(GetTourUrl()),
                    },
                },
            };

            return welcomeCard;
        }

        /// <summary>
        /// Create and return attachment to share the existing events of user with team
        /// </summary>
        /// <param name="teamId">Team id</param>
        /// <param name="teamName">Team name to share the event with</param>
        /// <param name="userAadObjectId">AadObject Id of user</param>
        /// <returns>Attachment</returns>
        public static Attachment GetShareEventAttachment(string teamId, string teamName, string userAadObjectId)
        {
            return new HeroCard()
            {
                Text = string.Format(Strings.EventShareMessage, teamName),
                Buttons = new List<CardAction>()
                {
                    new CardAction()
                    {
                        Title = Strings.ShareButtonTitle,
                        DisplayText = Strings.ShareButtonTitle,
                        Type = ActionTypes.MessageBack,
                        Text = Strings.ShareButtonTitle,
                        Value = JsonConvert.SerializeObject(new ShareEventPayload
                        {
                            Action = "ShareEvent",
                            TeamId = teamId,
                            TeamName = teamName,
                            UserAadObjectId = userAadObjectId,
                        }),
                    },
                    new CardAction()
                    {
                        Title = Strings.NoThanksButtonTitle,
                        DisplayText = Strings.NoThanksButtonTitle,
                        Type = ActionTypes.MessageBack,
                        Text = Strings.NoThanksButtonTitle,
                        Value = JsonConvert.SerializeObject(new ShareEventPayload
                        {
                            Action = "IgnoreEventShare",
                            TeamId = teamId,
                            TeamName = teamName,
                            UserAadObjectId = userAadObjectId,
                        }),
                    },
                },
            }.ToAttachment();
        }

        /// <summary>
        /// Create and return attachment to share the existing events of user with team
        /// </summary>
        /// <param name="teamName">Team name to share the event with</param>
        /// <returns>Attachment</returns>
        public static Attachment GetShareEventAttachmentWithoutActionButton(string teamName)
        {
            return new HeroCard()
            {
                Text = string.Format(Strings.EventShareMessage, teamName),
            }.ToAttachment();
        }

        /// <summary>
        /// Create a URL for Take a tour action button
        /// </summary>
        /// <returns>Take a tour URL</returns>
        private static string GetTourUrl()
        {
            var appId = ApplicationSettings.ManifestAppId;
            var htmlUrl = $"{ApplicationSettings.BaseUrl}/Tabs/Tour?theme={{theme}}";
            var tourTitle = Strings.TourTaskModuleTitle;
            return $"https://teams.microsoft.com/l/task/{appId}?url={Uri.EscapeDataString(htmlUrl)}&height=533&width=600&title={Uri.EscapeDataString(tourTitle)}";
        }
    }
}