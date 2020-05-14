// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Graph;
using Microsoft.Identity.Web;
using System.Net.Http;
using System.Threading.Tasks;

namespace WebAppCallsMicrosoftGraph
{
    public class WebSignInCredential : IAuthenticationProvider
    {
        public WebSignInCredential(ITokenAcquisition tokenAcquisition)
        {
            _tokenAcquisition = tokenAcquisition;
        }

        ITokenAcquisition _tokenAcquisition;

        public async Task AuthenticateRequestAsync(HttpRequestMessage request)
        {
            request.Headers.Add("Authorization",
                $"Bearer {await _tokenAcquisition.GetAccessTokenForUserAsync(new string[0])}");
        }
    }
}
