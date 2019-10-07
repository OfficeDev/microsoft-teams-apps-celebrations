// <copyright file="ClaimsPrincipalExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.Common.Extensions
{
    using System.Linq;
    using System.Security.Claims;

    /// <summary>
    /// Extension methods for <see cref="ClaimsPrincipal"/>.
    /// </summary>
    public static class ClaimsPrincipalExtensions
    {
        /// <summary>
        /// Get the value of the user principal name claim.
        /// </summary>
        /// <param name="principal">The claims principal instance</param>
        /// <returns>The UPN claim, if present, or null otherwise</returns>
        public static string GetUserPrincipalName(this ClaimsPrincipal principal)
        {
            return principal.Claims?.FirstOrDefault(x => x.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/upn")?.Value;
        }

        /// <summary>
        /// Get the value of the user object identifier claim.
        /// </summary>
        /// <param name="principal">The claims principal instance</param>
        /// <returns>The objectidentifier claim, if present, or null otherwise</returns>
        public static string GetUserObjectId(this ClaimsPrincipal principal)
        {
            return principal.Claims?.FirstOrDefault(x => x.Type == "http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value;
        }
    }
}
