// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using Microsoft.Identity.Abstractions;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Configuration options for <see cref="MicrosoftIdentityMessageHandler"/> authentication.
    /// Inherits from <see cref="AuthorizationHeaderProviderOptions"/> to enable compatibility
    /// with existing extension methods such as <c>WithAgentIdentity()</c> and <c>WithUserAgentIdentity()</c>.
    /// </summary>
    /// <example>
    /// <para>Basic usage with scopes:</para>
    /// <code>
    /// var options = new MicrosoftIdentityMessageHandlerOptions
    /// {
    ///     Scopes = { "https://graph.microsoft.com/.default" }
    /// };
    /// </code>
    /// 
    /// <para>Usage with extension methods:</para>
    /// <code>
    /// var options = new MicrosoftIdentityMessageHandlerOptions
    /// {
    ///     Scopes = { "api://myapi/.default" }
    /// };
    /// options.WithAgentIdentity("agent-guid");
    /// </code>
    /// </example>
    /// <seealso cref="MicrosoftIdentityMessageHandler"/>
    /// <seealso cref="AuthorizationHeaderProviderOptions"/>
    public class MicrosoftIdentityMessageHandlerOptions : AuthorizationHeaderProviderOptions
    {
        /// <summary>
        /// Gets or sets the scopes to request for the token.
        /// </summary>
        /// <value>
        /// A list of scopes required to access the target API. 
        /// For instance, "user.read mail.read" for Microsoft Graph user permissions.
        /// For Microsoft Identity, in the case of application tokens (requested by the app on behalf of itself), 
        /// there should be only one scope, and it should end with ".default" (e.g., "https://graph.microsoft.com/.default").
        /// </value>
        /// <example>
        /// <code>
        /// var options = new MicrosoftIdentityMessageHandlerOptions();
        /// options.Scopes.Add("https://graph.microsoft.com/.default");
        /// options.Scopes.Add("https://myapi.domain.com/access");
        /// </code>
        /// </example>
        /// <remarks>
        /// This property must contain at least one scope, or the <see cref="MicrosoftIdentityMessageHandler"/>
        /// will throw a <see cref="MicrosoftIdentityAuthenticationException"/> when processing requests.
        /// </remarks>
        public IList<string> Scopes { get; set; } = new List<string>();
    }
}