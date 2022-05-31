using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Identity.Web.Resource
{
    /// <summary>
    /// Issue #1641 - A single attribute for Scopes and Roles
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class RequiredScopeOrAppPermissionAttribute : RequiredScopeAttribute, IAuthRequiredScopeAndAppPermissionMetadata
    {
        /// <summary>
        /// Roles accepted by this Web API
        /// </summary>
        public IEnumerable<string>? AcceptedAppPermissions { get; set; }

        /// <summary>
        /// Fully qualified name of the configuration key containing the required app permissions (separated
        /// by spaces).
        /// </summary>
        /// <example>
        /// If the appsettings.json file contains a section named "AzureAd", in which
        /// a property named "Scopes" contains the required scopes, the attribute on the
        /// controller/page/action to protect should be set to the following:
        /// <code>
        /// [RequiredScopeOrAppPermissionAttribute(RequiredScopesConfigurationKey="AzureAd:Scopes",
        ///                                                                      RequiredAppPermissionsConfigurationKey="AzureAd:AppPermissions")]
        /// </code>
        /// </example>
        public string? RequiredAppPermissionsConfigurationKey { get; set; }

        /// <summary>
        /// Verifies that the web API is called with the right scopes (delegated permissions) or app permissions.
        /// If the token obtained for this API is on behalf of the authenticated user does not have
        /// any of these <paramref name="acceptedScopes"/> or in its scope claim, or 
        /// any of the <paramref name="acceptedAppPermissions"/> in its role/roles claims, the
        /// method updates the HTTP response providing a status code 403 (Forbidden)
        /// and writes to the response body a message telling which scopes are expected in the token.
        /// </summary>
        /// <param name="acceptedScopes">Scopes accepted by this web API.</param>
        /// <param name="acceptedAppPermissions">App permissions accepted by this web API.</param>
        /// <remarks>When the scopes don't match, the response is a 403 (Forbidden),
        /// because the user is authenticated (hence not 401), but not authorized.</remarks>
        /// <example>
        /// Add the following attribute on the controller/page/action to protect:
        ///
        /// <code>
        /// [RequiredScopeOrAppPermissionAttribute(acceptedScopes = new []{"access_as_user"}, acceptedAppPermissions = new []{"access_as_app"}, )]
        /// </code>
        /// </example>
        /// <seealso cref="M:RequiredScopeAttribute(string,string)"/>
        /// if you want to express the required scopes from the configuration.
        public RequiredScopeOrAppPermissionAttribute(string[] acceptedScopes, string[] acceptedAppPermissions) : base(acceptedScopes)
        {
            AcceptedAppPermissions = acceptedAppPermissions ?? throw new ArgumentNullException(nameof(acceptedAppPermissions));
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        public RequiredScopeOrAppPermissionAttribute() { }

    }
}
