// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Interface to a class that provides the <see cref="DelegatingHandler"/> that
    /// addes an authorization header with a token for the application.
    /// </summary>
    public interface IMicrosoftIdentityAuthenticationDelegatingHandlerFactory
    {
        /// <summary>
        /// Creates an instance of a <see cref="DelegatingHandler"/> that adds
        /// an authorization header with a token for an application.
        /// </summary>
        /// <param name="serviceName">
        /// Name of the service describing the downstream web API. Used to
        /// retrieve the appropriate config section.
        /// </param>
        /// <returns>
        /// The <see cref="DelegatingHandler"/>.
        /// </returns>
        DelegatingHandler CreateAppHandler(string? serviceName);

        /// <summary>
        /// Creates an instance of a <see cref="DelegatingHandler"/> that adds
        /// an authorization header with a token on behalf of the current user.
        /// </summary>
        /// <param name="serviceName">
        /// Name of the service describing the downstream web API. Used to
        /// retrieve the appropriate config section.
        /// </param>
        /// <returns>
        /// The <see cref="DelegatingHandler"/>.
        /// </returns>
        DelegatingHandler CreateUserHandler(string? serviceName);
    }

    /// <summary>
    /// The default implementation of <see cref="IMicrosoftIdentityAuthenticationDelegatingHandlerFactory"/>.
    /// </summary>
    public class MicrosoftIdentityAuthenticationDelegatingHandlerFactory : IMicrosoftIdentityAuthenticationDelegatingHandlerFactory
    {
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// Creates a new instance of <see cref="MicrosoftIdentityAuthenticationDelegatingHandlerFactory"/>.
        /// </summary>
        /// <param name="serviceProvider">
        /// The <see cref="IServiceProvider"/> to resolve dependencies from.
        /// </param>
        public MicrosoftIdentityAuthenticationDelegatingHandlerFactory(IServiceProvider serviceProvider)
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
