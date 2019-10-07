// <copyright file="ApplicationSettings.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.Celebration.Helpers
{
    using System;
    using System.Configuration;

    /// <summary>
    /// Store Application settings
    /// </summary>
    public static class ApplicationSettings
    {
        /// <summary>
        /// Initializes static members of the <see cref="ApplicationSettings"/> class.
        /// </summary>
        static ApplicationSettings()
        {
            BaseUrl = ConfigurationManager.AppSettings[nameof(ApplicationConfig.BaseUrl)];
            ManifestAppId = ConfigurationManager.AppSettings[nameof(ApplicationConfig.ManifestAppId)];
        }

        /// <summary>
        /// Gets or sets base url
        /// </summary>
        public static string BaseUrl { get; set; }

        /// <summary>
        /// Gets manifest id which is Guid
        /// </summary>
        public static string ManifestAppId { get; }
    }
}