// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Abstractions;

namespace Microsoft.Identity.Web
{
    internal sealed class DefaultAuthorizationHeaderProvider : IAuthorizationHeaderProvider
    {
        private readonly ITokenAcquisition _tokenAcquisition;

        public DefaultAuthorizationHeaderProvider(ITokenAcquisition tokenAcquisition)
        {
            _tokenAcquisition = tokenAcquisition;
        }

        /// <inheritdoc/>
        public async Task<string> CreateAuthorizationHeaderForUserAsync(
            IEnumerable<string> scopes,
            AuthorizationHeaderProviderOptions? downstreamApiOptions = null,
            ClaimsPrincipal? claimsPrincipal = null,
            CancellationToken cancellationToken = default)
        {
            var result = await _tokenAcquisition.GetAuthenticationResultForUserAsync(
                scopes,
                downstreamApiOptions?.TokenAcquirerOptions.AuthenticationOptionsName,
                downstreamApiOptions?.TokenAcquirerOptions.Tenant,
                downstreamApiOptions?.TokenAcquirerOptions.UserFlow,
                claimsPrincipal,
                CreateTokenAcquisitionOptionsFromRestApiOptions(downstreamApiOptions, cancellationToken)).ConfigureAwait(false);
            return result.CreateAuthorizationHeader();
        }

        /// <inheritdoc/>
        public async Task<string> CreateAuthorizationHeaderForAppAsync(string scopes, AuthorizationHeaderProviderOptions? downstreamApiOptions = null, CancellationToken cancellationToken = default)
        {
            var result = await _tokenAcquisition.GetAuthenticationResultForAppAsync(
                scopes,
                downstreamApiOptions?.TokenAcquirerOptions.AuthenticationOptionsName,
                downstreamApiOptions?.TokenAcquirerOptions.Tenant,
                CreateTokenAcquisitionOptionsFromRestApiOptions(downstreamApiOptions, cancellationToken)).ConfigureAwait(false);
            return result.CreateAuthorizationHeader();
        }

        private static TokenAcquisitionOptions CreateTokenAcquisitionOptionsFromRestApiOptions(AuthorizationHeaderProviderOptions? downstreamApiOptions, CancellationToken cancellationToken)
        {
            return new TokenAcquisitionOptions()
            {
                AuthenticationOptionsName = downstreamApiOptions?.TokenAcquirerOptions.AuthenticationOptionsName,
                CancellationToken = cancellationToken,
                Claims = downstreamApiOptions?.TokenAcquirerOptions.Claims,
                CorrelationId = downstreamApiOptions?.TokenAcquirerOptions.CorrelationId ?? Guid.Empty,
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
