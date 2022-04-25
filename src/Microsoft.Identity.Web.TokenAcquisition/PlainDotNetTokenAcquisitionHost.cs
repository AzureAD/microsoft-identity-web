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

        IOptionsMonitor<MicrosoftIdentityOptions> _microsoftIdentityOptionsMonitor;
        IOptionsMonitor<MergedOptions> _mergedOptionsMonitor;
        IOptionsMonitor<ConfidentialClientApplicationOptions> _ccaOptionsMonitor;

        public PlainDotNetTokenAcquisitionHost(
            IOptionsMonitor<MicrosoftIdentityOptions> optionsMonitor, 
            IOptionsMonitor<MergedOptions> mergedOptionsMonitor,
            IOptionsMonitor<ConfidentialClientApplicationOptions> ccaOptionsMonitor)
        {
            _microsoftIdentityOptionsMonitor = optionsMonitor;
            _mergedOptionsMonitor = mergedOptionsMonitor;
            _ccaOptionsMonitor = ccaOptionsMonitor;
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

            if (!mergedOptions.MergedWithCca)
            {
                _ccaOptionsMonitor.Get(effectiveAuthenticationScheme);
            }

            MergedOptions.UpdateMergedOptionsFromMicrosoftIdentityOptions(_microsoftIdentityOptionsMonitor.Get(effectiveAuthenticationScheme), mergedOptions);

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
