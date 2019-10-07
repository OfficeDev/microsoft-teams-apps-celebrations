// <copyright file="ITokenValidator.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.Common.Authentication
{
    using System.Security.Claims;
    using System.Threading.Tasks;

    /// <summary>
    /// Interface to validate tokens.
    /// </summary>
    public interface ITokenValidator
    {
        /// <summary>
        /// Validates a JWT token and extracts the claims principal and AAD objectId of the user.
        /// </summary>
        /// <param name="token">The token to validate.</param>
        /// <returns>The claims principal or null if the token is not valid.</returns>
        Task<ClaimsPrincipal> ValidateIdTokenAsync(string token);
    }
}
