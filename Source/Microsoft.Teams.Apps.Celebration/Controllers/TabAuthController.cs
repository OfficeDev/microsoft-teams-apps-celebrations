// <copyright file="TabAuthController.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.Celebration.Controllers
{
    using System.Web.Mvc;
    using Microsoft.Teams.Apps.Common;
    using Microsoft.Teams.Apps.Common.Configuration;

    /// <summary>
    /// Tab authentication controller
    /// </summary>
    public class TabAuthController : Controller
    {
        /// <summary>
        /// Path to the tab login page
        /// </summary>
        public const string LoginPath = "/TabAuth/Login";

        private const string StartPath = "/TabAuth/Start";
        private const string RedirectUriPath = "/TabAuth/Callback";

        private readonly string tenantId;
        private readonly string clientId;

        /// <summary>
        /// Initializes a new instance of the <see cref="TabAuthController"/> class.
        /// </summary>
        /// <param name="configProvider">The configuration provider to use</param>
        public TabAuthController(IConfigProvider configProvider)
        {
            this.tenantId = configProvider.GetSetting(CommonConfig.ActiveDirectoryTenant);
            this.clientId = configProvider.GetSetting(CommonConfig.ActiveDirectoryClientId);
        }

        /// <summary>
        /// Tab authentication login page
        /// </summary>
        /// <param name="returnUrl">Destination path</param>
        /// <returns>View</returns>
        // GET: Login
        [AllowAnonymous]
        public ActionResult Login(string returnUrl)
        {
            this.ViewBag.TenantId = this.tenantId;
            this.ViewBag.ClientId = this.clientId;
            this.ViewBag.StartPath = StartPath;
            this.ViewBag.RedirectUriPath = RedirectUriPath;
            this.ViewBag.DestinationPath = returnUrl;
            this.ViewBag.TokenLoginEndpointPath = AuthController.TokenLoginEndpointPath;
            return this.View();
        }

        /// <summary>
        /// Tab authentication flow start page
        /// </summary>
        /// <returns>View</returns>
        // GET: Start
        [AllowAnonymous]
        public ActionResult Start()
        {
            this.ViewBag.TenantId = this.tenantId;
            this.ViewBag.ClientId = this.clientId;
            this.ViewBag.RedirectUriPath = RedirectUriPath;
            return this.View();
        }

        /// <summary>
        /// Tab authentication flow callback page
        /// </summary>
        /// <returns>View</returns>
        // GET: Callback
        [AllowAnonymous]
        public ActionResult Callback()
        {
            this.ViewBag.TenantId = this.tenantId;
            this.ViewBag.ClientId = this.clientId;
            return this.View();
        }
    }
}