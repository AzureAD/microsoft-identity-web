// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using Microsoft.Identity.Web.InstanceDiscovery;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Tokens;

namespace Microsoft.Identity.Web.Resource
{
    /// <summary>
    /// Generic class that validates token issuer from the provided Azure AD authority.
    /// </summary>
    public class AadIssuerValidator
    {
        internal AadIssuerValidator(
            HttpClient? httpClient,
            string aadAuthority)
        {
            HttpClient = httpClient;
            AadAuthority = aadAuthority.TrimEnd('/');
        }

        private HttpClient? HttpClient { get; }
        internal string? AadIssuerV1 { get; set; }
        internal string? AadIssuerV2 { get; set; }
        internal string AadAuthority { get; set; }

        /// <summary>
        /// Validate the issuer for multi-tenant applications of various audiences (Work and School accounts, or Work and School accounts +
        /// Personal accounts).
        /// </summary>
        /// <param name="actualIssuer">Issuer to validate (will be tenanted).</param>
        /// <param name="securityToken">Received security token.</param>
        /// <param name="validationParameters">Token validation parameters.</param>
        /// <remarks>The issuer is considered as valid if it has the same HTTP scheme and authority as the
        /// authority from the configuration file, has a tenant ID, and optionally v2.0 (this web API
        /// accepts both V1 and V2 tokens).</remarks>
        /// <returns>The <c>issuer</c> if it's valid, or otherwise <c>SecurityTokenInvalidIssuerException</c> is thrown.</returns>
        /// <exception cref="ArgumentNullException"> if <paramref name="securityToken"/> is null.</exception>
        /// <exception cref="ArgumentNullException"> if <paramref name="validationParameters"/> is null.</exception>
        /// <exception cref="SecurityTokenInvalidIssuerException">if the issuer is invalid. </exception>
        public string Validate(
            string actualIssuer,
            SecurityToken securityToken,
            TokenValidationParameters validationParameters)
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
                throw new SecurityTokenInvalidIssuerException(IssuerValidatorErrorMessage.TenantIdClaimNotPresentInToken);
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

            if (validationParameters.ValidIssuer != null)
            {
                if (IsValidIssuer(validationParameters.ValidIssuer, tenantId, actualIssuer))
                {
                    return actualIssuer;
                }
            }

            try
            {
                if (securityToken.Issuer.EndsWith("v2.0", StringComparison.OrdinalIgnoreCase))
                {
                    if (AadIssuerV2 == null)
                    {
                        IssuerMetadata issuerMetadata =
                            CreateConfigManager(AadAuthority).GetConfigurationAsync().ConfigureAwait(false).GetAwaiter().GetResult();
                        AadIssuerV2 = issuerMetadata.Issuer!;
                    }

                    if (IsValidIssuer(AadIssuerV2, tenantId, actualIssuer))
                    {
                        return actualIssuer;
                    }
                }
                else
                {
                    if (AadIssuerV1 == null)
                    {
                        IssuerMetadata issuerMetadata =
                            CreateConfigManager(CreateV1Authority()).GetConfigurationAsync().ConfigureAwait(false).GetAwaiter().GetResult();
                        AadIssuerV1 = issuerMetadata.Issuer!;
                    }

                    if (IsValidIssuer(AadIssuerV1, tenantId, actualIssuer))
                    {
                        return actualIssuer;
                    }
                }
            }
            catch
            {
            }

            // If a valid issuer is not found, throw
            throw new SecurityTokenInvalidIssuerException(
                string.Format(
                    CultureInfo.InvariantCulture,
                    IssuerValidatorErrorMessage.IssuerDoesNotMatchValidIssuers,
                    actualIssuer));
        }

        private string CreateV1Authority()
        {
#if DOTNET_STANDARD_20 || DOTNET_462 || DOTNET_472
            if (AadAuthority.Contains(IssuerValidatorConstants.Organizations))
            {
                return AadAuthority.Replace($"{IssuerValidatorConstants.Organizations}/v2.0", IssuerValidatorConstants.Common);
            }

            return AadAuthority.Replace("/v2.0", string.Empty);
#else

            if (AadAuthority.Contains(IssuerValidatorConstants.Organizations, StringComparison.OrdinalIgnoreCase))
            {
                return AadAuthority.Replace($"{IssuerValidatorConstants.Organizations}/v2.0", IssuerValidatorConstants.Common, StringComparison.OrdinalIgnoreCase);
            }

            return AadAuthority.Replace("/v2.0", string.Empty, StringComparison.OrdinalIgnoreCase);
#endif
        }

        private ConfigurationManager<IssuerMetadata> CreateConfigManager(
            string aadAuthority)
        {
            if (HttpClient != null)
            {
                return
                 new ConfigurationManager<IssuerMetadata>(
                     $"{aadAuthority}{IssuerValidatorConstants.OidcEndpoint}",
                     new IssuerConfigurationRetriever(),
                     HttpClient);
            }
            else
            {
                return
                new ConfigurationManager<IssuerMetadata>(
                    $"{aadAuthority}{IssuerValidatorConstants.OidcEndpoint}",
                    new IssuerConfigurationRetriever());
            }
        }

        private bool IsValidIssuer(string validIssuerTemplate, string tenantId, string actualIssuer)
        {
            if (string.IsNullOrEmpty(validIssuerTemplate))
            {
                return false;
            }

            try
            {
#if DOTNET_STANDARD_20 || DOTNET_462 || DOTNET_472
                Uri issuerFromTemplateUri = new Uri(validIssuerTemplate.Replace("{tenantid}", tenantId));
#else
                Uri issuerFromTemplateUri = new Uri(validIssuerTemplate.Replace("{tenantid}", tenantId, StringComparison.OrdinalIgnoreCase));
#endif
                Uri actualIssuerUri = new Uri(actualIssuer);

                return issuerFromTemplateUri.AbsoluteUri == actualIssuerUri.AbsoluteUri;
            }
            catch
            {
                // if something faults, ignore
            }

            return false;
        }

        /// <summary>Gets the tenant ID from a token.</summary>
        /// <param name="securityToken">A JWT token.</param>
        /// <returns>A string containing the tenant ID, if found or <see cref="string.Empty"/>.</returns>
        /// <remarks>Only <see cref="JwtSecurityToken"/> and <see cref="JsonWebToken"/> are acceptable types.</remarks>
        private static string GetTenantIdFromToken(SecurityToken securityToken)
        {
            if (securityToken is JwtSecurityToken jwtSecurityToken)
            {
                if (jwtSecurityToken.Payload.TryGetValue(IssuerValidatorConstants.Tid, out object? tid))
                {
                    return (string)tid;
                }

                jwtSecurityToken.Payload.TryGetValue(IssuerValidatorConstants.TenantId, out object? tenantId);
                if (tenantId != null)
                {
                    return (string)tenantId;
                }

                // Since B2C doesn't have "tid" as default, get it from issuer
                return GetTenantIdFromIss(jwtSecurityToken.Issuer);
            }

            if (securityToken is JsonWebToken jsonWebToken)
            {
                jsonWebToken.TryGetPayloadValue(IssuerValidatorConstants.Tid, out string? tid);
                if (tid != null)
                {
                    return tid;
                }

                jsonWebToken.TryGetPayloadValue(IssuerValidatorConstants.TenantId, out string? tenantId);
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

            if (uri.Segments.Length == 5 && uri.Segments[1].TrimEnd('/') == IssuerValidatorConstants.Tfp)
            {
                throw new SecurityTokenInvalidIssuerException(IssuerValidatorErrorMessage.B2CTfpIssuerNotSupported);
            }

            return string.Empty;
        }
    }
}
