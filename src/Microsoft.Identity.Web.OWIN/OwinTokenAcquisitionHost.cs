// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Client;
using Microsoft.IdentityModel.Tokens;

namespace Microsoft.Identity.Web.Hosts
{
    internal class OwinTokenAcquisitionHost : ITokenAcquisitionHost
    {
        readonly IOptionsMonitor<MicrosoftIdentityOptions> _microsoftIdentityOptionsMonitor;
        readonly IMergedOptionsStore _mergedOptionsMonitor;
        readonly IOptionsMonitor<ConfidentialClientApplicationOptions> _ccaOptionsMonitor;
        readonly IOptionsMonitor<MicrosoftIdentityApplicationOptions> _MicrosoftIdentityApplicationOptionsMonitor;

        public OwinTokenAcquisitionHost(
            IOptionsMonitor<MicrosoftIdentityOptions> microsoftIdentityOptionsMonitor,
            IMergedOptionsStore mergedOptionsMonitor,
            IOptionsMonitor<ConfidentialClientApplicationOptions> ccaOptionsMonitor,
            IOptionsMonitor<MicrosoftIdentityApplicationOptions> MicrosoftIdentityApplicationOptionsMonitor)
        {
            _microsoftIdentityOptionsMonitor = microsoftIdentityOptionsMonitor;
            _mergedOptionsMonitor = mergedOptionsMonitor;
            _ccaOptionsMonitor = ccaOptionsMonitor;
            _MicrosoftIdentityApplicationOptionsMonitor = MicrosoftIdentityApplicationOptionsMonitor;
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
            _MicrosoftIdentityApplicationOptionsMonitor.Get(effectiveAuthenticationScheme); // force the PostConfigure

            DefaultCertificateLoader.UserAssignedManagedIdentityClientId = mergedOptions.UserAssignedManagedIdentityClientId;
            return mergedOptions;
        }

        public SecurityToken? GetTokenUsedToCallWebAPI()
        {
            return HttpContext.Current.User.GetBootstrapToken();
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
