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
        /// <param name="serviceProvider">
        /// The <see cref="IServiceProvider"/> to resolve dependencies from.
        /// </param>
        /// <param name="serviceName">
        /// Name of the service describing the downstream web API. Used to
        /// retrieve the appropriate config section.
        /// </param>
        /// <returns>
        /// The <see cref="DelegatingHandler"/>.
        /// </returns>
        DelegatingHandler CreateAppHandler(IServiceProvider serviceProvider, string? serviceName);

        /// <summary>
        /// Creates an instance of a <see cref="DelegatingHandler"/> that adds
        /// an authorization header with a token on behalf of the current user.
        /// </summary>
        /// <param name="serviceProvider"></param>
        /// The <see cref="IServiceProvider"/> to resolve dependencies from.
        /// <param name="serviceName">
        /// Name of the service describing the downstream web API. Used to
        /// retrieve the appropriate config section.
        /// </param>
        /// <returns>
        /// The <see cref="DelegatingHandler"/>.
        /// </returns>
        DelegatingHandler CreateUserHandler(IServiceProvider serviceProvider, string? serviceName);
    }

    /// <summary>
    /// The default implementation of <see cref="IMicrosoftIdentityAuthenticationDelegatingHandlerFactory"/>.
    /// </summary>
    public class MicrosoftIdentityAuthenticationDelegatingHandlerFactory : IMicrosoftIdentityAuthenticationDelegatingHandlerFactory
    {
        /// <inheritdoc/>
        public DelegatingHandler CreateAppHandler(IServiceProvider serviceProvider, string? serviceName)
        {
            return new MicrosoftIdentityAppAuthenticationMessageHandler(
                serviceProvider.GetRequiredService<ITokenAcquisition>(),
                serviceProvider.GetRequiredService<IOptionsMonitor<MicrosoftIdentityAuthenticationMessageHandlerOptions>>(),
                serviceName);
        }

        /// <inheritdoc/>
        public DelegatingHandler CreateUserHandler(IServiceProvider serviceProvider, string? serviceName)
        {
            return new MicrosoftIdentityUserAuthenticationMessageHandler(
                serviceProvider.GetRequiredService<ITokenAcquisition>(),
                serviceProvider.GetRequiredService<IOptionsMonitor<MicrosoftIdentityAuthenticationMessageHandlerOptions>>(),
                serviceProvider.GetRequiredService<IOptionsMonitor<MicrosoftIdentityOptions>>(),
                serviceName);
        }
    }
}
