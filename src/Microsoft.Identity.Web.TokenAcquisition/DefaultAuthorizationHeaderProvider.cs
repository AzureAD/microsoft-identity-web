// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Abstractions;

namespace Microsoft.Identity.Web
{
    /*
     * Used by Microsoft.Identity.Web
     * Any changes to this member (including removal) can cause runtime failures.
     * Treat as a public member.
     */
    internal sealed class DefaultAuthorizationHeaderProvider :
        IAuthorizationHeaderProvider,
        IAuthorizationHeaderProvider2,
        IBoundAuthorizationHeaderProvider
    {
        private static readonly object s_boxedTrue = true;

        private readonly ITokenAcquisition _tokenAcquisition;

        private const string TokenBindingProtocolScheme = "MTLS_POP";
        private const string TokenBindingParameterName = "IsTokenBinding";

        public DefaultAuthorizationHeaderProvider(ITokenAcquisition tokenAcquisition)
        {
            _tokenAcquisition = tokenAcquisition;
        }

        // ---------------------------------------------------------------------
        // IAuthorizationHeaderProvider (string-returning) — thin adapters over
        // the metadata-rich engine introduced for IAuthorizationHeaderProvider2.
        // ---------------------------------------------------------------------

        /// <inheritdoc/>
        public async Task<string> CreateAuthorizationHeaderForUserAsync(
            IEnumerable<string> scopes,
            AuthorizationHeaderProviderOptions? downstreamApiOptions = null,
            ClaimsPrincipal? claimsPrincipal = null,
            CancellationToken cancellationToken = default)
        {
            var info = await BuildHeaderInformationAsync(
                scopes,
                downstreamApiOptions,
                claimsPrincipal,
                forceAppToken: false,
                cancellationToken).ConfigureAwait(false);
            return info.AuthorizationHeaderValue!;
        }

        /// <inheritdoc/>
        public async Task<string> CreateAuthorizationHeaderForAppAsync(
            string scopes,
            AuthorizationHeaderProviderOptions? downstreamApiOptions = null,
            CancellationToken cancellationToken = default)
        {
            var info = await BuildHeaderInformationAsync(
                new[] { scopes },
                downstreamApiOptions,
                claimsPrincipal: null,
                forceAppToken: true,
                cancellationToken).ConfigureAwait(false);
            return info.AuthorizationHeaderValue!;
        }

        /// <inheritdoc/>
        public async Task<string> CreateAuthorizationHeaderAsync(
            IEnumerable<string> scopes,
            AuthorizationHeaderProviderOptions? downstreamApiOptions = null,
            ClaimsPrincipal? claimsPrincipal = null,
            CancellationToken cancellationToken = default)
        {
            var info = await BuildHeaderInformationAsync(
                scopes,
                downstreamApiOptions,
                claimsPrincipal,
                forceAppToken: IsAppTokenRequest(downstreamApiOptions),
                cancellationToken).ConfigureAwait(false);
            return info.AuthorizationHeaderValue!;
        }

        // ---------------------------------------------------------------------
        // IAuthorizationHeaderProvider2 (Abstractions 12.3.0+) — preferred surface.
        // Returns the full AuthorizationHeaderInformation (header value, binding
        // certificate, metadata) wrapped in an OperationResult.
        // ---------------------------------------------------------------------

        /// <inheritdoc/>
        public async Task<OperationResult<AuthorizationHeaderInformation, AuthorizationHeaderError>> CreateAuthorizationHeaderInformationForUserAsync(
            IEnumerable<string> scopes,
            AuthorizationHeaderProviderOptions? authorizationHeaderProviderOptions = null,
            ClaimsPrincipal? claimsPrincipal = default,
            CancellationToken cancellationToken = default)
        {
            var info = await BuildHeaderInformationAsync(
                scopes,
                authorizationHeaderProviderOptions,
                claimsPrincipal,
                forceAppToken: false,
                cancellationToken).ConfigureAwait(false);
            return new OperationResult<AuthorizationHeaderInformation, AuthorizationHeaderError>(info);
        }

        /// <inheritdoc/>
        public async Task<OperationResult<AuthorizationHeaderInformation, AuthorizationHeaderError>> CreateAuthorizationHeaderInformationForAppAsync(
            string scopes,
            AuthorizationHeaderProviderOptions? downstreamApiOptions = null,
            CancellationToken cancellationToken = default)
        {
            var info = await BuildHeaderInformationAsync(
                new[] { scopes },
                downstreamApiOptions,
                claimsPrincipal: null,
                forceAppToken: true,
                cancellationToken).ConfigureAwait(false);
            return new OperationResult<AuthorizationHeaderInformation, AuthorizationHeaderError>(info);
        }

        /// <inheritdoc/>
        public async Task<OperationResult<AuthorizationHeaderInformation, AuthorizationHeaderError>> CreateAuthorizationHeaderInformationAsync(
            IEnumerable<string> scopes,
            AuthorizationHeaderProviderOptions? options = null,
            ClaimsPrincipal? claimsPrincipal = null,
            CancellationToken cancellationToken = default)
        {
            var info = await BuildHeaderInformationAsync(
                scopes,
                options,
                claimsPrincipal,
                forceAppToken: IsAppTokenRequest(options),
                cancellationToken).ConfigureAwait(false);
            return new OperationResult<AuthorizationHeaderInformation, AuthorizationHeaderError>(info);
        }

        // ---------------------------------------------------------------------
        // IBoundAuthorizationHeaderProvider — kept for source/binary compat.
        // New code should call IAuthorizationHeaderProvider2 instead. The body
        // is now a thin adapter over the same engine so behavior is identical.
        // ---------------------------------------------------------------------

        /// <inheritdoc/>
        /// <remarks>
        /// Retained for backward compatibility. Prefer
        /// <see cref="IAuthorizationHeaderProvider2.CreateAuthorizationHeaderInformationAsync"/>
        /// for new code; both paths share the same implementation.
        /// </remarks>
        public async Task<OperationResult<AuthorizationHeaderInformation, AuthorizationHeaderError>> CreateBoundAuthorizationHeaderAsync(
            DownstreamApiOptions downstreamApiOptions,
            ClaimsPrincipal? claimsPrincipal = null,
            CancellationToken cancellationToken = default)
        {
            var info = await BuildHeaderInformationAsync(
                downstreamApiOptions?.Scopes ?? Enumerable.Empty<string>(),
                downstreamApiOptions,
                claimsPrincipal,
                forceAppToken: IsAppTokenRequest(downstreamApiOptions),
                cancellationToken).ConfigureAwait(false);
            return new OperationResult<AuthorizationHeaderInformation, AuthorizationHeaderError>(info);
        }

        // ---------------------------------------------------------------------
        // Engine: single code path used by every public method above.
        // ---------------------------------------------------------------------

        private async Task<AuthorizationHeaderInformation> BuildHeaderInformationAsync(
            IEnumerable<string> scopes,
            AuthorizationHeaderProviderOptions? options,
            ClaimsPrincipal? claimsPrincipal,
            bool forceAppToken,
            CancellationToken cancellationToken)
        {
            bool isTokenBinding = string.Equals(options?.ProtocolScheme, TokenBindingProtocolScheme, StringComparison.OrdinalIgnoreCase);

            // Token binding (mTLS PoP) currently supports app tokens only.
            if (isTokenBinding && !forceAppToken)
            {
                throw new ArgumentException(
                    IDWebErrorMessage.TokenBindingRequiresEnabledAppTokenAcquisition,
                    nameof(options.RequestAppToken));
            }

            var newTokenAcquisitionOptions = CreateTokenAcquisitionOptionsFromApiOptions(options, cancellationToken);

            // Previously, with the API name we were able to distinguish between app and user token acquisition.
            // This context is missing in the new API, so we rely on AuthorizationHeaderProviderOptions.RequestAppToken
            // (or a ManagedIdentity binding) to switch into the app flow. We cannot rely on ClaimsPrincipal as it can be
            // null for user token acquisition.
            // DevEx Before:
            // await authorizationHeaderProvider.CreateAuthorizationHeaderForAppAsync("https://graph.microsoft.com/.default").ConfigureAwait(false);
            // DevEx with the new API:
            // await authorizationHeaderProvider.CreateAuthorizationHeaderAsync(
            //  new [] { "https://graph.microsoft.com/.default" },
            //  new AuthorizationHeaderProviderOptions { RequestAppToken = true }).ConfigureAwait(false);
            Client.AuthenticationResult result;
            if (forceAppToken)
            {
                result = await _tokenAcquisition.GetAuthenticationResultForAppAsync(
                    scopes.FirstOrDefault()!,
                    options?.AcquireTokenOptions.AuthenticationOptionsName,
                    options?.AcquireTokenOptions.Tenant,
                    newTokenAcquisitionOptions).ConfigureAwait(false);
            }
            else
            {
                result = await _tokenAcquisition.GetAuthenticationResultForUserAsync(
                    scopes,
                    options?.AcquireTokenOptions?.AuthenticationOptionsName,
                    options?.AcquireTokenOptions?.Tenant,
                    options?.AcquireTokenOptions?.UserFlow,
                    claimsPrincipal,
                    newTokenAcquisitionOptions).ConfigureAwait(false);
            }

            UpdateOriginalTokenAcquisitionOptions(options?.AcquireTokenOptions, newTokenAcquisitionOptions);

            return new AuthorizationHeaderInformation
            {
                AuthorizationHeaderValue = result.CreateAuthorizationHeader(),
                BindingCertificate = isTokenBinding ? result.BindingCertificate : null,
                Metadata = AcquireTokenResultFactory.GetMetadata(result),
                AdditionalResponseParameters = result.AdditionalResponseParameters,
            };
        }

        private static bool IsAppTokenRequest(AuthorizationHeaderProviderOptions? options)
        {
            return options != null
                && (options.RequestAppToken || options.AcquireTokenOptions?.ManagedIdentity != null);
        }

        private static TokenAcquisitionOptions CreateTokenAcquisitionOptionsFromApiOptions(
            AuthorizationHeaderProviderOptions? downstreamApiOptions,
            CancellationToken cancellationToken)
        {
            return new TokenAcquisitionOptions()
            {
                AuthenticationOptionsName = downstreamApiOptions?.AcquireTokenOptions.AuthenticationOptionsName,
                CancellationToken = cancellationToken,
                Claims = downstreamApiOptions?.AcquireTokenOptions.Claims,
                CorrelationId = downstreamApiOptions?.AcquireTokenOptions.CorrelationId ?? Guid.Empty,
                ExtraHeadersParameters = downstreamApiOptions?.AcquireTokenOptions.ExtraHeadersParameters,
                ExtraQueryParameters = downstreamApiOptions?.AcquireTokenOptions.ExtraQueryParameters,
                ExtraParameters = GetExtraParameters(downstreamApiOptions),
                ForceRefresh = downstreamApiOptions?.AcquireTokenOptions.ForceRefresh ?? false,
                LongRunningWebApiSessionKey = downstreamApiOptions?.AcquireTokenOptions.LongRunningWebApiSessionKey,
                ManagedIdentity = downstreamApiOptions?.AcquireTokenOptions.ManagedIdentity,
                Tenant = downstreamApiOptions?.AcquireTokenOptions.Tenant,
                UserFlow = downstreamApiOptions?.AcquireTokenOptions.UserFlow,
                PopPublicKey = downstreamApiOptions?.AcquireTokenOptions.PopPublicKey,
                FmiPath = downstreamApiOptions?.AcquireTokenOptions.FmiPath,
            };
        }

        /// <summary>
        /// Since AcquireTokenOptions is recreated, we need to update the original TokenAcquisitionOptions wth the parameters that were
        /// updated in the new TokenAcquisitionOptions.
        /// </summary>
        private void UpdateOriginalTokenAcquisitionOptions(AcquireTokenOptions? acquireTokenOptions, TokenAcquisitionOptions newTokenAcquisitionOptions)
        {
            if (acquireTokenOptions is not null && newTokenAcquisitionOptions is not null)
            {
                acquireTokenOptions.LongRunningWebApiSessionKey = newTokenAcquisitionOptions.LongRunningWebApiSessionKey;
            }
        }

        /// <summary>
        /// Retrieves the collection of extra parameters to be included when acquiring a token, optionally adding
        /// protocol-specific parameters based on the provided options.
        /// </summary>
        /// <param name="downstreamApiOptions">The options used to configure token acquisition.</param>
        /// <returns>A dictionary containing extra parameters to be sent during token acquisition or null.</returns>
        private static IDictionary<string, object>? GetExtraParameters(AuthorizationHeaderProviderOptions? downstreamApiOptions)
        {
            var extraParameters = downstreamApiOptions?.AcquireTokenOptions.ExtraParameters;
            if (string.Equals(downstreamApiOptions?.ProtocolScheme, TokenBindingProtocolScheme, StringComparison.OrdinalIgnoreCase))
            {
                extraParameters ??= new Dictionary<string, object>();
                extraParameters[TokenBindingParameterName] = s_boxedTrue;
            }

            return extraParameters;
        }
    }
}
