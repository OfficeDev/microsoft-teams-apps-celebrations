// <copyright file="TokenValidator.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.Common.Authentication
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IdentityModel.Tokens.Jwt;
    using System.Linq;
    using System.Net.Http;
    using System.Security.Claims;
    using System.Threading.Tasks;
    using Microsoft.IdentityModel.Protocols;
    using Microsoft.IdentityModel.Protocols.OpenIdConnect;
    using Microsoft.IdentityModel.Tokens;
    using Microsoft.Teams.Apps.Common.Configuration;

    /// <summary>
    /// The TokenValidator is an implementation of the <see cref="ITokenValidator"/> interface.
    /// It uses AAD OAuth to validate a token and return the user's AAD objectId or an error.
    /// </summary>
    public class TokenValidator : ITokenValidator
    {
        private readonly string expectedAudience;
        private readonly ConfigurationManager<OpenIdConnectConfiguration> openIdV1ConfigManager;
        private readonly ConfigurationManager<OpenIdConnectConfiguration> openIdV2ConfigManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="TokenValidator"/> class.
        /// </summary>
        /// <param name="expectedAudience">The expected audience value.</param>
        public TokenValidator(string expectedAudience)
        {
            Debug.Assert(!string.IsNullOrEmpty(expectedAudience), "expectedAudience is null or empty");
            this.expectedAudience = expectedAudience ?? throw new ArgumentException("expectedAudience is null or empty", nameof(expectedAudience));

            var httpClient = new HttpClient();
            this.openIdV1ConfigManager = new ConfigurationManager<OpenIdConnectConfiguration>(
                "https://login.microsoftonline.com/common/.well-known/openid-configuration", new OpenIdConnectConfigurationRetriever(), httpClient);
            this.openIdV2ConfigManager = new ConfigurationManager<OpenIdConnectConfiguration>(
                "https://login.microsoftonline.com/common/v2.0/.well-known/openid-configuration", new OpenIdConnectConfigurationRetriever(), httpClient);
        }

        /// <inheritdoc/>
        public async Task<ClaimsPrincipal> ValidateIdTokenAsync(string token)
        {
            if (token == null)
            {
                return null;
            }

            var tokenPayload = this.ParseTokenPayload(token);
            if (tokenPayload == null)
            {
                return null;
            }

            var tidClaim = this.GetClaims(tokenPayload, "tid").FirstOrDefault();
            if (tidClaim == null)
            {
                return null;
            }

            var issClaim = this.GetClaims(tokenPayload, "iss").FirstOrDefault();
            if (issClaim == null)
            {
                return null;
            }

            // Test if the token is issued by the V2 AAD issuer
            var openIdConfig = await this.openIdV2ConfigManager.GetConfigurationAsync();
            var expectedIssuer = openIdConfig.Issuer.Replace("{tenantid}", tidClaim.Value);
            if (!issClaim.Value.Equals(expectedIssuer, StringComparison.InvariantCulture))
            {
                // If the token is not issued by the V2 AAD issuer, then try the V1 issuer.
                openIdConfig = await this.openIdV1ConfigManager.GetConfigurationAsync();
                expectedIssuer = openIdConfig.Issuer.Replace("{tenantid}", tidClaim.Value);
            }

            TokenValidationParameters validationParameters = new TokenValidationParameters
            {
                ValidateAudience = true,
                ValidateIssuer = true,
                ValidateLifetime = true,
                IssuerSigningKeys = openIdConfig.SigningKeys,
                ValidAudience = this.expectedAudience,
                ValidIssuer = expectedIssuer,
            };

            JwtSecurityTokenHandler tokenHandler = new JwtSecurityTokenHandler();
            try
            {
                return tokenHandler.ValidateToken(token, validationParameters, out _);
            }
            catch (Exception)
            {
                return null;
            }
        }

        private JwtSecurityToken ParseTokenPayload(string token)
        {
            if (token == null)
            {
                throw new ArgumentNullException(nameof(token));
            }

            try
            {
                JwtSecurityTokenHandler tokenHandler = new JwtSecurityTokenHandler();
                if (!tokenHandler.CanReadToken(token))
                {
                    return null;
                }

                return tokenHandler.ReadJwtToken(token);
            }
            catch (Exception)
            {
                return null;
            }
        }

        private IEnumerable<Claim> GetClaims(JwtSecurityToken token, string claimType)
        {
            return token.Claims.Where(c => c.Type.Equals(claimType, StringComparison.InvariantCultureIgnoreCase));
        }
    }
}
