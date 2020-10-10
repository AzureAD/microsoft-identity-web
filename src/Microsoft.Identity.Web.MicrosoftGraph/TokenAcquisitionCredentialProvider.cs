// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Graph;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Authentication provider based on ITokenAcquisition.
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
            // In case of a retry there is already an authorization header; replace it with new one to avoid using expired token
            if (request.Headers.Contains(Constants.Authorization))
            {
                request.Headers.Remove(Constants.Authorization);
            }

            request.Headers.Add(
                Constants.Authorization,
                string.Format(
                    CultureInfo.InvariantCulture,
                    "{0} {1}",
                    Constants.Bearer,
                    await _tokenAcquisition.GetAccessTokenForUserAsync(_initialScopes).ConfigureAwait(false)));
        }
    }
}
