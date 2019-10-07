// <copyright file="TabsController.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.Celebration
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Threading.Tasks;
    using System.Web;
    using System.Web.Mvc;
    using Microsoft.Teams.Apps.Celebration.Helpers;
    using Microsoft.Teams.Apps.Celebration.Models;
    using Microsoft.Teams.Apps.Celebration.Resources;
    using Microsoft.Teams.Apps.Common.Configuration;
    using Microsoft.Teams.Apps.Common.Extensions;
    using Microsoft.Teams.Apps.Common.Logging;
    using TimeZoneConverter;

    /// <summary>
    /// Represents the tab action methods
    /// </summary>
    [Authorize]
    public class TabsController : Controller
    {
        private readonly IEventDataProvider eventDataProvider;
        private readonly IUserManagementHelper userManagementHelper;
        private readonly IConfigProvider configProvider;
        private readonly ILogProvider logProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="TabsController"/> class.
        /// </summary>
        /// <param name="eventHelper">Event data provider instance</param>
        /// <param name="userManagementHelper">UserManagementHelper instance</param>
        /// <param name="configProvider">Configuration provider instance</param>
        /// <param name="logProvider">ILogProvider</param>
        public TabsController(IEventDataProvider eventHelper, IUserManagementHelper userManagementHelper, IConfigProvider configProvider, ILogProvider logProvider)
        {
            this.eventDataProvider = eventHelper;
            this.userManagementHelper = userManagementHelper;
            this.configProvider = configProvider;
            this.logProvider = logProvider;
        }

        /// <summary>
        /// Returns view for events tab
        /// </summary>
        /// <returns>Events View</returns>
        [Route("Events")]
        [HttpGet]
        public ActionResult Events()
        {
            var viewModel = new EventsTabViewModel
            {
                MaxUserEventsCount = Convert.ToInt32(this.configProvider.GetSetting(ApplicationConfig.MaxUserEventsCount)),
            };
            return this.View(viewModel);
        }

        /// <summary>
        /// Returns view for events tab
        /// </summary>
        /// <returns>Events View</returns>
        [Route("EventsData")]
        [HttpPost]
        public async Task<ActionResult> EventsData()
        {
            var context = this.Request.GetOwinContext();
            var userObjectId = context.Authentication.User.GetUserObjectId();

            var viewModel = new EventsTabViewModel
            {
                Events = await this.GetEventsByOwnerObjectIdAsync(userObjectId),
                MaxUserEventsCount = Convert.ToInt32(this.configProvider.GetSetting(ApplicationConfig.MaxUserEventsCount)),
            };
            return this.PartialView(viewModel);
        }

        /// <summary>
        /// Get and return TotalEvent count of user
        /// </summary>
        /// <returns>Event count</returns>
        [HttpGet]
        public async Task<ActionResult> GetTotalEventCountOfUser()
        {
            var context = this.Request.GetOwinContext();
            var userObjectId = context.Authentication.User.GetUserObjectId();

            var events = await this.GetEventsByOwnerObjectIdAsync(userObjectId);

            return this.Content(events.Count.ToString());
        }

        /// <summary>
        /// Returns empty view for ManageEvent task module
        /// </summary>
        /// <param name="eventId">EventId</param>
        /// <param name="clientTimeZone">Client's machine timeZone id</param>
        /// <returns>Manage event task module view</returns>
        [Route("ManageEvent")]
        [HttpGet]
        public ActionResult ManageEvent(string eventId, string clientTimeZone)
        {
            var manageEventModel = new ManageEventViewModel()
            {
                EventId = eventId,
                ClientTimeZone = clientTimeZone,
            };

            return this.View(manageEventModel);
        }

        /// <summary>
        /// Returns the tour view.
        /// </summary>
        /// <returns>Tour View</returns>
        [Route("Tour")]
        [HttpGet]
        public ActionResult Tour()
        {
            return this.View();
        }

        /// <summary>
        /// Returns view for ManageEvent task module
        /// </summary>
        /// <param name="eventId">EventId</param>
        /// <param name="clientTimeZone">Client's machine timeZone id</param>
        /// <returns>A <see cref="Task{TResult}"/> Representing the result of the asynchronous operation</returns>
        [Route("ManageEventData")]
        [HttpPost]
        public async Task<ActionResult> ManageEventData(string eventId, string clientTimeZone)
        {
            var context = this.Request.GetOwinContext();
            var userObjectId = context.Authentication.User.GetUserObjectId();

            TZConvert.TryIanaToWindows(clientTimeZone, out string windowsTimeZoneId);
            var manageEventModel = new ManageEventViewModel()
            {
                EventId = eventId,
                ClientTimeZone = clientTimeZone,
                TeamDetails = await this.GetTeamDetailsWhereBothBotAndUsersAreInAsync(userObjectId),
                TimeZoneList = Common.GetTimeZoneList(),
                SelectedTimeZoneId = windowsTimeZoneId,
                EventTypesInfo = new List<Tuple<EventTypes, string>>
                {
                    Tuple.Create(EventTypes.Birthday, Strings.BirthdayEventTypeName),
                    Tuple.Create(EventTypes.Anniversary, Strings.AnniversaryEventTypeName),
                    Tuple.Create(EventTypes.Other, Strings.OtherEventTypeName),
                },
            };

            if (!string.IsNullOrWhiteSpace(eventId))
            {
                manageEventModel.CelebrationEvent = await this.eventDataProvider.GetEventByIdAsync(eventId, userObjectId);
                manageEventModel.SelectedTimeZoneId = manageEventModel.CelebrationEvent?.TimeZoneId ?? manageEventModel.SelectedTimeZoneId;
            }

            return this.PartialView(manageEventModel);
        }

        /// <summary>
        /// Save celebration event
        /// </summary>
        /// <param name="celebrationEvent">CelebrationEvent object</param>
        /// <returns>Events View.</returns>
        [Route("SaveEvent")]
        [HttpPost]
        public async Task<ActionResult> SaveEvent(CelebrationEvent celebrationEvent)
        {
            var context = this.Request.GetOwinContext();
            var userObjectId = context.Authentication.User.GetUserObjectId();

            if (this.ModelState.IsValid)
            {
                celebrationEvent.OwnerAadObjectId = userObjectId;
                await this.eventDataProvider.AddEventAsync(celebrationEvent);
            }

            var viewModel = new EventsTabViewModel
            {
                Events = await this.GetEventsByOwnerObjectIdAsync(userObjectId),
                MaxUserEventsCount = Convert.ToInt32(this.configProvider.GetSetting(ApplicationConfig.MaxUserEventsCount)),
            };
            return this.PartialView("EventsData", viewModel);
        }

        /// <summary>
        /// Update celebration event
        /// </summary>
        /// <param name="celebrationEvent">CelebrationEvent object</param>
        /// <returns>Events View.</returns>
        [Route("UpdateEvent")]
        [HttpPost]
        public async Task<ActionResult> UpdateEvent(CelebrationEvent celebrationEvent)
        {
            var context = this.Request.GetOwinContext();
            var userObjectId = context.Authentication.User.GetUserObjectId();

            if (this.ModelState.IsValid)
            {
                // Ensure that the provided event is actually owned by the current user
                var fetchedEvent = await this.eventDataProvider.GetEventByIdAsync(celebrationEvent.Id, userObjectId);
                if (fetchedEvent != null)
                {
                    celebrationEvent.OwnerAadObjectId = userObjectId;
                    await this.eventDataProvider.UpdateEventAsync(celebrationEvent);

                    // If event date or timezone is changed then delete record from Occurrences and EventMessages collections
                    if (fetchedEvent.Date != celebrationEvent.Date || fetchedEvent.TimeZoneId != celebrationEvent.TimeZoneId)
                    {
                        await this.eventDataProvider.DeleteEventOccurrencesByEventIdAsync(celebrationEvent.Id);
                        await this.eventDataProvider.DeleteEventMessagesByEventIdAsync(celebrationEvent.Id);

                        // The next run of the preview function will handle scheduling the preview
                    }
                }
                else
                {
                    this.logProvider.LogInfo($"Could not find event {celebrationEvent.Id} belonging to {userObjectId}");
                }
            }

            var viewModel = new EventsTabViewModel
            {
                Events = await this.GetEventsByOwnerObjectIdAsync(userObjectId),
                MaxUserEventsCount = Convert.ToInt32(this.configProvider.GetSetting(ApplicationConfig.MaxUserEventsCount)),
            };
            return this.PartialView("EventsData", viewModel);
        }

        /// <summary>
        /// Delete event
        /// </summary>
        /// <param name="eventId">Event Id</param>
        /// <returns>Task.</returns>
        [Route("DeleteEvent")]
        [HttpPost]
        public async Task<ActionResult> DeleteEvent(string eventId)
        {
            var context = this.Request.GetOwinContext();
            var userObjectId = context.Authentication.User.GetUserObjectId();

            var fetchedEvent = await this.eventDataProvider.GetEventByIdAsync(eventId, userObjectId);
            if (fetchedEvent != null)
            {
                await this.eventDataProvider.DeleteEventAsync(eventId, userObjectId);
                await this.eventDataProvider.DeleteEventOccurrencesByEventIdAsync(eventId);
                await this.eventDataProvider.DeleteEventMessagesByEventIdAsync(eventId);
            }
            else
            {
                this.logProvider.LogInfo($"Could not find event {eventId} belonging to {userObjectId}");
            }

            var viewModel = new EventsTabViewModel
            {
                Events = await this.GetEventsByOwnerObjectIdAsync(userObjectId),
                MaxUserEventsCount = Convert.ToInt32(this.configProvider.GetSetting(ApplicationConfig.MaxUserEventsCount)),
            };
            return this.PartialView("EventsData", viewModel);
        }

        /// <summary>
        /// Check if the event with the given ID exists for the user
        /// </summary>
        /// <param name="eventId">Event id</param>
        /// <returns>A <see cref="Task"/>Representing the asynchronous operation</returns>
        [HttpGet]
        public async Task<ActionResult> CheckIfEventExists(string eventId)
        {
            var context = this.Request.GetOwinContext();
            var userObjectId = context.Authentication.User.GetUserObjectId();

            HttpStatusCode documentStatus = HttpStatusCode.NotFound;
            var document = await this.eventDataProvider.GetEventByIdAsync(eventId, userObjectId);
            if (document != null)
            {
                documentStatus = HttpStatusCode.OK;
            }

            return this.Content(documentStatus.ToString());
        }

        /// <summary>
        /// Returns Team details where bot and users both are in
        /// </summary>
        /// <param name="userObjectId">AadObjectId of user</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation</returns>
        private async Task<IList<Team>> GetTeamDetailsWhereBothBotAndUsersAreInAsync(string userObjectId)
        {
            IList<Team> teamDetails;
            try
            {
                var user = await this.userManagementHelper.GetUserByAadObjectIdAsync(userObjectId);
                var userTeamMembership = await this.userManagementHelper.GetUserTeamMembershipByTeamsIdAsync(user.TeamsId);
                teamDetails = await this.userManagementHelper.GetTeamsDetailsByTeamIdsAsync(userTeamMembership.Select(x => x.TeamId).ToList());
                teamDetails = teamDetails.OrderBy(x => x.Name).ToList();
            }
            catch (Exception ex)
            {
                this.logProvider.LogError("Failed to get Team details from method GetTeamDetailsWhereBothBotAndUsersAreIn. error:" + ex.ToString());
                teamDetails = new List<Team>();
            }

            return teamDetails;
        }

        // Get events owned by the given user
        private async Task<IList<CelebrationEvent>> GetEventsByOwnerObjectIdAsync(string userObjectId)
        {
            return await this.eventDataProvider.GetEventsByOwnerObjectIdAsync(userObjectId);
        }
    }
}