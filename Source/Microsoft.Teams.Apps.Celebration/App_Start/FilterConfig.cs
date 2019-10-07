// <copyright file="FilterConfig.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.Celebration
{
    using System.Web.Mvc;

    /// <summary>
    /// Filter configurator
    /// </summary>
    public class FilterConfig
    {
        /// <summary>
        /// Configures global filters
        /// </summary>
        /// <param name="filters">The filters to configure</param>
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
        }
    }
}
