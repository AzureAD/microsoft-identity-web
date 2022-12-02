// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Client;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Microsoft.Identity.Web.Hosts
{
    internal class OwinTokenAcquisitionHost : ITokenAcquisitionHost
    {
        readonly IOptionsMonitor<MicrosoftIdentityOptions> _microsoftIdentityOptionsMonitor;
        readonly IMergedOptionsStore _mergedOptionsMonitor;
        readonly IOptionsMonitor<ConfidentialClientApplicationOptions> _ccaOptionsMonitor;
        readonly IOptionsMonitor<MicrosoftAuthenticationOptions> _microsoftAuthenticationOptionsMonitor;

        public OwinTokenAcquisitionHost(
            IOptionsMonitor<MicrosoftIdentityOptions> microsoftIdentityOptionsMonitor,
            IMergedOptionsStore mergedOptionsMonitor,
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
            return Task.FromResult<ClaimsPrincipal?>(HttpContext.Current.User as ClaimsPrincipal);
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

            // TODO can we factorize somewhere else?
            MergedOptions.ParseAuthorityIfNecessary(mergedOptions);
            if (!mergedOptions.MergedWithCca)
            {
                _ccaOptionsMonitor.Get(effectiveAuthenticationScheme);
            }

            _microsoftIdentityOptionsMonitor.Get(effectiveAuthenticationScheme); // force the PostConfigure
            _microsoftAuthenticationOptionsMonitor.Get(effectiveAuthenticationScheme); // force the PostConfigure

            DefaultCertificateLoader.UserAssignedManagedIdentityClientId = mergedOptions.UserAssignedManagedIdentityClientId;
            return mergedOptions;
        }

        public SecurityToken? GetTokenUsedToCallWebAPI()
        {
            object? o = (HttpContext.Current.User.Identity as ClaimsIdentity)?.BootstrapContext;

            if (o != null)
            {
                // TODO: do better as this won't do for JWE and wastes time. The token was already decrypted.
                return new JsonWebToken(o as string);
            }
            return null;
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
