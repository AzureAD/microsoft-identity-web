// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Core;
using Microsoft.Identity.Client;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Azure SDK token credential based on the ITokenAcquisition service.
    /// </summary>
    [Obsolete("Use MicrosoftIdentityTokenCredential (registered via AddMicrosoftIdentityAzureTokenCredential). See https://aka.ms/ms-id-web/v3-to-v4", true)]
    public class TokenAcquisitionTokenCredential : TokenCredential
    {
        private ITokenAcquisition _tokenAcquisition;

        /// <summary>
        /// Constructor from an ITokenAcquisition service.
        /// </summary>
        /// <param name="tokenAcquisition">Token acquisition.</param>
        public TokenAcquisitionTokenCredential(ITokenAcquisition tokenAcquisition)
        {
            _tokenAcquisition = tokenAcquisition;
        }

        /// <inheritdoc/>
        public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken)
        {
            AuthenticationResult result = _tokenAcquisition.GetAuthenticationResultForUserAsync(requestContext.Scopes)
                .GetAwaiter()
                .GetResult();
            return new AccessToken(result.AccessToken, result.ExpiresOn);
        }

        /// <inheritdoc/>
        public override async ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken)
        {
            AuthenticationResult result = await _tokenAcquisition.GetAuthenticationResultForUserAsync(requestContext.Scopes).ConfigureAwait(false);
            return new AccessToken(result.AccessToken, result.ExpiresOn);
        }
    }
}
