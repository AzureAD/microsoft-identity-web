// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Microsoft.Identity.Web.Hosts
{
    internal class OwinTokenAcquisitionHost : ITokenAcquisitionHost
    {
        readonly IOptionsMonitor<MicrosoftIdentityOptions> _microsoftIdentityOptionsMonitor;
        readonly IOptionsMonitor<MergedOptions> _mergedOptionsMonitor;
        readonly IOptionsMonitor<ConfidentialClientApplicationOptions> _ccaOptionsMonitor;
        readonly IOptionsMonitor<MicrosoftAuthenticationOptions> _microsoftAuthenticationOptionsMonitor;

        public OwinTokenAcquisitionHost(
            IOptionsMonitor<MicrosoftIdentityOptions> microsoftIdentityOptionsMonitor, 
            IOptionsMonitor<MergedOptions> mergedOptionsMonitor,
            IOptionsMonitor<ConfidentialClientApplicationOptions> ccaOptionsMonitor,
            IOptionsMonitor<MicrosoftAuthenticationOptions> microsoftAuthenticationOptionsMonitor)
        {
            _microsoftIdentityOptionsMonitor = microsoftIdentityOptionsMonitor;
            _mergedOptionsMonitor = mergedOptionsMonitor;
            _ccaOptionsMonitor = ccaOptionsMonitor;
            _microsoftAuthenticationOptionsMonitor = microsoftAuthenticationOptionsMonitor;
        }

        public Task<ClaimsPrincipal?> GetAuthenticatedUserAsync(ClaimsPrincipal? user)
        {
            return Task.FromResult<ClaimsPrincipal?>(null);
        }

        public string? GetCurrentRedirectUri(MergedOptions mergedOptions)
        {
            return mergedOptions.RedirectUri;
        }

        public string GetEffectiveAuthenticationScheme(string? authenticationScheme)
        {
            return authenticationScheme ?? string.Empty;
        }

        public MergedOptions GetOptions(string? authenticationScheme, out string effectiveAuthenticationScheme)
        {
            effectiveAuthenticationScheme = GetEffectiveAuthenticationScheme(authenticationScheme);
            var mergedOptions = _mergedOptionsMonitor.Get(effectiveAuthenticationScheme);

            if (!mergedOptions.MergedWithCca)
            {
                _ccaOptionsMonitor.Get(effectiveAuthenticationScheme);
            }

            MergedOptions.UpdateMergedOptionsFromMicrosoftIdentityOptions(_microsoftIdentityOptionsMonitor.Get(effectiveAuthenticationScheme), mergedOptions);

            if (string.IsNullOrEmpty(mergedOptions.Instance) || string.IsNullOrEmpty(mergedOptions.ClientId))
            {
                MicrosoftAuthenticationOptions? microsoftAuthenticationOptions = _microsoftAuthenticationOptionsMonitor.Get(effectiveAuthenticationScheme);
                if (microsoftAuthenticationOptions != null)
                {
                    MergedOptions.UpdateMergedOptionsFromMicrosoftAuthenticationOptions(microsoftAuthenticationOptions, mergedOptions);
                }
            }

            DefaultCertificateLoader.UserAssignedManagedIdentityClientId = mergedOptions.UserAssignedManagedIdentityClientId;
            return mergedOptions;
        }

        public SecurityToken? GetTokenUsedToCallWebAPI()
        {
            object? o = (HttpContext.Current.User.Identity as ClaimsIdentity)?.BootstrapContext;

            // TODO: do better as this won't do for JWE and wastes time. The token was already decrypted.
            return new JsonWebToken(o as string);
        }

        public ClaimsPrincipal? GetUserFromRequest()
        {
            return HttpContext.Current.User as ClaimsPrincipal;
        }

        public void SetHttpResponse(HttpStatusCode statusCode, string wwwAuthenticate)
        {
        }

        public void SetSession(string key, string value)
        {
        }
    }
}
