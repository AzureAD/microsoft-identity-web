// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Abstractions;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// A DelegatingHandler implementation that adds an authorization header to outgoing HTTP requests
    /// using <see cref="IAuthorizationHeaderProvider"/> and <see cref="AuthorizationHeaderProviderOptions"/>.
    /// </summary>
    public class MicrosoftIdentityMessageHandler : DelegatingHandler
    {
        private readonly IAuthorizationHeaderProvider _headerProvider;
        private readonly MicrosoftIdentityMessageHandlerOptions? _defaultOptions;
        private readonly ILogger<MicrosoftIdentityMessageHandler>? _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="MicrosoftIdentityMessageHandler"/> class.
        /// </summary>
        /// <param name="headerProvider">The authorization header provider.</param>
        /// <param name="defaultOptions">Default options for authentication. Can be overridden per-request.</param>
        /// <param name="logger">Optional logger instance.</param>
        /// <exception cref="ArgumentNullException">Thrown when headerProvider is null.</exception>
        public MicrosoftIdentityMessageHandler(
            IAuthorizationHeaderProvider headerProvider,
            MicrosoftIdentityMessageHandlerOptions? defaultOptions = null,
            ILogger<MicrosoftIdentityMessageHandler>? logger = null)
        {
            _headerProvider = headerProvider ?? throw new ArgumentNullException(nameof(headerProvider));
            _defaultOptions = defaultOptions;
            _logger = logger;
        }

        /// <inheritdoc/>
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // Get per-request options or use default
            var options = request.GetAuthenticationOptions() ?? _defaultOptions;
            
            if (options == null)
            {
                throw new MicrosoftIdentityAuthenticationException(
                    "Authentication options must be configured either in default options or per-request using WithAuthenticationOptions().");
            }

            // Get scopes from options
            var scopes = options.Scopes;
            
            if (scopes == null || !scopes.Any())
            {
                throw new MicrosoftIdentityAuthenticationException(
                    "Authentication scopes must be configured in the options.Scopes property.");
            }

            // Acquire authorization header
            try
            {
                var authHeader = await _headerProvider.CreateAuthorizationHeaderAsync(
                    scopes, options, cancellationToken: cancellationToken).ConfigureAwait(false);

                // Remove existing authorization header if present
                if (request.Headers.Contains(Constants.Authorization))
                {
                    request.Headers.Remove(Constants.Authorization);
                }

                // Add the authorization header
                request.Headers.Add(Constants.Authorization, authHeader);

                _logger?.LogDebug(
                    "Added Authorization header for scopes: {Scopes}",
                    string.Join(", ", scopes));
            }
            catch (Exception ex)
            {
                var message = "Failed to acquire authorization header.";
                _logger?.LogError(ex, message);
                throw new MicrosoftIdentityAuthenticationException(message, ex);
            }

            // Send the request
            var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

            // Handle WWW-Authenticate challenge if present
            if (response.StatusCode == HttpStatusCode.Unauthorized &&
                response.Headers.WwwAuthenticate?.ToString().IndexOf("Bearer", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                _logger?.LogWarning("Received WWW-Authenticate challenge. Token may need to be refreshed or additional claims may be required.");
                
                // For now, we'll return the response as-is. 
                // Future enhancement: Handle challenge by extracting claims and retrying
                // This logic can be factorized with DownstreamApi implementation later
            }

            return response;
        }
    }
}