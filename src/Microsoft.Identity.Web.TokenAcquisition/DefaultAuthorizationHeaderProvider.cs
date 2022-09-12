// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Identity.Web
{
    internal class DefaultAuthorizationHeaderProvider : IAuthorizationHeaderProvider
    {
        private readonly ITokenAcquisition _tokenAcquisition;

        public DefaultAuthorizationHeaderProvider(ITokenAcquisition tokenAcquisition)
        {
            _tokenAcquisition = tokenAcquisition;
        }

        /// <inheritdoc/>
        public async Task<string> CreateAuthorizationHeaderForUserAsync(IEnumerable<string> scopes, DownstreamRestApiOptions? downstreamApiOptions = null, ClaimsPrincipal? claimsPrincipal = null, CancellationToken cancellationToken = default)
        {
            var result = await _tokenAcquisition.GetAuthenticationResultForUserAsync(
                scopes,
                downstreamApiOptions?.TokenAcquirerOptions.AuthenticationScheme,
                downstreamApiOptions?.TokenAcquirerOptions.Tenant,
                downstreamApiOptions?.TokenAcquirerOptions.UserFlow,
                claimsPrincipal,
                CreateTokenAcquisitionOptionsFromRestApiOptions(downstreamApiOptions, cancellationToken)).ConfigureAwait(false);
            return result.CreateAuthorizationHeader();
        }

        /// <inheritdoc/>
        public async Task<string> CreateAuthorizationHeaderForAppAsync(string scopes, DownstreamRestApiOptions? downstreamApiOptions = null, CancellationToken cancellationToken = default)
        {
            var result = await _tokenAcquisition.GetAuthenticationResultForAppAsync(
                scopes,
                downstreamApiOptions?.TokenAcquirerOptions.Tenant,
                downstreamApiOptions?.TokenAcquirerOptions.UserFlow,
                CreateTokenAcquisitionOptionsFromRestApiOptions(downstreamApiOptions, cancellationToken)).ConfigureAwait(false);
            return result.CreateAuthorizationHeader();
        }

        private static TokenAcquisitionOptions CreateTokenAcquisitionOptionsFromRestApiOptions(DownstreamRestApiOptions? downstreamApiOptions, CancellationToken cancellationToken)
        {
            return new TokenAcquisitionOptions()
            {
                AuthenticationScheme = downstreamApiOptions?.TokenAcquirerOptions.AuthenticationScheme,
                CancellationToken = cancellationToken,
                Claims = downstreamApiOptions?.TokenAcquirerOptions.Claims,
                CorrelationId = downstreamApiOptions?.TokenAcquirerOptions.CorrelationId ?? default(Guid),
                ExtraQueryParameters = downstreamApiOptions?.TokenAcquirerOptions.ExtraQueryParameters,
                ForceRefresh = downstreamApiOptions?.TokenAcquirerOptions.ForceRefresh ?? false,
                LongRunningWebApiSessionKey = downstreamApiOptions?.TokenAcquirerOptions.LongRunningWebApiSessionKey,
                Tenant = downstreamApiOptions?.TokenAcquirerOptions.Tenant,
                UserFlow = downstreamApiOptions?.TokenAcquirerOptions.UserFlow,
                PopPublicKey = downstreamApiOptions?.TokenAcquirerOptions.PopPublicKey,
            };
        }
    }
}
