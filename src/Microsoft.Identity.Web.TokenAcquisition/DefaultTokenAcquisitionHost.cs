// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Client;
using Microsoft.IdentityModel.Tokens;

namespace Microsoft.Identity.Web.Hosts
{
    internal sealed class DefaultTokenAcquisitionHost : ITokenAcquisitionHost
    {
        readonly IOptionsMonitor<MicrosoftIdentityOptions> _microsoftIdentityOptionsMonitor;
        readonly IMergedOptionsStore _mergedOptionsMonitor;
        readonly IOptionsMonitor<ConfidentialClientApplicationOptions> _ccaOptionsMonitor;
        readonly IOptionsMonitor<MicrosoftIdentityApplicationOptions> _MicrosoftIdentityApplicationOptionsMonitor;

        public DefaultTokenAcquisitionHost(
            IOptionsMonitor<MicrosoftIdentityOptions> optionsMonitor, 
            IMergedOptionsStore mergedOptionsMonitor,
            IOptionsMonitor<ConfidentialClientApplicationOptions> ccaOptionsMonitor,
            IOptionsMonitor<MicrosoftIdentityApplicationOptions> microsoftIdentityApplicationOptionsMonitor)
        {
            _microsoftIdentityOptionsMonitor = optionsMonitor;
            _mergedOptionsMonitor = mergedOptionsMonitor;
            _ccaOptionsMonitor = ccaOptionsMonitor;
            _MicrosoftIdentityApplicationOptionsMonitor = microsoftIdentityApplicationOptionsMonitor;
        }

        public Task<ClaimsPrincipal?> GetAuthenticatedUserAsync(ClaimsPrincipal? user)
        {
            return Task.FromResult<ClaimsPrincipal?>(null);
        }

        public string? GetCurrentRedirectUri(MergedOptions mergedOptions)
        {
            return null;
        }

        public string GetEffectiveAuthenticationScheme(string? authenticationScheme)
        {
            return authenticationScheme ?? string.Empty;
        }

        public MergedOptions GetOptions(string? authenticationScheme, out string effectiveAuthenticationScheme)
        {
            effectiveAuthenticationScheme = GetEffectiveAuthenticationScheme(authenticationScheme);
            var mergedOptions = _mergedOptionsMonitor.Get(effectiveAuthenticationScheme);

            // TODO can we factorize somewhere else?
            MergedOptions.ParseAuthorityIfNecessary(mergedOptions);

            if (!mergedOptions.MergedWithCca)
            {
                _ccaOptionsMonitor.Get(effectiveAuthenticationScheme);
            }

            _microsoftIdentityOptionsMonitor.Get(effectiveAuthenticationScheme);
            _MicrosoftIdentityApplicationOptionsMonitor.Get(effectiveAuthenticationScheme);

            // Get the merged options again after the merging has occurred
            mergedOptions = _mergedOptionsMonitor.Get(effectiveAuthenticationScheme);

            if (string.IsNullOrEmpty(mergedOptions.Instance))
            {
                // Check if the issue is that MicrosoftIdentityApplicationOptions are not configured
                var microsoftIdentityApplicationOptions = _MicrosoftIdentityApplicationOptionsMonitor.Get(effectiveAuthenticationScheme);
                bool isMicrosoftIdentityApplicationOptionsConfigured = microsoftIdentityApplicationOptions != null && 
                    (!string.IsNullOrEmpty(microsoftIdentityApplicationOptions.Instance) || 
                     !string.IsNullOrEmpty(microsoftIdentityApplicationOptions.Authority) ||
                     !string.IsNullOrEmpty(microsoftIdentityApplicationOptions.ClientId));

                if (!isMicrosoftIdentityApplicationOptionsConfigured)
                {
                    string msg = string.Format(System.Globalization.CultureInfo.InvariantCulture, IDWebErrorMessage.MicrosoftIdentityApplicationOptionsNotConfigured,
                        effectiveAuthenticationScheme);
                    throw new InvalidOperationException(msg);
                }
                else
                {
                    string msg = string.Format(System.Globalization.CultureInfo.InvariantCulture, IDWebErrorMessage.ProvidedAuthenticationSchemeIsIncorrect,
                        authenticationScheme, effectiveAuthenticationScheme, string.Empty);
                    throw new InvalidOperationException(msg);
                }
            }

            DefaultCertificateLoader.UserAssignedManagedIdentityClientId = mergedOptions.UserAssignedManagedIdentityClientId;
            return mergedOptions;
        }

        public SecurityToken? GetTokenUsedToCallWebAPI()
        {
            return null;
        }

        public ClaimsPrincipal? GetUserFromRequest()
        {
            return null;
        }

        public void SetHttpResponse(HttpStatusCode statusCode, string wwwAuthenticate)
        {
        }

        public void SetSession(string key, string value)
        {
        }
    }
}
