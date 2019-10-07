// <copyright file="Startup.Auth.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.Celebration
{
    using System;
    using global::Owin;
    using Microsoft.Owin.Security;
    using Microsoft.Owin.Security.Cookies;
    using Microsoft.Teams.Apps.Celebration.Controllers;

    /// <summary>
    /// OWIN startup class
    /// </summary>
    public partial class Startup
    {
        /// <summary>
        /// Configure authentication for the app
        /// </summary>
        /// <param name="app">App builder</param>
        // For more information on configuring authentication, please visit https://go.microsoft.com/fwlink/?LinkId=301864
        public void ConfigureAuth(IAppBuilder app)
        {
            app.SetDefaultSignInAsAuthenticationType(CookieAuthenticationDefaults.AuthenticationType);

            app.UseCookieAuthentication(new CookieAuthenticationOptions
            {
                AuthenticationMode = AuthenticationMode.Active,
                AuthenticationType = CookieAuthenticationDefaults.AuthenticationType,
                LoginPath = new Owin.PathString(TabAuthController.LoginPath),
                ExpireTimeSpan = TimeSpan.FromDays(1),
                SlidingExpiration = true,
                Provider = new CookieAuthenticationProvider
                {
                    OnApplyRedirect = (context) =>
                    {
                        // Convert HTTP redirects to HTTPS
                        var redirectUri = context.RedirectUri;
                        if (!string.IsNullOrEmpty(redirectUri) && redirectUri.ToLowerInvariant().StartsWith("http://"))
                        {
                            redirectUri = "https://" + redirectUri.Substring(7);
                        }

                        context.Response.Redirect(redirectUri);
                    },
                },
            });
        }
    }
}