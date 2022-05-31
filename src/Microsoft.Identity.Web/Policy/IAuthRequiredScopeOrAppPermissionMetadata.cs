// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// This is the metadata that describes required auth scopes or app permissions for a given endpoint
    /// in a web API. It's the underlying data structure the requirement <see cref="ScopeAuthorizationRequirement"/> will look for
    /// in order to validate scopes in the scope claims.
    /// </summary>
    public interface IAuthRequiredScopeOrAppPermissionMetadata
    {
        /// <summary>
        /// App permissions accepted by this web API.
        /// App permissions appear in the roles claim of the token.
        /// </summary>
        string[]? AcceptedAppPermission { get; }

        /// <summary>
        /// Fully qualified name of the configuration key containing the required scopes 
        /// or app permissions (separated by spaces).
        /// </summary>
        string? RequiredAppPermissionsConfigurationKey { get; }

        /// <summary>
        /// Scopes accepted by this web API.
        /// </summary>
        string[]? AcceptedScope { get; }

        /// <summary>
        /// Fully qualified name of the configuration key containing the required scopes (separated
        /// by spaces).
        /// </summary>
        string? RequiredScopesConfigurationKey { get; }
    }
}
