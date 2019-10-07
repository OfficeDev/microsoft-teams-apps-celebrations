// <copyright file="RouteConfig.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.Celebration
{
    using System.Web.Mvc;
    using System.Web.Routing;

    /// <summary>
    /// Route configurator
    /// </summary>
    public class RouteConfig
    {
        /// <summary>
        /// Registers routes with the <see cref="RouteCollection"/>
        /// </summary>
        /// <param name="routes">The routes to configure</param>
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { action = "Index", id = UrlParameter.Optional });

           // routes.MapMvcAttributeRoutes();
        }
    }
}
