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
        private readonly bool _hasExplicitApplicationOptions;
        private readonly AgentAcquisitionContext? _agentContext;

        public TokenAcquirer(ITokenAcquisition tokenAcquisition, string? authenticationScheme)
            : this(tokenAcquisition, authenticationScheme, false)
        {
        }

        internal TokenAcquirer(ITokenAcquisition tokenAcquisition, string? authenticationScheme, bool hasExplicitApplicationOptions, AgentAcquisitionContext? agentContext = null)
        {
            _tokenAcquisition = tokenAcquisition;
            _authenticationScheme = authenticationScheme;
            _hasExplicitApplicationOptions = hasExplicitApplicationOptions;
            _agentContext = agentContext;
        }

        async Task<AcquireTokenResult> ITokenAcquirer.GetTokenForUserAsync(
            IEnumerable<string> scopes,
            AcquireTokenOptions? tokenAcquisitionOptions,
            ClaimsPrincipal? user,
            CancellationToken cancellationToken)
        {
            string? authenticationScheme = GetAuthenticationScheme(tokenAcquisitionOptions);

            var effectiveOptions = GetEffectiveTokenAcquisitionOptions(tokenAcquisitionOptions, authenticationScheme, cancellationToken);
            var result = await _tokenAcquisition.GetAuthenticationResultForUserAsync(
                scopes,
                authenticationScheme,
                tokenAcquisitionOptions?.Tenant,
                tokenAcquisitionOptions?.UserFlow,
                user,
                effectiveOptions
                ).ConfigureAwait(false);

            // Propagate LongRunningWebApiSessionKey (possibly auto-generated) back to the caller
            if (tokenAcquisitionOptions is not null && effectiveOptions is not null
                && !string.IsNullOrEmpty(effectiveOptions.LongRunningWebApiSessionKey))
            {
                tokenAcquisitionOptions.LongRunningWebApiSessionKey = effectiveOptions.LongRunningWebApiSessionKey;
            }

            return AcquireTokenResultFactory.FromMsal(result);
        }

        async Task<AcquireTokenResult> ITokenAcquirer.GetTokenForAppAsync(string scope, AcquireTokenOptions? tokenAcquisitionOptions, CancellationToken cancellationToken)
        {
            string? authenticationScheme = GetAuthenticationScheme(tokenAcquisitionOptions);

            var result = await _tokenAcquisition.GetAuthenticationResultForAppAsync(
                scope,
                authenticationScheme,
                tokenAcquisitionOptions?.Tenant,
                GetEffectiveTokenAcquisitionOptions(tokenAcquisitionOptions, authenticationScheme, cancellationToken)
                ).ConfigureAwait(false);

            return AcquireTokenResultFactory.FromMsal(result);
        }

        private string? GetAuthenticationScheme(AcquireTokenOptions? options) =>
            _hasExplicitApplicationOptions && options?.ExtraParameters?.ContainsKey(Constants.AgentIdentityKey) == true
                ? _authenticationScheme
                : options?.AuthenticationOptionsName ?? _authenticationScheme;

        private TokenAcquisitionOptions? GetEffectiveTokenAcquisitionOptions(AcquireTokenOptions? tokenAcquisitionOptions, string? authenticationScheme, CancellationToken cancellationToken)
        {
            if (tokenAcquisitionOptions is null && _agentContext is null)
            {
                return null;
            }

            tokenAcquisitionOptions ??= new AcquireTokenOptions();
            return new TokenAcquisitionOptions
            {
                AuthenticationOptionsName = authenticationScheme,
                CancellationToken = cancellationToken,
                Claims = tokenAcquisitionOptions.Claims,
                CorrelationId = tokenAcquisitionOptions.CorrelationId,
                ExtraQueryParameters = tokenAcquisitionOptions.ExtraQueryParameters,
                ForceRefresh = tokenAcquisitionOptions.ForceRefresh,
                LongRunningWebApiSessionKey = tokenAcquisitionOptions.LongRunningWebApiSessionKey,
                Tenant = tokenAcquisitionOptions.Tenant,
                UserFlow = tokenAcquisitionOptions.UserFlow,
                PopPublicKey = tokenAcquisitionOptions.PopPublicKey,
                PopClaim = tokenAcquisitionOptions.PopClaim,
                ExtraParameters = _agentContext is null
                    ? tokenAcquisitionOptions.ExtraParameters
                    : new Dictionary<string, object>(tokenAcquisitionOptions.ExtraParameters ?? new Dictionary<string, object>())
                    {
                        [Constants.AgentAcquisitionContext] = _agentContext,
                    },
                ManagedIdentity = tokenAcquisitionOptions.ManagedIdentity,
                FmiPath = tokenAcquisitionOptions.FmiPath
            };
        }
    }
}
