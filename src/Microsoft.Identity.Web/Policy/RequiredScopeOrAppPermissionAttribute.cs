// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

namespace Microsoft.Identity.Web.Resource
{
    /// <summary>
    /// This attribute is used on a controller, pages, or controller actions
    /// to declare (and validate) the scopes or app permissions required by a web API. 
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
        public IEnumerable<string>? AcceptedScope { get; set; }

        /// <summary>
        /// Fully qualified name of the configuration key containing the required scopes (separated
        /// by spaces).
        /// </summary>
        /// <example>
        /// If the appsettings.json file contains a section named "AzureAd", in which
        /// a property named "Scopes" contains the required scopes, the attribute on the
        /// controller/page/action to protect should be set to the following:
        /// <code>
        /// [RequiredScope(RequiredScopesConfigurationKey="AzureAd:Scopes")]
        /// </code>
        /// </example>
        public string? RequiredScopesConfigurationKey { get; set; }

        /// <summary>
        /// Unused: Compatibility of interface with the Authorization Filter.
        /// </summary>
        public bool IsReusable { get; set; }

        /// <summary>
        /// App permissions accepted by this web API.
        /// App permissions appear in the roles claim of the token.
        /// </summary>
        public IEnumerable<string>? AcceptedAppPermission { get; set; }

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
        /// Verifies that the web API is called with the right app permissions.
        /// If the token obtained for this API is on behalf of the authenticated user does not have
        /// any of these <paramref name="acceptedScopes"/> in its scope claim, 
        /// nor <paramref name="acceptedAppPermissions"/> in its roles claim, the
        /// method updates the HTTP response providing a status code 403 (Forbidden)
        /// and writes to the response body a message telling which scopes are expected in the token.
        /// </summary>
        /// <param name="acceptedScopes">Scopes accepted by this web API.</param>
        /// <param name="acceptedAppPermissions">App permissions accepted by this web API.</param>
        /// <remarks>When neither the scopes nor app permissions match, the response is a 403 (Forbidden),
        /// because the user is authenticated (hence not 401), but not authorized.</remarks>
        /// <example>
        /// Add the following attribute on the controller/page/action to protect:
        ///
        /// <code>
        /// [RequiredScopeOrAppPermissionAttribute(new string[] { "access_as_user" }, new string[] { "access_as_app" })]
        /// </code>
        /// </example>
        /// <seealso cref="M:RequiredScopeOrAppPermissionAttribute()"/> and <see cref="RequiredAppPermissionsConfigurationKey"/>
        /// if you want to express the required scopes or app permissions from the configuration.
        public RequiredScopeOrAppPermissionAttribute(string[] acceptedScopes, string[] acceptedAppPermissions)
        {
            AcceptedScope = acceptedScopes ?? throw new ArgumentNullException(nameof(acceptedScopes));
            AcceptedAppPermission = acceptedAppPermissions ?? throw new ArgumentNullException(nameof(acceptedAppPermissions));
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
