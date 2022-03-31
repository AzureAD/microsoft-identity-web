// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Net.Http;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Interface to a class that provides the <see cref="DelegatingHandler"/> that
    /// adds an authorization header with a token for the application.
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
}
