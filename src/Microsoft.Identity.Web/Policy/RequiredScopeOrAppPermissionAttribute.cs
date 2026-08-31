// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Microsoft.Identity.Web.Resource
{
    /// <summary>
    /// This attribute is used on a controller, pages, or controller actions
    /// to declare (and validate) the scopes or app permissions required by a web API.
    /// Authorization succeeds when an accepted scope is present in a scope claim or an accepted
    /// app permission is present in a role claim. Matching a role does not classify the token as app-only.
    /// These scopes or app permissions can be declared in two ways:
    /// hardcoding them, or declaring them in the configuration. Depending on your
    /// choice, use either one or the other of the constructors.
    /// For details, see https://aka.ms/ms-id-web/required-scope-or-app-permissions-attribute.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class RequiredScopeOrAppPermissionAttribute : Attribute, IAuthRequiredScopeOrAppPermissionMetadata
    {
        /// <summary>
        /// Scopes accepted by this web API.
        /// </summary>
        public string[]? AcceptedScope { get; set; }

        /// <summary>
        /// Fully qualified name of the configuration key containing the required scopes (separated
        /// by spaces).
        /// </summary>
        /// <example>
        /// If the appsettings.json file contains a section named "AzureAd", in which
        /// a property named "Scopes" contains the required scopes, the attribute on the
        /// controller/page/action to protect should be set to the following:
        /// <code>
        /// [RequiredScopeOrAppPermission(RequiredScopesConfigurationKey="AzureAd:Scopes")]
        /// </code>
        /// </example>
        public string? RequiredScopesConfigurationKey { get; set; }

        /// <summary>
        /// App permissions accepted by this web API.
        /// App permissions appear in the roles claim of the token.
        /// </summary>
        public string[]? AcceptedAppPermission { get; set; }

        /// <summary>
        /// Fully qualified name of the configuration key containing the required app permissions (separated
        /// by spaces).
        /// </summary>
        /// <example>
        /// If the appsettings.json file contains a section named "AzureAd", in which
        /// a property named "AppPermissions" contains the required app permissions, the attribute on the
        /// controller/page/action to protect should be set to the following:
        /// <code>
        /// [RequiredScopeOrAppPermission(RequiredAppPermissionsConfigurationKey="AzureAd:AppPermissions")]
        /// </code>
        /// </example>
        public string? RequiredAppPermissionsConfigurationKey { get; set; }

        /// <summary>
        /// Declares the scopes and app permissions accepted by this web API.
        /// Authorization succeeds when the token has any of these <paramref name="acceptedScopes"/> in its
        /// scope claims or any of these <paramref name="acceptedAppPermissions"/> in its role claims.
        /// </summary>
        /// <param name="acceptedScopes">Scopes accepted by this web API.</param>
        /// <param name="acceptedAppPermissions">App permissions accepted by this web API.</param>
        /// <remarks>When neither the scopes nor app permissions match, the response is a 403 (Forbidden),
        /// because the user is authenticated (hence not 401), but not authorized.</remarks>
        /// <example>
        /// Add the following attribute on the controller/page/action to protect:
        ///
        /// <code>
        /// [RequiredScopeOrAppPermission(new [] { "access_as_user" }, new [] { "access_as_app" })]
        /// </code>
        /// </example>
        /// <seealso cref="M:RequiredScopeOrAppPermissionAttribute()"/> and <see cref="RequiredAppPermissionsConfigurationKey"/>
        /// if you want to express the required scopes or app permissions from the configuration.
        public RequiredScopeOrAppPermissionAttribute(string[] acceptedScopes, string[] acceptedAppPermissions)
        {
            AcceptedScope = Throws.IfNull(acceptedScopes);
            AcceptedAppPermission = Throws.IfNull(acceptedAppPermissions);
        }

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <example>
        /// <code>
        /// [RequiredScopeOrAppPermission(RequiredScopesConfigurationKey="AzureAD:Scope", RequiredAppPermissionsConfigurationKey="AzureAD:AppPermission")]
        /// class Controller : BaseController
        /// {
        /// }
        /// </code>
        /// </example>
        public RequiredScopeOrAppPermissionAttribute()
        {
        }
    }
}
