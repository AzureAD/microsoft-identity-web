// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Implements an <see cref="IAuthorizationRequirement"/>
    /// which requires at least one instance of the specified claim type, and, if allowed values are specified,
    /// the claim value must be any of the allowed values.
    /// </summary>
    public class ScopeOrAppPermissionAuthorizationRequirement : IAuthorizationRequirement
    {
        /// <summary>
        /// Creates a new instance of <see cref="ScopeAuthorizationRequirement"/>.
        /// </summary>
        /// <param name="scopeAllowedValues">The optional list of scope values.</param>
        /// <param name="appPermissionAllowedValues"></param>
        public ScopeOrAppPermissionAuthorizationRequirement(
            IEnumerable<string>? scopeAllowedValues = null,
            IEnumerable<string>? appPermissionAllowedValues = null)
        {
            ScopeAllowedValues = scopeAllowedValues;
            AppPermissionAllowedValues = appPermissionAllowedValues;
        }

        /// <summary>
        /// Gets the optional list of scope values.
        /// </summary>
        public IEnumerable<string>? ScopeAllowedValues { get; }

        /// <summary>
        /// Gets the optional list of app permission values.
        /// </summary>
        public IEnumerable<string>? AppPermissionAllowedValues { get; }

        /// <summary>
        /// Gets the optional list of scope values from configuration.
        /// </summary>
        public string? RequiredScopesConfigurationKey { get; set; }

        /// <summary>
        /// Gets the optional list of app permission values from configuration.
        /// </summary>
        public string? RequiredAppPermissionsConfigurationKey { get; set; }

        /// <inheritdoc />
        public override string ToString()
        {
            string value;

            if (ScopeAllowedValues == null && AppPermissionAllowedValues == null)
            {
                value = string.Empty;
            }
            else if (ScopeAllowedValues != null && AppPermissionAllowedValues != null)
            {
                value = $" and `{ClaimConstants.Scp}` or `{ClaimConstants.Scope}` is one of the following values: ({string.Join("|", ScopeAllowedValues)}) or " +
                    $"TokenValidationParameters.RoleClaimType is one of th following values:  ({string.Join("|", AppPermissionAllowedValues)})";
            }
            else if (ScopeAllowedValues != null)
            {
                value = $" and `{ClaimConstants.Scp}` or `{ClaimConstants.Scope}` is one of the following values: ({string.Join("|", ScopeAllowedValues)})";
            }
            else
            {
                value = $"TokenValidationParameters.RoleClaimType is one of th following values:  ({string.Join("|", AppPermissionAllowedValues)})";
            }

            return $"{nameof(ScopeOrAppPermissionAuthorizationRequirement)}:Scope/AppPermission={value}";
        }
    }
}
