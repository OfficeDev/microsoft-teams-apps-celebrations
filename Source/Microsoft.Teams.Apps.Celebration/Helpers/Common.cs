// <copyright file="Common.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.Celebration.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Microsoft.Bot.Connector;
    using Microsoft.Teams.Apps.Celebration.Models;
    using Newtonsoft.Json;

    /// <summary>
    /// Contains common static methods used in project
    /// </summary>
    public static class Common
    {
        /// <summary>
        /// Entity ID of the events tab
        /// </summary>
        public const string EventsTabEntityId = "EventsTab";

        /// <summary>
        /// Count no. of files in a directory
        /// </summary>
        /// <param name="directoryPath">directory path</param>
        /// <returns>file count.</returns>
        public static int GetCountOfFilesInDirectory(string directoryPath)
        {
            return Directory.Exists(directoryPath) ? Directory.GetFiles(directoryPath).Length : 0;
        }

        /// <summary>
        /// Returns a list of TimeZoneDisplayInfo
        /// </summary>
        /// <returns>List of TimeZoneDisplayInfo</returns>
        public static IList<TimeZoneDisplayInfo> GetTimeZoneList()
        {
            var timeZonelist = new List<TimeZoneDisplayInfo>();
            foreach (TimeZoneInfo info in TimeZoneInfo.GetSystemTimeZones())
            {
                timeZonelist.Add(new TimeZoneDisplayInfo { TimeZoneDisplayName = info.DisplayName, TimeZoneId = info.Id });
            }

            return timeZonelist;
        }

        /// <summary>
        /// Get absolute URL of image from relative path
        /// </summary>
        /// <param name="imageRelativePath">Image relative path</param>
        /// <returns>image URL</returns>
        public static string GetImageUrlFromPath(string imageRelativePath)
        {
            return ApplicationSettings.BaseUrl + imageRelativePath;
        }

        /// <summary>
        /// Get absolute URL of image from file name
        /// </summary>
        /// <param name="imageName">Image name</param>
        /// <returns>image URL</returns>
        public static string GetImageUrlFromName(string imageName)
        {
            return ApplicationSettings.BaseUrl + "/Content/images/" + imageName;
        }

        /// <summary>
        /// Construct deeplink URL to the Events tab
        /// </summary>
        /// <param name="subEntityId">optional sub entity id</param>
        /// <returns>Deep link URL</returns>
        public static Uri GetDeeplinkToEventsTab(string subEntityId = null)
        {
            string context;
            if (!string.IsNullOrEmpty(subEntityId))
            {
                var contextObject = new
                {
                    subEntityId,
                };
                context = "context=" + Uri.EscapeDataString(JsonConvert.SerializeObject(contextObject));
            }
            else
            {
                context = string.Empty;
            }

            return new Uri(string.Format(
                "https://teams.microsoft.com/l/entity/{0}/{1}?{2}",
                ApplicationSettings.ManifestAppId,
                EventsTabEntityId,
                context));
        }

        /// <summary>
        /// Get the next occurrence of the event that is after the given date.
        /// </summary>
        /// <param name="recurrenceReferenceDate">The month and day on which the event occurs every year. The time component is set to the time that it is supposed to occur.</param>
        /// <param name="afterDateTime">The occurrence calculated must be after this date and time</param>
        /// <returns>The date of the next occurrence of the event that happens after <paramref name="afterDateTime"/>.</returns>
        public static DateTime GetNextOccurrenceAfterDateTime(DateTime recurrenceReferenceDate, DateTime afterDateTime)
        {
            int differenceInYears = afterDateTime.Date.Year - recurrenceReferenceDate.Year;

            // If the occurrence is before the given "afterDate", add 1 year
            if (recurrenceReferenceDate.AddYears(differenceInYears) <= afterDateTime)
            {
                differenceInYears += 1;
            }

            return recurrenceReferenceDate.AddYears(differenceInYears);
        }
    }
}
