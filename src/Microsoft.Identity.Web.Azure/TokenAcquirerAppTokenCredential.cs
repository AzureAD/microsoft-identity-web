// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure.Core;
using Microsoft.Identity.Abstractions;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Azure SDK token credential for App tokens based on the ITokenAcquisition service.
    /// It's recommended to use MicrosoftIdentityTokenCredential. See Readme-Azure.md file.
    /// </summary>
    [Obsolete("Use MicrosoftIdentityTokenCredential (registered via AddMicrosoftIdentityAzureTokenCredential). Set Options.RequestAppToken = true for app tokens. See https://aka.ms/ms-id-web/v3-to-v4", false)]
    public class TokenAcquirerAppTokenCredential : TokenCredential
    {
        private ITokenAcquirer _tokenAcquirer;

        /// <summary>
        /// Constructor from an ITokenAcquisition service.
        /// </summary>
        /// <param name="tokenAcquirer">Token acquisition.</param>
        public TokenAcquirerAppTokenCredential(ITokenAcquirer tokenAcquirer)
        {
            _tokenAcquirer = tokenAcquirer;
        }

        /// <inheritdoc/>
        public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken)
        {
            AcquireTokenResult result = _tokenAcquirer.GetTokenForAppAsync(requestContext.Scopes.First(), cancellationToken: cancellationToken)
                .GetAwaiter()
                .GetResult();
            return new AccessToken(result.AccessToken!, result.ExpiresOn);
        }

        /// <inheritdoc/>
        public override async ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken)
        {
            AcquireTokenResult result = await _tokenAcquirer.GetTokenForAppAsync(requestContext.Scopes.First(), cancellationToken: cancellationToken).ConfigureAwait(false);
            return new AccessToken(result.AccessToken!, result.ExpiresOn);
        }
    }
}
