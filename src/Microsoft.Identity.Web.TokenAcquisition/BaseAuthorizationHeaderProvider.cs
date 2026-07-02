// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Abstractions;

namespace Microsoft.Identity.Web.Extensibility
{
    /// <summary>
    /// Base class for custom implementations of <see cref="IAuthorizationHeaderProvider"/> that
    /// would still want to leverage the default implementation for the bearer and Pop protocols.
    /// </summary>
    /// <remarks>
    /// Also implements <see cref="IAuthorizationHeaderProvider2"/> (added in
    /// Microsoft.Identity.Abstractions 12.3.0) so subclasses automatically expose the
    /// metadata-rich header-creation surface without needing to opt in. Override the
    /// <c>CreateAuthorizationHeaderInformation*</c> virtuals to customize that path.
    /// </remarks>
    public class BaseAuthorizationHeaderProvider : IAuthorizationHeaderProvider, IAuthorizationHeaderProvider2
    {
        /// <summary>
        /// Constructor from a service provider
        /// </summary>
        /// <param name="serviceProvider"></param>
        public BaseAuthorizationHeaderProvider(IServiceProvider serviceProvider)
        {
            // We, intentionally, use a locator pattern here, because we don't want to expose ITokenAcquisition
            // in the public API as it's going to be deprecated in future versions of IdWeb. Here this
            // is an implementation detail.
            var _tokenAcquisition = serviceProvider.GetRequiredService<ITokenAcquisition>();
            _headerProvider = new DefaultAuthorizationHeaderProvider(_tokenAcquisition);
        }

        private readonly DefaultAuthorizationHeaderProvider _headerProvider;

        /// <inheritdoc/>
        public virtual Task<string> CreateAuthorizationHeaderForUserAsync(IEnumerable<string> scopes, AuthorizationHeaderProviderOptions? authorizationHeaderProviderOptions = null, ClaimsPrincipal? claimsPrincipal = null, CancellationToken cancellationToken = default)
        {
            return _headerProvider.CreateAuthorizationHeaderForUserAsync(scopes, authorizationHeaderProviderOptions, claimsPrincipal, cancellationToken);
        }

        /// <inheritdoc/>
        public virtual Task<string> CreateAuthorizationHeaderForAppAsync(string scopes, AuthorizationHeaderProviderOptions? downstreamApiOptions = null, CancellationToken cancellationToken = default)
        {
            return _headerProvider.CreateAuthorizationHeaderForAppAsync(scopes, downstreamApiOptions, cancellationToken);
        }

        /// <inheritdoc/>
        public virtual Task<string> CreateAuthorizationHeaderAsync(
            IEnumerable<string> scopes,
            AuthorizationHeaderProviderOptions? authorizationHeaderProviderOptions = null,
            ClaimsPrincipal? claimsPrincipal = null,
            CancellationToken cancellationToken = default)
        {
            return _headerProvider.CreateAuthorizationHeaderAsync(
                scopes,
                authorizationHeaderProviderOptions,
                claimsPrincipal,
                cancellationToken);
        }

        /// <inheritdoc/>
        /// <remarks>
        /// Default implementation delegates to <see cref="DefaultAuthorizationHeaderProvider"/>; override to inject
        /// custom logic while still receiving the binding certificate and token-acquisition metadata for free.
        /// </remarks>
        public virtual Task<OperationResult<AuthorizationHeaderInformation, AuthorizationHeaderError>> CreateAuthorizationHeaderInformationForUserAsync(
            IEnumerable<string> scopes,
            AuthorizationHeaderProviderOptions? authorizationHeaderProviderOptions = null,
            ClaimsPrincipal? claimsPrincipal = default,
            CancellationToken cancellationToken = default)
        {
            return _headerProvider.CreateAuthorizationHeaderInformationForUserAsync(
                scopes,
                authorizationHeaderProviderOptions,
                claimsPrincipal,
                cancellationToken);
        }

        /// <inheritdoc/>
        public virtual Task<OperationResult<AuthorizationHeaderInformation, AuthorizationHeaderError>> CreateAuthorizationHeaderInformationForAppAsync(
            string scopes,
            AuthorizationHeaderProviderOptions? downstreamApiOptions = null,
            CancellationToken cancellationToken = default)
        {
            return _headerProvider.CreateAuthorizationHeaderInformationForAppAsync(
                scopes,
                downstreamApiOptions,
                cancellationToken);
        }

        /// <inheritdoc/>
        public virtual Task<OperationResult<AuthorizationHeaderInformation, AuthorizationHeaderError>> CreateAuthorizationHeaderInformationAsync(
            IEnumerable<string> scopes,
            AuthorizationHeaderProviderOptions? options = null,
            ClaimsPrincipal? claimsPrincipal = null,
            CancellationToken cancellationToken = default)
        {
            return _headerProvider.CreateAuthorizationHeaderInformationAsync(
                scopes,
                options,
                claimsPrincipal,
                cancellationToken);
        }
    }
}
