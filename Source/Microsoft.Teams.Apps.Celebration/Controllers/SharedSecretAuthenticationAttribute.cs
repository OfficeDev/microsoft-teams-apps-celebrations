// <copyright file="SharedSecretAuthenticationAttribute.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.Celebration.Controllers
{
    using System;
    using System.Configuration;
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Web.Http.Controllers;
    using System.Web.Http.Filters;

    /// <summary>
    /// Action filter that authenticates an incoming request against a shared secret
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public class SharedSecretAuthenticationAttribute : ActionFilterAttribute
    {
        /// <summary>
        /// Gets or sets the name of the setting that has the shared secret
        /// </summary>
        public string SharedSecretSettingName { get; set; }

        /// <inheritdoc/>
        public override Task OnActionExecutingAsync(HttpActionContext actionContext, CancellationToken cancellationToken)
        {
            var authHeader = actionContext.Request.Headers.Authorization;
            if (authHeader == null ||
                authHeader.Scheme != "SharedSecret" ||
                authHeader.Parameter != ConfigurationManager.AppSettings[this.SharedSecretSettingName])
            {
                actionContext.Response = actionContext.Request.CreateResponse(HttpStatusCode.Unauthorized);
                return Task.CompletedTask;
            }

            return base.OnActionExecutingAsync(actionContext, cancellationToken);
        }
    }
}
