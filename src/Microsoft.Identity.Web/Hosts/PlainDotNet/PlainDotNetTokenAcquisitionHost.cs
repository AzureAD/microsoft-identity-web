using System;
using System.Globalization;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;
using Microsoft.IdentityModel.Tokens;

namespace Microsoft.Identity.Web.Hosts
{
    internal class PlainDotNetTokenAcquisitionHost : ITokenAcquisitionHost
    {

        IOptionsMonitor<MicrosoftIdentityOptions> _optionsMonitor;
        IOptionsMonitor<MergedOptions> _mergedOptionsMonitor;
        IOptionsMonitor<ConfidentialClientApplicationOptions> _ccaOptionsMonitor;

        public PlainDotNetTokenAcquisitionHost(
            IOptionsMonitor<MicrosoftIdentityOptions> optionsMonitor, 
            IOptionsMonitor<MergedOptions> mergedOptionsMonitor,
            IOptionsMonitor<ConfidentialClientApplicationOptions> ccaOptionsMonitor)
        {
            _optionsMonitor = optionsMonitor;
            _mergedOptionsMonitor = mergedOptionsMonitor;
            _ccaOptionsMonitor = ccaOptionsMonitor;
        }

        Task<ClaimsPrincipal?> ITokenAcquisitionHost.GetAuthenticatedUserAsync(ClaimsPrincipal? user)
        {
            return Task.FromResult<ClaimsPrincipal?>(null);
        }

        string? ITokenAcquisitionHost.GetCurrentRedirectUri(MergedOptions mergedOptions)
        {
            return null;
        }

        string ITokenAcquisitionHost.GetEffectiveAuthenticationScheme(string? authenticationScheme)
        {
            return authenticationScheme!;
        }

        MergedOptions ITokenAcquisitionHost.GetOptions(string? authenticationScheme, out string effectiveAuthenticationScheme)
        {
            var mergedOptions = _mergedOptionsMonitor.Get(authenticationScheme);
            effectiveAuthenticationScheme = authenticationScheme;
            MergedOptions.UpdateMergedOptionsFromMicrosoftIdentityOptions(
             /*authenticationScheme == null ? _optionsMonitor.CurrentValue :*/ _optionsMonitor.Get(authenticationScheme),
             mergedOptions);

            DefaultCertificateLoader.UserAssignedManagedIdentityClientId = mergedOptions.UserAssignedManagedIdentityClientId;
            return mergedOptions;
        }

        SecurityToken? ITokenAcquisitionHost.GetTokenUsedToCallWebAPI()
        {
            return null;
        }

        ClaimsPrincipal? ITokenAcquisitionHost.GetUserFromRequest()
        {
            return null;
        }

        void ITokenAcquisitionHost.SetHttpResponse(HttpStatusCode statusCode, string wwwAuthenticate)
        {
        }

        void ITokenAcquisitionHost.SetSession(string key, string value)
        {
        }
    }
}
