// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Identity.Abstractions;

namespace Microsoft.Identity.Web
{
    internal sealed class TokenAcquirer : ITokenAcquirer
    {
        private readonly ITokenAcquisition _tokenAcquisition;
        private readonly string? _authenticationScheme;

        public TokenAcquirer(ITokenAcquisition tokenAcquisition, string? authenticationScheme)
        {
            _tokenAcquisition = tokenAcquisition;
            _authenticationScheme = authenticationScheme;
        }

        async Task<AcquireTokenResult> ITokenAcquirer.GetTokenForUserAsync(
            IEnumerable<string> scopes,
            AcquireTokenOptions? tokenAcquisitionOptions,
            ClaimsPrincipal? user,
            CancellationToken cancellationToken)
        {
            string? authenticationScheme = tokenAcquisitionOptions?.AuthenticationOptionsName ?? _authenticationScheme;

            var result = await _tokenAcquisition.GetAuthenticationResultForUserAsync(
                scopes,
                authenticationScheme,
                tokenAcquisitionOptions?.Tenant,
                tokenAcquisitionOptions?.UserFlow,
                user,
                GetEffectiveTokenAcquisitionOptions(tokenAcquisitionOptions, authenticationScheme, cancellationToken)
                ).ConfigureAwait(false);

            return new AcquireTokenResult(
                result.AccessToken,
                result.ExpiresOn,
                result.TenantId,
                result.IdToken,
                result.Scopes,
                result.CorrelationId,
                result.TokenType);
        }

        async Task<AcquireTokenResult> ITokenAcquirer.GetTokenForAppAsync(string scope, AcquireTokenOptions? tokenAcquisitionOptions, CancellationToken cancellationToken)
        {
            string? authenticationScheme = tokenAcquisitionOptions?.AuthenticationOptionsName ?? _authenticationScheme;

            var result = await _tokenAcquisition.GetAuthenticationResultForAppAsync(
                scope,
                authenticationScheme,
                tokenAcquisitionOptions?.Tenant,
                GetEffectiveTokenAcquisitionOptions(tokenAcquisitionOptions, authenticationScheme, cancellationToken)
                ).ConfigureAwait(false);

            return new AcquireTokenResult(
                result.AccessToken,
                result.ExpiresOn,
                result.TenantId,
                result.IdToken,
                result.Scopes,
                result.CorrelationId,
                result.TokenType);
        }

        private static TokenAcquisitionOptions? GetEffectiveTokenAcquisitionOptions(AcquireTokenOptions? tokenAcquisitionOptions, string? authenticationScheme, CancellationToken cancellationToken)
        {
            return (tokenAcquisitionOptions == null) ? null : new TokenAcquisitionOptions
            {
                AuthenticationOptionsName = authenticationScheme,
                CancellationToken = cancellationToken,
                Claims = tokenAcquisitionOptions!.Claims,
                CorrelationId = tokenAcquisitionOptions!.CorrelationId,
                ExtraQueryParameters = tokenAcquisitionOptions.ExtraQueryParameters,
                ForceRefresh = tokenAcquisitionOptions.ForceRefresh,
                LongRunningWebApiSessionKey = tokenAcquisitionOptions.LongRunningWebApiSessionKey,
                Tenant = tokenAcquisitionOptions.Tenant,
                UserFlow = tokenAcquisitionOptions.UserFlow,
                PopPublicKey = tokenAcquisitionOptions.PopPublicKey,
                PopClaim = tokenAcquisitionOptions.PopClaim,
                ExtraParameters = tokenAcquisitionOptions.ExtraParameters,
                FmiPath = tokenAcquisitionOptions.FmiPath
            };
        }
    }
}
