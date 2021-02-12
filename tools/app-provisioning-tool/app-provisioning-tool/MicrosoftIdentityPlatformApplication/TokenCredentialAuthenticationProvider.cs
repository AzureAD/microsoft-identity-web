using Azure.Core;
using Microsoft.Graph;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace DotnetTool.MicrosoftIdentityPlatformApplication
{
    /// <summary>
    /// Graph SDK authentication provider based on an Azure SDK token credential provider.
    /// </summary>
    internal class TokenCredentialAuthenticationProvider : IAuthenticationProvider
    {
        public TokenCredentialAuthenticationProvider(
            TokenCredential tokenCredentials, 
            IEnumerable<string>? initialScopes = null)
        {
            _tokenCredentials = tokenCredentials;
            _initialScopes = initialScopes ?? new string[] { "Application.ReadWrite.All" };
        }

        readonly TokenCredential _tokenCredentials;
        readonly IEnumerable<string> _initialScopes;

        public async Task AuthenticateRequestAsync(HttpRequestMessage request)
        {
            string accessToken;

            // Try with the Shared token cache credentials

                TokenRequestContext context = new TokenRequestContext(_initialScopes.ToArray());
                AccessToken token = await _tokenCredentials.GetTokenAsync(context, CancellationToken.None);
                accessToken = token.Token;


            request.Headers.Remove("Authorization");
            request.Headers.Add("Authorization", $"bearer {accessToken}");
        }
    }
}
