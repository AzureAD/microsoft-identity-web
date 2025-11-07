// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Abstractions;

namespace Microsoft.Identity.Web
{
    internal sealed class DefaultAuthorizationHeaderBoundProvider : IAuthorizationHeaderProvider, IAuthorizationHeaderBoundProvider
    {
        private readonly IAuthorizationHeaderProvider _authorizationHeaderProvider;

        private readonly ITokenAcquisition _tokenAcquisition;

        /// <summary>
        /// Initializes a new instance of the DefaultAuthorizationHeaderBoundProvider class using the specified
        /// authorization header provider.
        /// </summary>
        /// <param name="authorizationHeaderProvider">The provider used to supply authorization headers for HTTP requests.</param>
        /// <param name="tokenAcquisition">The token acquisition service.</param>
        public DefaultAuthorizationHeaderBoundProvider(
            IAuthorizationHeaderProvider authorizationHeaderProvider,
            ITokenAcquisition tokenAcquisition)
        {
            _authorizationHeaderProvider = authorizationHeaderProvider;
            _tokenAcquisition = tokenAcquisition;
        }

        /// <inheritdoc/>
        public async Task<OperationResult<AuthorizationHeaderInformation, AuthorizationHeaderError>> CreateAuthorizationHeaderAsync(
            DownstreamApiOptions downstreamApiOptions,
            ClaimsPrincipal? claimsPrincipal = null,
            CancellationToken cancellationToken = default)
        {
            var newTokenAcquisitionOptions = TokenAcquisitionOptionsHelper.CreateTokenAcquisitionOptionsFromApiOptions(downstreamApiOptions, cancellationToken);

            // Token binding flow currently supports only app tokens.
            var tokenAcquisitionResult = await _tokenAcquisition.GetAuthenticationResultForAppAsync(
                downstreamApiOptions.Scopes?.FirstOrDefault() ?? string.Empty,
                downstreamApiOptions?.AcquireTokenOptions.AuthenticationOptionsName,
                downstreamApiOptions?.AcquireTokenOptions.Tenant,
                newTokenAcquisitionOptions).ConfigureAwait(false);

            TokenAcquisitionOptionsHelper.UpdateOriginalTokenAcquisitionOptions(downstreamApiOptions?.AcquireTokenOptions, newTokenAcquisitionOptions);

            var authorizationHeader = tokenAcquisitionResult.CreateAuthorizationHeader();
            var authorizationHeaderInformation = new AuthorizationHeaderInformation()
            {
                AuthorizationHeaderValue = authorizationHeader,
                BindingCertificate = tokenAcquisitionResult.BindingCertificate
            };

            return new(authorizationHeaderInformation);
        }

        /// <inheritdoc/>
        public async Task<string> CreateAuthorizationHeaderAsync(
            IEnumerable<string> scopes,
            AuthorizationHeaderProviderOptions? options = null,
            ClaimsPrincipal? claimsPrincipal = null,
            CancellationToken cancellationToken = default)
        {
            return await _authorizationHeaderProvider.CreateAuthorizationHeaderAsync(scopes, options, claimsPrincipal, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<string> CreateAuthorizationHeaderForAppAsync(
            string scopes,
            AuthorizationHeaderProviderOptions? downstreamApiOptions = null,
            CancellationToken cancellationToken = default)
        {
            return await _authorizationHeaderProvider.CreateAuthorizationHeaderForAppAsync(scopes, downstreamApiOptions, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<string> CreateAuthorizationHeaderForUserAsync(
            IEnumerable<string> scopes,
            AuthorizationHeaderProviderOptions? authorizationHeaderProviderOptions = null,
            ClaimsPrincipal? claimsPrincipal = null,
            CancellationToken cancellationToken = default)
        {
            return await _authorizationHeaderProvider.CreateAuthorizationHeaderForUserAsync(scopes, authorizationHeaderProviderOptions, claimsPrincipal, cancellationToken).ConfigureAwait(false);
        }
    }
}
