// <copyright file="AuthController.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.Celebration.Controllers
{
    using System;
    using System.Linq;
    using System.Net;
    using System.Net.Http.Headers;
    using System.Security.Claims;
    using System.Threading.Tasks;
    using System.Web;
    using System.Web.Mvc;
    using Microsoft.Owin.Security.Cookies;
    using Microsoft.Teams.Apps.Common.Authentication;

    /// <summary>
    /// Authentication controller
    /// </summary>
    public class AuthController : Controller
    {
        /// <summary>
        /// Path to the login endpoint
        /// </summary>
        public const string TokenLoginEndpointPath = "/Auth/TokenLogin";

        private readonly ITokenValidator tokenValidator;

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthController"/> class.
        /// </summary>
        /// <param name="tokenValidator">The token validator</param>
        public AuthController(ITokenValidator tokenValidator)
        {
            this.tokenValidator = tokenValidator;
        }

        /// <summary>
        /// Action to login using AAD ID token
        /// </summary>
        /// <returns>HTTP result</returns>
        // POST: TokenLogin
        [AllowAnonymous]
        [HttpPost]
        public async Task<ActionResult> TokenLogin()
        {
            var headerString = this.Request.Headers?.GetValues("Authorization")?.FirstOrDefault();
            if (string.IsNullOrEmpty(headerString))
            {
                return new HttpStatusCodeResult(HttpStatusCode.Unauthorized);
            }

            try
            {
                var headerValue = AuthenticationHeaderValue.Parse(headerString);
                var claimsPrincipal = await this.tokenValidator.ValidateIdTokenAsync(headerValue.Parameter);
                var firstIdentity = claimsPrincipal.Identities.FirstOrDefault();

                var context = this.Request.GetOwinContext();
                context.Authentication.SignIn(new ClaimsIdentity(firstIdentity.Claims, CookieAuthenticationDefaults.AuthenticationType));

                return new HttpStatusCodeResult(HttpStatusCode.OK);
            }
            catch (Exception)
            {
                return new HttpStatusCodeResult(HttpStatusCode.Unauthorized);
            }
        }

        /// <summary>
        /// Logout.
        /// </summary>
        /// <returns>HTTP result</returns>
        // GET: Logout
        [HttpGet]
        public ActionResult Logout()
        {
            var context = this.Request.GetOwinContext();
            context.Authentication.SignOut(CookieAuthenticationDefaults.AuthenticationType);

            return new HttpStatusCodeResult(HttpStatusCode.OK);
        }
    }
}