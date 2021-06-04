// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Microsoft.Identity.Web.Resource
{
    /// <summary>
    /// Generic class that validates token issuer from the provided Azure AD authority.
    /// </summary>
    public class AadIssuerValidator
    {
        /// <summary>
        /// A list of all Issuers across the various Azure AD instances.
        /// </summary>
        private readonly ISet<string> _issuerAliases;

        internal /*internal for tests*/ AadIssuerValidator(IEnumerable<string> aliases)
        {
            _issuerAliases = new HashSet<string>(aliases, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Validate the issuer for multi-tenant applications of various audiences (Work and School accounts, or Work and School accounts +
        /// Personal accounts).
        /// </summary>
        /// <param name="actualIssuer">Issuer to validate (will be tenanted).</param>
        /// <param name="securityToken">Received security token.</param>
        /// <param name="validationParameters">Token validation parameters.</param>
        /// <remarks>The issuer is considered as valid if it has the same HTTP scheme and authority as the
        /// authority from the configuration file, has a tenant ID, and optionally v2.0 (this web API
        /// accepts both V1 and V2 tokens).
        /// Authority aliasing is also taken into account.</remarks>
        /// <returns>The <c>issuer</c> if it's valid, or otherwise <c>SecurityTokenInvalidIssuerException</c> is thrown.</returns>
        /// <exception cref="ArgumentNullException"> if <paramref name="securityToken"/> is null.</exception>
        /// <exception cref="ArgumentNullException"> if <paramref name="validationParameters"/> is null.</exception>
        /// <exception cref="SecurityTokenInvalidIssuerException">if the issuer is invalid. </exception>
        public string Validate(string actualIssuer, SecurityToken securityToken, TokenValidationParameters validationParameters)
        {
            if (string.IsNullOrEmpty(actualIssuer))
            {
                throw new ArgumentNullException(nameof(actualIssuer));
            }

            if (securityToken == null)
            {
                throw new ArgumentNullException(nameof(securityToken));
            }

            if (validationParameters == null)
            {
                throw new ArgumentNullException(nameof(validationParameters));
            }

            string tenantId = GetTenantIdFromToken(securityToken);
            if (string.IsNullOrWhiteSpace(tenantId))
            {
                throw new SecurityTokenInvalidIssuerException(IDWebErrorMessage.TenantIdClaimNotPresentInToken);
            }

            if (validationParameters.ValidIssuers != null)
            {
                foreach (var validIssuerTemplate in validationParameters.ValidIssuers)
                {
                    if (IsValidIssuer(validIssuerTemplate, tenantId, actualIssuer))
                    {
                        return actualIssuer;
                    }
                }
            }

            if (IsValidIssuer(validationParameters.ValidIssuer, tenantId, actualIssuer))
            {
                return actualIssuer;
            }

            // If a valid issuer is not found, throw
            throw new SecurityTokenInvalidIssuerException(
                string.Format(
                    CultureInfo.InvariantCulture,
                    IDWebErrorMessage.IssuerDoesNotMatchValidIssuers,
                    actualIssuer));
        }

        private bool IsValidIssuer(string validIssuerTemplate, string tenantId, string actualIssuer)
        {
            if (string.IsNullOrEmpty(validIssuerTemplate))
            {
                return false;
            }

            try
            {
                Uri issuerFromTemplateUri = new Uri(validIssuerTemplate.Replace("{tenantid}", tenantId, StringComparison.OrdinalIgnoreCase));
                Uri actualIssuerUri = new Uri(actualIssuer);

                // Template authority is in the aliases
                return _issuerAliases.Contains(issuerFromTemplateUri.Authority) &&
                       // "iss" authority is in the aliases
                       _issuerAliases.Contains(actualIssuerUri.Authority) &&
                      // Template authority ends in the tenant ID
                      IsValidTidInLocalPath(tenantId, issuerFromTemplateUri) &&
                      // "iss" ends in the tenant ID
                      IsValidTidInLocalPath(tenantId, actualIssuerUri);
            }
            catch
            {
                // if something faults, ignore
            }

            return false;
        }

        private static bool IsValidTidInLocalPath(string tenantId, Uri uri)
        {
            string trimmedLocalPath = uri.LocalPath.Trim('/');
            return trimmedLocalPath == tenantId || trimmedLocalPath == $"{tenantId}/v2.0";
        }

        /// <summary>Gets the tenant ID from a token.</summary>
        /// <param name="securityToken">A JWT token.</param>
        /// <returns>A string containing the tenant ID, if found or <see cref="string.Empty"/>.</returns>
        /// <remarks>Only <see cref="JwtSecurityToken"/> and <see cref="JsonWebToken"/> are acceptable types.</remarks>
        private static string GetTenantIdFromToken(SecurityToken securityToken)
        {
            if (securityToken is JwtSecurityToken jwtSecurityToken)
            {
                if (jwtSecurityToken.Payload.TryGetValue(ClaimConstants.Tid, out object? tid))
                {
                    return (string)tid;
                }

                jwtSecurityToken.Payload.TryGetValue(ClaimConstants.TenantId, out object? tenantId);
                if (tenantId != null)
                {
                    return (string)tenantId;
                }

                // Since B2C doesn't have "tid" as default, get it from issuer
                return GetTenantIdFromIss(jwtSecurityToken.Issuer);
            }

            if (securityToken is JsonWebToken jsonWebToken)
            {
                jsonWebToken.TryGetPayloadValue(ClaimConstants.Tid, out string? tid);
                if (tid != null)
                {
                    return tid;
                }

                jsonWebToken.TryGetPayloadValue(ClaimConstants.TenantId, out string? tenantId);
                if (tenantId != null)
                {
                    return tenantId;
                }

                // Since B2C doesn't have "tid" as default, get it from issuer
                return GetTenantIdFromIss(jsonWebToken.Issuer);
            }

            return string.Empty;
        }

        // The AAD "iss" claims contains the tenant ID in its value.
        // The URI can be
        // - {domain}/{tid}/v2.0
        // - {domain}/{tid}/v2.0/
        // - {domain}/{tfp}/{tid}/{userFlow}/v2.0/
        private static string GetTenantIdFromIss(string iss)
        {
            if (string.IsNullOrEmpty(iss))
            {
                return string.Empty;
            }

            var uri = new Uri(iss);

            if (uri.Segments.Length == 3)
            {
                return uri.Segments[1].TrimEnd('/');
            }

            if (uri.Segments.Length == 5 && uri.Segments[1].TrimEnd('/') == ClaimConstants.Tfp)
            {
                throw new SecurityTokenInvalidIssuerException(IDWebErrorMessage.B2CTfpIssuerNotSupported);
            }

            return string.Empty;
        }

        /// <summary>
        /// This method is now Obsolete.
        /// </summary>
        /// <param name="aadAuthority">Aad authority.</param>
        /// <returns>NotImplementedException.</returns>
        [Obsolete(IDWebErrorMessage.AadIssuerValidatorGetIssuerValidatorIsObsolete, true)]
        public static AadIssuerValidator GetIssuerValidator(string aadAuthority)
        {
            throw new NotImplementedException(IDWebErrorMessage.AadIssuerValidatorGetIssuerValidatorIsObsolete);
        }
    }
}
