// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Extensions for building the RequiredScope policy during application startup.
    /// </summary>
    /// <example>
    /// <code>
    /// services.AddAuthorization(o =>
    /// { o.AddPolicy("Custom",
    ///     policyBuilder =>policyBuilder.RequireScope("access_as_user"));
    /// });
    /// </code>
    /// </example>
    public static class PolicyBuilderExtensions
    {
        /// <summary>
        /// Adds a <see cref="DenyGuestsAuthorizationRequirement"/> to the current instance which requires
        /// that the current user is a member of the tenant.
        /// </summary>
        /// <param name="authorizationPolicyBuilder">Used for building policies during application startup.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public static AuthorizationPolicyBuilder DenyGuests(this AuthorizationPolicyBuilder authorizationPolicyBuilder)
        {
            _ = Throws.IfNull(authorizationPolicyBuilder);

            authorizationPolicyBuilder.Requirements.Add(new DenyGuestsAuthorizationRequirement());

            return authorizationPolicyBuilder;
        }

        /// <summary>
        /// Adds a <see cref="ScopeAuthorizationRequirement"/> to the current instance which requires
        /// that the current user has the specified claim and that the claim value must be one of the allowed values.
        /// </summary>
        /// <param name="authorizationPolicyBuilder">Used for building policies during application startup.</param>
        /// <param name="allowedValues">Values the claim must process one or more of for evaluation to succeed.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public static AuthorizationPolicyBuilder RequireScope(
            this AuthorizationPolicyBuilder authorizationPolicyBuilder,
            params string[] allowedValues)
        {
            _ = Throws.IfNull(authorizationPolicyBuilder);

            return RequireScope(authorizationPolicyBuilder, (IEnumerable<string>)allowedValues);
        }

        /// <summary>
        /// Adds a <see cref="ScopeAuthorizationRequirement"/> to the current instance which requires
        /// that the current user has the specified claim and that the claim value must be one of the allowed values.
        /// </summary>
        /// <param name="authorizationPolicyBuilder">Used for building policies during application startup.</param>
        /// <param name="allowedValues">Values the claim must process one or more of for evaluation to succeed.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public static AuthorizationPolicyBuilder RequireScope(
            this AuthorizationPolicyBuilder authorizationPolicyBuilder,
            IEnumerable<string> allowedValues)
        {
            _ = Throws.IfNull(authorizationPolicyBuilder);

            authorizationPolicyBuilder.Requirements.Add(new ScopeAuthorizationRequirement(allowedValues));
            return authorizationPolicyBuilder;
        }

        /// <summary>
        /// Adds a <see cref="ScopeOrAppPermissionAuthorizationRequirement"/> to the current instance which requires
        /// that the current user has the specified claim and that the claim value must be one of the allowed values.
        /// </summary>
        /// <param name="authorizationPolicyBuilder">Used for building policies during application startup.</param>
        /// <param name="allowedScopeValues">scopes (the value of scope or scp) accepted by this app.</param>
        /// <param name="allowedAppPermissionValues">App permission (in role claim) that this app accepts.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public static AuthorizationPolicyBuilder RequireScopeOrAppPermission(
            this AuthorizationPolicyBuilder authorizationPolicyBuilder,
            IEnumerable<string> allowedScopeValues,
            IEnumerable<string> allowedAppPermissionValues)
        {
            _ = Throws.IfNull(authorizationPolicyBuilder);

            authorizationPolicyBuilder.Requirements.Add(new ScopeOrAppPermissionAuthorizationRequirement(
                allowedScopeValues,
                allowedAppPermissionValues));
            return authorizationPolicyBuilder;
        }
    }
}
