// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace Microsoft.Identity.Abstractions
{
    /// <summary>
    /// Token acquirer factory.
    /// </summary>
    public interface ITokenAcquirerFactory
    {
        /// <summary>
        /// Get a token acquirer given an authority, client ID, client credentials and an optional Azure region.
        /// </summary>
        /// <param name="authority">Authority from which to acquire the security tokens.</param>
        /// <param name="clientId">Client ID of the application registered to get the tokens.</param>
        /// <param name="clientCredentials">Client Credentials (client certificate, ...) used to
        /// prove the identity of the application registered to get the tokens.</param>
        /// <param name="region">Optional Azure Region.</param>
        /// <returns>An instance of <see cref="ITokenAcquirer"/> that will enable token acquisition.</returns>
        ITokenAcquirer GetTokenAcquirer(string authority, string clientId, IEnumerable<CredentialDescription> clientCredentials, string? region = "TryAutoDetect");

        /// <summary>
        /// Get a token acquirer given a set of application identity options.
        /// </summary>
        /// <param name="applicationIdentityOptions">Application configuration. Can be an
        /// <see cref="MicrosoftAuthenticationOptions"/>.</param>
        /// <returns>An instance of <see cref="ITokenAcquirer"/> that will enable token acquisition.</returns>
        ITokenAcquirer GetTokenAcquirer(AuthenticationOptions applicationIdentityOptions);

        /// <summary>
        /// Get a token acquirer for a specific <see cref="MicrosoftAuthenticationOptions"/> named configuration
        /// (for instance an ASP.NET Core authentication scheme).
        /// </summary>
        /// <param name="optionName">Name of the Application configuration as defined by the configuration.
        /// For instance in ASP.NET Core it would be the authentication scheme.</param>
        /// <returns>An instance of <see cref="ITokenAcquirer"/> that will enable token acquisition.</returns>
        ITokenAcquirer GetTokenAcquirer(string optionName = "");
    }
}
