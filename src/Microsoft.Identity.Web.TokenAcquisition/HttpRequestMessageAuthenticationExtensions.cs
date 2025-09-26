// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Net.Http;
using Microsoft.Identity.Abstractions;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Extensions for <see cref="HttpRequestMessage"/> to configure per-request authentication options.
    /// </summary>
    public static class HttpRequestMessageAuthenticationExtensions
    {
        private const string AuthOptionsKey = "Microsoft.Identity.AuthenticationOptions";

        /// <summary>
        /// Sets authentication options for the HTTP request.
        /// </summary>
        /// <param name="request">The HTTP request message.</param>
        /// <param name="options">The authentication options to set.</param>
        /// <returns>The same request message for method chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when request or options is null.</exception>
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
        /// <param name="request">The HTTP request message.</param>
        /// <param name="configure">A delegate to configure the authentication options.</param>
        /// <returns>The same request message for method chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when request or configure is null.</exception>
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
        /// Gets the authentication options set for the HTTP request.
        /// </summary>
        /// <param name="request">The HTTP request message.</param>
        /// <returns>The authentication options if set, otherwise null.</returns>
        /// <exception cref="ArgumentNullException">Thrown when request is null.</exception>
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