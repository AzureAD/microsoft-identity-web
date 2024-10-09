// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;
using Azure.Core;
using Microsoft.Identity.Abstractions;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Azure SDK token credential based on the ITokenAcquisition service.
    /// </summary>
    public class TokenAcquirerTokenCredential : TokenCredential
    {
        private ITokenAcquirer _tokenAcquirer;

        /// <summary>
        /// Constructor from an ITokenAcquisition service.
        /// </summary>
        /// <param name="tokenAcquirer">Token acquisition.</param>
        public TokenAcquirerTokenCredential(ITokenAcquirer tokenAcquirer)
        {
            _tokenAcquirer = tokenAcquirer;
        }

        /// <inheritdoc/>
        public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken)
        {
            AcquireTokenResult result = _tokenAcquirer.GetTokenForUserAsync(requestContext.Scopes, cancellationToken: cancellationToken)
                .GetAwaiter()
                .GetResult();
            return new AccessToken(result.AccessToken!, result.ExpiresOn);
        }

        /// <inheritdoc/>
        public override async ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken)
        {
            AcquireTokenResult result = await _tokenAcquirer.GetTokenForUserAsync(requestContext.Scopes, cancellationToken: cancellationToken).ConfigureAwait(false);
            return new AccessToken(result.AccessToken!, result.ExpiresOn);
        }
    }
}
