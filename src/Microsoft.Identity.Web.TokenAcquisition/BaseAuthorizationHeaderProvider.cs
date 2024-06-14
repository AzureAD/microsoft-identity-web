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
    public class BaseAuthorizationHeaderProvider : IAuthorizationHeaderProvider, IAuthorizationHeaderProviderExtension
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
            _headerProviderExtension = _headerProvider as IAuthorizationHeaderProviderExtension 
                ?? throw new InvalidOperationException("The default implementation of IAuthorizationHeaderProvider does not implement IAuthorizationHeaderProviderExtension.");

        }

        private readonly IAuthorizationHeaderProvider _headerProvider;
        private readonly IAuthorizationHeaderProviderExtension _headerProviderExtension;

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
            RequestContext requestContext,
            IEnumerable<string> scopes, 
            AuthorizationHeaderProviderOptions? authorizationHeaderProviderOptions = null, 
            ClaimsPrincipal? claimsPrincipal = null, 
            CancellationToken cancellationToken = default)
        {
            return _headerProviderExtension.CreateAuthorizationHeaderAsync(requestContext, scopes, authorizationHeaderProviderOptions, claimsPrincipal, cancellationToken);
        }
    }
}
