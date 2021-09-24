// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

namespace Microsoft.Identity.Web.Resource
{
    /// <summary>
    /// This attribute is used on a controller, pages, or controller actions
    /// to declare (and validate) the scopes required by a web API. These scopes can be declared
    /// in two ways: hardcoding them, or declaring them in the configuration. Depending on your
    /// choice, use either one or the other of the constructors.
    /// For details, see https://aka.ms/ms-id-web/required-scope-attribute.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class RequiredScopeAttribute : Attribute, IAuthRequiredScopeMetadata
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
        public string? RequiredScopeConfigurationKey { get; set; }

        /// <summary>
        /// Verifies that the web API is called with the right scopes.
        /// If the token obtained for this API is on behalf of the authenticated user does not have
        /// any of these <paramref name="acceptedScopes"/> in its scope claim, the
        /// method updates the HTTP response providing a status code 403 (Forbidden)
        /// and writes to the response body a message telling which scopes are expected in the token.
        /// </summary>
        /// <param name="acceptedScopes">Scopes accepted by this web API.</param>
        /// <remarks>When the scopes don't match, the response is a 403 (Forbidden),
        /// because the user is authenticated (hence not 401), but not authorized.</remarks>
        /// <example>
        /// Add the following attribute on the controller/page/action to protect:
        ///
        /// <code>
        /// [RequiredScope("access_as_user")]
        /// </code>
        /// </example>
        /// <seealso cref="M:RequiredScopeAttribute()"/> and <see cref="RequiredScopeConfigurationKey"/>
        /// if you want to express the required scopes from the configuration.
        public RequiredScopeAttribute(params string[] acceptedScopes)
        {
            AcceptedScope = acceptedScopes ?? throw new ArgumentNullException(nameof(acceptedScopes));
        }

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <example>
        /// <code>
        /// [RequiredScope(RequiredScopesConfigurationKey="AzureAD:Scope")]
        /// class Controller : BaseController
        /// {
        /// }
        /// </code>
        /// </example>
        public RequiredScopeAttribute()
        {
        }
    }
}
