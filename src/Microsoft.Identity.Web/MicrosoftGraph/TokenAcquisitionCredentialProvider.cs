// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Graph;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Authentication provider based on MSAL.NET.
    /// </summary>
    internal class TokenAcquisitionCredentialProvider : IAuthenticationProvider
    {
        public TokenAcquisitionCredentialProvider(ITokenAcquisition tokenAcquisition, IEnumerable<string> initialScopes)
        {
            _tokenAcquisition = tokenAcquisition;
            _initialScopes = initialScopes;
        }

        private ITokenAcquisition _tokenAcquisition;
        private IEnumerable<string> _initialScopes;

        /// <summary>
        /// Adds a bearer header to an HttpRequestMessage.
        /// </summary>
        /// <param name="request">HttpRequest message to authenticate.</param>
        /// <returns>A Task (as this is an async method).</returns>
        public async Task AuthenticateRequestAsync(HttpRequestMessage request)
        {
            request.Headers.Add(
                "Authorization",
                $"Bearer {await _tokenAcquisition.GetAccessTokenForUserAsync(_initialScopes).ConfigureAwait(false)}");
        }
    }
}
