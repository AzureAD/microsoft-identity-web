// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// The default implementation of <see cref="IMicrosoftIdentityAuthenticationDelegatingHandlerFactory"/>.
    /// </summary>
    internal class DefaultMicrosoftIdentityAuthenticationDelegatingHandlerFactory : IMicrosoftIdentityAuthenticationDelegatingHandlerFactory
    {
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// Creates a new instance of <see cref="DefaultMicrosoftIdentityAuthenticationDelegatingHandlerFactory"/>.
        /// </summary>
        /// <param name="serviceProvider">
        /// The <see cref="IServiceProvider"/> to resolve dependencies from.
        /// </param>
        public DefaultMicrosoftIdentityAuthenticationDelegatingHandlerFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        /// <inheritdoc/>
        public DelegatingHandler CreateAppHandler(string? serviceName)
        {
            return new MicrosoftIdentityAppAuthenticationMessageHandler(
                _serviceProvider.GetRequiredService<ITokenAcquisition>(),
                _serviceProvider.GetRequiredService<IOptionsMonitor<MicrosoftIdentityAuthenticationMessageHandlerOptions>>(),
                serviceName);
        }

        /// <inheritdoc/>
        public DelegatingHandler CreateUserHandler(string? serviceName)
        {
            return new MicrosoftIdentityUserAuthenticationMessageHandler(
                _serviceProvider.GetRequiredService<ITokenAcquisition>(),
                _serviceProvider.GetRequiredService<IOptionsMonitor<MicrosoftIdentityAuthenticationMessageHandlerOptions>>(),
                _serviceProvider.GetRequiredService<IOptionsMonitor<MicrosoftIdentityOptions>>(),
                serviceName);
        }
    }
}
