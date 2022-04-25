using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Tokens;

namespace Microsoft.Identity.Web
{
    internal interface ITokenAcquisitionHost
    {
        MergedOptions GetOptions(string? authenticationScheme, out string effectiveAuthenticationScheme);

        void SetSession(string key, string value);
        string GetEffectiveAuthenticationScheme(string? authenticationScheme);
        string? GetCurrentRedirectUri(MergedOptions mergedOptions);
        SecurityToken? GetTokenUsedToCallWebAPI();
        Task<ClaimsPrincipal?> GetAuthenticatedUserAsync(ClaimsPrincipal? user);
        ClaimsPrincipal? GetUserFromRequest();
        void SetHttpResponse(HttpStatusCode statusCode, string wwwAuthenticate);
    }
}
