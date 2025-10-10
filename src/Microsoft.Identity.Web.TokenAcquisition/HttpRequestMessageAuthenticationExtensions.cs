// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Net.Http;
using Microsoft.Identity.Abstractions;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Extension methods for <see cref="HttpRequestMessage"/> to configure per-request authentication options
    /// when using <see cref="MicrosoftIdentityMessageHandler"/>.
    /// </summary>
    /// <remarks>
    /// These extension methods enable flexible per-request authentication configuration that can override
    /// or supplement the default options configured in the message handler. The methods support both
    /// modern .NET (using HttpRequestMessage.Options) and legacy frameworks 
    /// (using HttpRequestMessage.Properties).
    /// </remarks>
    /// <example>
    /// <para>Setting authentication options with an object:</para>
    /// <code>
    /// var request = new HttpRequestMessage(HttpMethod.Get, "/api/data")
    ///     .WithAuthenticationOptions(new MicrosoftIdentityMessageHandlerOptions
    ///     {
    ///         Scopes = { "custom.scope" }
    ///     });
    /// </code>
    /// 
    /// <para>Configuring authentication options with a delegate:</para>
    /// <code>
    /// var request = new HttpRequestMessage(HttpMethod.Get, "/api/data")
    ///     .WithAuthenticationOptions(options =>
    ///     {
    ///         options.Scopes.Add("https://graph.microsoft.com/.default");
    ///         options.WithAgentIdentity("agent-guid");
    ///         options.RequestAppToken = true;
    ///     });
    /// </code>
    /// </example>
    public static class HttpRequestMessageAuthenticationExtensions
    {
        private const string AuthOptionsKey = "Microsoft.Identity.AuthenticationOptions";

        /// <summary>
        /// Sets authentication options for the HTTP request.
        /// </summary>
        /// <param name="request">The HTTP request message to configure.</param>
        /// <param name="options">The authentication options to apply to this request.</param>
        /// <returns>The same request message for method chaining.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="request"/> or <paramref name="options"/> is <see langword="null"/>.
        /// </exception>
        /// <example>
        /// <code>
        /// var options = new MicrosoftIdentityMessageHandlerOptions
        /// {
        ///     Scopes = { "https://graph.microsoft.com/.default" }
        /// };
        /// options.WithAgentIdentity("my-agent-guid");
        /// 
        /// var request = new HttpRequestMessage(HttpMethod.Get, "/me")
        ///     .WithAuthenticationOptions(options);
        /// </code>
        /// </example>
        /// <remarks>
        /// This method will override any existing authentication options set on the request.
        /// The options object can be further configured with extension methods from other Microsoft Identity Web packages.
        /// </remarks>
        public static HttpRequestMessage WithAuthenticationOptions(
            this HttpRequestMessage request, MicrosoftIdentityMessageHandlerOptions options)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (options == null) throw new ArgumentNullException(nameof(options));

#if NET5_0_OR_GREATER
            request.Options.Set(new HttpRequestOptionsKey<MicrosoftIdentityMessageHandlerOptions>(AuthOptionsKey), options);
#else
            // Use Properties dictionary for older frameworks
            request.Properties[AuthOptionsKey] = options;
#endif
            return request;
        }

        /// <summary>
        /// Configures authentication options for the HTTP request using a delegate.
        /// </summary>
        /// <param name="request">The HTTP request message to configure.</param>
        /// <param name="configure">A delegate that configures the authentication options.</param>
        /// <returns>The same request message for method chaining.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="request"/> or <paramref name="configure"/> is <see langword="null"/>.
        /// </exception>
        /// <example>
        /// <code>
        /// var request = new HttpRequestMessage(HttpMethod.Get, "/api/users")
        ///     .WithAuthenticationOptions(options =>
        ///     {
        ///         options.Scopes.Add("https://myapi.domain.com/user.read");
        ///         options.WithAgentIdentity("agent-application-id");
        ///         options.RequestAppToken = true;
        ///     });
        /// </code>
        /// </example>
        /// <remarks>
        /// <para>
        /// If the request already has authentication options configured, the delegate will receive
        /// the existing options object to modify. Otherwise, a new <see cref="MicrosoftIdentityMessageHandlerOptions"/> 
        /// instance will be created and passed to the delegate.
        /// </para>
        /// <para>
        /// This method is particularly useful when you need to apply extension methods from other
        /// Microsoft Identity Web packages, such as agent identity methods.
        /// </para>
        /// </remarks>
        public static HttpRequestMessage WithAuthenticationOptions(
            this HttpRequestMessage request, Action<MicrosoftIdentityMessageHandlerOptions> configure)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (configure == null) throw new ArgumentNullException(nameof(configure));

            var options = request.GetAuthenticationOptions() ?? new MicrosoftIdentityMessageHandlerOptions();

            configure(options);

#if NET5_0_OR_GREATER
            request.Options.Set(new HttpRequestOptionsKey<MicrosoftIdentityMessageHandlerOptions>(AuthOptionsKey), options);
#else
            // Use Properties dictionary for older frameworks
            request.Properties[AuthOptionsKey] = options;
#endif
            return request;
        }

        /// <summary>
        /// Gets the authentication options that have been set for the HTTP request.
        /// </summary>
        /// <param name="request">The HTTP request message to examine.</param>
        /// <returns>
        /// The <see cref="MicrosoftIdentityMessageHandlerOptions"/> if previously set using 
        /// <see cref="WithAuthenticationOptions(HttpRequestMessage, MicrosoftIdentityMessageHandlerOptions)"/> 
        /// or <see cref="WithAuthenticationOptions(HttpRequestMessage, Action{MicrosoftIdentityMessageHandlerOptions})"/>, 
        /// otherwise <see langword="null"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="request"/> is <see langword="null"/>.
        /// </exception>
        /// <example>
        /// <code>
        /// var request = new HttpRequestMessage(HttpMethod.Get, "/api/data")
        ///     .WithAuthenticationOptions(options => options.Scopes.Add("custom.scope"));
        /// 
        /// var options = request.GetAuthenticationOptions();
        /// if (options != null)
        /// {
        ///     Console.WriteLine($"Request has {options.Scopes.Count} scopes configured.");
        /// }
        /// </code>
        /// </example>
        /// <remarks>
        /// This method is primarily used internally by <see cref="MicrosoftIdentityMessageHandler"/>
        /// but can also be useful for debugging or conditional logic based on authentication configuration.
        /// </remarks>
        public static MicrosoftIdentityMessageHandlerOptions? GetAuthenticationOptions(this HttpRequestMessage request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

#if NET5_0_OR_GREATER
            request.Options.TryGetValue(new HttpRequestOptionsKey<MicrosoftIdentityMessageHandlerOptions>(AuthOptionsKey), out var options);
            return options;
#else
            // Use Properties dictionary for older frameworks
            return request.Properties.TryGetValue(AuthOptionsKey, out var options) 
                ? options as MicrosoftIdentityMessageHandlerOptions 
                : null;
#endif
        }
    }
}