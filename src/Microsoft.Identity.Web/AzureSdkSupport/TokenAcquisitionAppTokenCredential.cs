// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure.Core;
using Microsoft.Identity.Client;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Azure SDK token credential for App tokens based on the ITokenAcquisition service.
    /// </summary>
    [Obsolete("Use MicrosoftIdentityTokenCredential (registered via AddMicrosoftIdentityAzureTokenCredential). Set Options.RequestAppToken = true for app tokens. See https://aka.ms/ms-id-web/v3-to-v4", true)]
    public class TokenAcquisitionAppTokenCredential : TokenCredential
    {
        private ITokenAcquisition _tokenAcquisition;

        /// <summary>
        /// Constructor from an ITokenAcquisition service.
        /// </summary>
        /// <param name="tokenAcquisition">Token acquisition.</param>
        public TokenAcquisitionAppTokenCredential(ITokenAcquisition tokenAcquisition)
        {
            _tokenAcquisition = tokenAcquisition;
        }

        /// <inheritdoc/>
        public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken)
        {
            AuthenticationResult result = _tokenAcquisition.GetAuthenticationResultForAppAsync(requestContext.Scopes.First())
                .GetAwaiter()
                .GetResult();
            return new AccessToken(result.AccessToken, result.ExpiresOn);
        }

        /// <inheritdoc/>
        public override async ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken)
        {
            AuthenticationResult result = await _tokenAcquisition.GetAuthenticationResultForAppAsync(requestContext.Scopes.First()).ConfigureAwait(false);
            return new AccessToken(result.AccessToken, result.ExpiresOn);
        }
    }
}
