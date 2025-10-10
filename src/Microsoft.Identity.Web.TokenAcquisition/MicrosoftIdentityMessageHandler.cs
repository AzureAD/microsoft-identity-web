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
using Microsoft.Identity.Client;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// A <see cref="DelegatingHandler"/> implementation that automatically adds authorization headers 
    /// to outgoing HTTP requests using <see cref="IAuthorizationHeaderProvider"/> and 
    /// <see cref="MicrosoftIdentityMessageHandlerOptions"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This message handler provides a flexible, composable way to add Microsoft Identity authentication 
    /// to HttpClient-based code. It serves as an alternative to <c>IDownstreamApi</c> for scenarios where
    /// developers want to maintain direct control over HTTP request handling while still benefiting from
    /// Microsoft Identity Web's authentication capabilities.
    /// </para>
    /// 
    /// <para><strong>Key Features:</strong></para>
    /// <list type="bullet">
    /// <item><description>Automatic authorization header injection for all outgoing requests</description></item>
    /// <item><description>Per-request authentication options using extension methods</description></item>
    /// <item><description>Automatic WWW-Authenticate challenge handling with token refresh</description></item>
    /// <item><description>Support for agent identity and managed identity scenarios</description></item>
    /// <item><description>Comprehensive logging and error handling</description></item>
    /// <item><description>Multi-framework compatibility (.NET Framework 4.6.2+, .NET Standard 2.0+, .NET 5+)</description></item>
    /// </list>
    /// 
    /// <para><strong>WWW-Authenticate Challenge Handling:</strong></para>
    /// <para>
    /// When a downstream API returns a 401 Unauthorized response with a WWW-Authenticate header containing
    /// Bearer challenges with additional claims, this handler will automatically attempt to acquire a new token
    /// with the requested claims and retry the request. This is particularly useful for Conditional Access
    /// scenarios where additional claims are required.
    /// </para>
    /// </remarks>
    /// 
    /// <example>
    /// <para><strong>Basic setup with dependency injection:</strong></para>
    /// <code>
    /// // In Program.cs or Startup.cs
    /// services.AddHttpClient("MyApiClient", client =>
    /// {
    ///     client.BaseAddress = new Uri("https://api.example.com");
    /// })
    /// .AddHttpMessageHandler(serviceProvider => new MicrosoftIdentityMessageHandler(
    ///     serviceProvider.GetRequiredService&lt;IAuthorizationHeaderProvider&gt;(),
    ///     new MicrosoftIdentityMessageHandlerOptions 
    ///     { 
    ///         Scopes = { "https://api.example.com/.default" }
    ///     }));
    /// 
    /// // In a controller or service
    /// public class ApiService
    /// {
    ///     private readonly HttpClient _httpClient;
    ///     
    ///     public ApiService(IHttpClientFactory httpClientFactory)
    ///     {
    ///         _httpClient = httpClientFactory.CreateClient("MyApiClient");
    ///     }
    ///     
    ///     public async Task&lt;string&gt; GetDataAsync()
    ///     {
    ///         var response = await _httpClient.GetAsync("/api/data");
    ///         response.EnsureSuccessStatusCode();
    ///         return await response.Content.ReadAsStringAsync();
    ///     }
    /// }
    /// </code>
    /// 
    /// <para><strong>Per-request authentication options:</strong></para>
    /// <code>
    /// // Override scopes for a specific request
    /// var request = new HttpRequestMessage(HttpMethod.Get, "/api/sensitive-data")
    ///     .WithAuthenticationOptions(options =>
    ///     {
    ///         options.Scopes.Add("https://api.example.com/sensitive.read");
    ///         options.RequestAppToken = true;
    ///     });
    /// 
    /// var response = await _httpClient.SendAsync(request);
    /// </code>
    /// 
    /// <para><strong>Agent identity usage:</strong></para>
    /// <code>
    /// var request = new HttpRequestMessage(HttpMethod.Get, "/api/agent-data")
    ///     .WithAuthenticationOptions(options =>
    ///     {
    ///         options.Scopes.Add("https://graph.microsoft.com/.default");
    ///         options.WithAgentIdentity("agent-application-id");
    ///         options.RequestAppToken = true;
    ///     });
    /// 
    /// var response = await _httpClient.SendAsync(request);
    /// </code>
    /// 
    /// <para><strong>Manual instantiation:</strong></para>
    /// <code>
    /// var headerProvider = serviceProvider.GetRequiredService&lt;IAuthorizationHeaderProvider&gt;();
    /// var logger = serviceProvider.GetService&lt;ILogger&lt;MicrosoftIdentityMessageHandler&gt;&gt;();
    /// 
    /// var handler = new MicrosoftIdentityMessageHandler(
    ///     headerProvider, 
    ///     new MicrosoftIdentityMessageHandlerOptions 
    ///     { 
    ///         Scopes = { "https://graph.microsoft.com/.default" }
    ///     },
    ///     logger);
    /// 
    /// using var httpClient = new HttpClient(handler);
    /// var response = await httpClient.GetAsync("https://graph.microsoft.com/v1.0/me");
    /// </code>
    /// 
    /// <para><strong>Error handling:</strong></para>
    /// <code>
    /// try
    /// {
    ///     var response = await _httpClient.SendAsync(request, cancellationToken);
    ///     response.EnsureSuccessStatusCode();
    ///     return await response.Content.ReadAsStringAsync();
    /// }
    /// catch (MicrosoftIdentityAuthenticationException authEx)
    /// {
    ///     // Handle authentication-specific failures
    ///     _logger.LogError(authEx, "Authentication failed: {Message}", authEx.Message);
    ///     throw;
    /// }
    /// catch (HttpRequestException httpEx)
    /// {
    ///     // Handle other HTTP failures
    ///     _logger.LogError(httpEx, "HTTP request failed: {Message}", httpEx.Message);
    ///     throw;
    /// }
    /// </code>
    /// </example>
    /// 
    /// <seealso cref="MicrosoftIdentityMessageHandlerOptions"/>
    /// <seealso cref="HttpRequestMessageAuthenticationExtensions"/>
    /// <seealso cref="MicrosoftIdentityAuthenticationException"/>
    /// <seealso cref="IAuthorizationHeaderProvider"/>
    public class MicrosoftIdentityMessageHandler : DelegatingHandler
    {
        private readonly IAuthorizationHeaderProvider _headerProvider;
        private readonly MicrosoftIdentityMessageHandlerOptions? _defaultOptions;
        private readonly ILogger<MicrosoftIdentityMessageHandler>? _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="MicrosoftIdentityMessageHandler"/> class.
        /// </summary>
        /// <param name="headerProvider">
        /// The <see cref="IAuthorizationHeaderProvider"/> used to acquire authorization headers for outgoing requests.
        /// This is typically obtained from the dependency injection container.
        /// </param>
        /// <param name="defaultOptions">
        /// Default authentication options that will be used for all requests unless overridden per-request 
        /// using <see cref="HttpRequestMessageAuthenticationExtensions.WithAuthenticationOptions(HttpRequestMessage, MicrosoftIdentityMessageHandlerOptions)"/>.
        /// If <see langword="null"/>, each request must specify its own authentication options or an exception will be thrown.
        /// </param>
        /// <param name="logger">
        /// Optional logger for debugging and monitoring authentication operations. 
        /// If provided, the handler will log information about token acquisition, challenges, and errors.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="headerProvider"/> is <see langword="null"/>.
        /// </exception>
        /// <example>
        /// <para>Basic usage with default options:</para>
        /// <code>
        /// var handler = new MicrosoftIdentityMessageHandler(
        ///     headerProvider,
        ///     new MicrosoftIdentityMessageHandlerOptions 
        ///     { 
        ///         Scopes = { "https://api.example.com/.default" }
        ///     });
        /// </code>
        /// 
        /// <para>Usage without default options (per-request configuration required):</para>
        /// <code>
        /// var handler = new MicrosoftIdentityMessageHandler(headerProvider);
        /// 
        /// // Each request must specify options
        /// var request = new HttpRequestMessage(HttpMethod.Get, "/api/data")
        ///     .WithAuthenticationOptions(options => 
        ///         options.Scopes.Add("custom.scope"));
        /// </code>
        /// 
        /// <para>Usage with logging:</para>
        /// <code>
        /// var logger = serviceProvider.GetService&lt;ILogger&lt;MicrosoftIdentityMessageHandler&gt;&gt;();
        /// var handler = new MicrosoftIdentityMessageHandler(headerProvider, defaultOptions, logger);
        /// </code>
        /// </example>
        /// <remarks>
        /// <para>
        /// The <paramref name="defaultOptions"/> parameter provides a convenient way to set authentication
        /// options that apply to all requests made through this handler instance. Individual requests can 
        /// still override these defaults using the extension methods.
        /// </para>
        /// <para>
        /// When <paramref name="logger"/> is provided, the handler will log at various levels:
        /// </para>
        /// <list type="bullet">
        /// <item><description><strong>Debug:</strong> Successful authorization header addition</description></item>
        /// <item><description><strong>Information:</strong> WWW-Authenticate challenge detection and handling</description></item>
        /// <item><description><strong>Warning:</strong> Challenge handling failures</description></item>
        /// <item><description><strong>Error:</strong> Token acquisition failures</description></item>
        /// </list>
        /// </remarks>
        public MicrosoftIdentityMessageHandler(
            IAuthorizationHeaderProvider headerProvider,
            MicrosoftIdentityMessageHandlerOptions? defaultOptions = null,
            ILogger<MicrosoftIdentityMessageHandler>? logger = null)
        {
            _headerProvider = headerProvider ?? throw new ArgumentNullException(nameof(headerProvider));
            _defaultOptions = defaultOptions;
            _logger = logger;
        }

        /// <summary>
        /// Sends an HTTP request with automatic authentication header injection.
        /// Handles WWW-Authenticate challenges by attempting token refresh with additional claims if needed.
        /// </summary>
        /// <param name="request">The HTTP request message to send.</param>
        /// <param name="cancellationToken">A cancellation token to cancel operation.</param>
        /// <returns>The HTTP response message.</returns>
        /// <exception cref="MicrosoftIdentityAuthenticationException">
        /// Thrown when authentication fails, including scenarios where:
        /// - No authentication options are configured
        /// - No scopes are specified in the options
        /// - Token acquisition fails
        /// - WWW-Authenticate challenge handling fails
        /// </exception>
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

            // Send the request with authentication
            var response = await SendWithAuthenticationAsync(request, options, scopes, cancellationToken).ConfigureAwait(false);

            // Handle WWW-Authenticate challenge if present
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                // Use MSAL's WWW-Authenticate parser to extract claims from challenge headers
                string? challengeClaims = WwwAuthenticateParameters.GetClaimChallengeFromResponseHeaders(response.Headers);
                
                if (!string.IsNullOrEmpty(challengeClaims))
                {
                    _logger?.LogInformation(
                        "Received WWW-Authenticate challenge with claims. Attempting token refresh.");

                    // Create a new options instance with the challenge claims
                    var challengeOptions = CreateOptionsWithChallengeClaims(options, challengeClaims);
                    
                    // Clone the original request for retry
                    using var retryRequest = await CloneHttpRequestMessageAsync(request).ConfigureAwait(false);
                    
                    // Attempt to get a new token with the challenge claims
                    var retryResponse = await SendWithAuthenticationAsync(retryRequest, challengeOptions, scopes, cancellationToken).ConfigureAwait(false);
                    
                    // Log information about the retry response
                    if (retryResponse.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        _logger?.LogWarning(
                            "Retry after WWW-Authenticate challenge still returned 401 Unauthorized. WWW-Authenticate header present: {HasWwwAuthenticate}",
                            retryResponse.Headers.WwwAuthenticate?.Any() == true);
                    }
                    else
                    {
                        _logger?.LogInformation("Successfully handled WWW-Authenticate challenge. Status code: {StatusCode}", retryResponse.StatusCode);
                    }
                    
                    // Dispose the original response and return the retry response
                    response.Dispose();
                    return retryResponse;
                }
                else
                {
                    _logger?.LogWarning("Received 401 Unauthorized but no WWW-Authenticate challenge with claims found.");
                }
            }

            return response;
        }

        /// <summary>
        /// Sends an HTTP request with authentication header injection.
        /// </summary>
        /// <param name="request">The HTTP request message.</param>
        /// <param name="options">The authentication options to use.</param>
        /// <param name="scopes">The scopes for token acquisition.</param>
        /// <param name="cancellationToken">A cancellation token to cancel operation.</param>
        /// <returns>The HTTP response message.</returns>
        /// <exception cref="MicrosoftIdentityAuthenticationException">Thrown when token acquisition fails.</exception>
        private async Task<HttpResponseMessage> SendWithAuthenticationAsync(
            HttpRequestMessage request, 
            MicrosoftIdentityMessageHandlerOptions options, 
            IList<string> scopes, 
            CancellationToken cancellationToken)
        {
            // Acquire authorization header
            try
            {
                var authHeader = await _headerProvider.CreateAuthorizationHeaderAsync(
                    scopes, options, cancellationToken: cancellationToken).ConfigureAwait(false);

                // Remove existing authorization header if present
                if (request.Headers.Contains("Authorization"))
                {
                    request.Headers.Remove("Authorization");
                }

                // Add the authorization header
                request.Headers.Add("Authorization", authHeader);

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
            return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Creates a new options instance with challenge claims added.
        /// </summary>
        /// <param name="originalOptions">The original authentication options.</param>
        /// <param name="challengeClaims">The claims from the WWW-Authenticate challenge.</param>
        /// <returns>A new options instance with challenge claims configured.</returns>
        private static MicrosoftIdentityMessageHandlerOptions CreateOptionsWithChallengeClaims(
            MicrosoftIdentityMessageHandlerOptions originalOptions, 
            string challengeClaims)
        {
            var challengeOptions = new MicrosoftIdentityMessageHandlerOptions
            {
                Scopes = originalOptions.Scopes
            };

            // Copy properties from the base AuthorizationHeaderProviderOptions
            if (originalOptions.AcquireTokenOptions != null)
            {
                challengeOptions.AcquireTokenOptions = new AcquireTokenOptions
                {
                    AuthenticationOptionsName = originalOptions.AcquireTokenOptions.AuthenticationOptionsName,
                    Claims = challengeClaims, // Set the challenge claims
                    CorrelationId = originalOptions.AcquireTokenOptions.CorrelationId,
                    ExtraHeadersParameters = originalOptions.AcquireTokenOptions.ExtraHeadersParameters,
                    ExtraQueryParameters = originalOptions.AcquireTokenOptions.ExtraQueryParameters,
                    ExtraParameters = originalOptions.AcquireTokenOptions.ExtraParameters,
                    ForceRefresh = true, // Force refresh when handling challenges
                    ManagedIdentity = originalOptions.AcquireTokenOptions.ManagedIdentity,
                    PopPublicKey = originalOptions.AcquireTokenOptions.PopPublicKey,
                    Tenant = originalOptions.AcquireTokenOptions.Tenant,
                    UserFlow = originalOptions.AcquireTokenOptions.UserFlow
                };
            }
            else
            {
                challengeOptions.AcquireTokenOptions = new AcquireTokenOptions
                {
                    Claims = challengeClaims,
                    ForceRefresh = true
                };
            }

            // Copy other inherited properties
            challengeOptions.RequestAppToken = originalOptions.RequestAppToken;
            challengeOptions.BaseUrl = originalOptions.BaseUrl;
            challengeOptions.HttpMethod = originalOptions.HttpMethod;
            challengeOptions.RelativePath = originalOptions.RelativePath;

            return challengeOptions;
        }

        /// <summary>
        /// Clones an HttpRequestMessage for retry scenarios.
        /// </summary>
        /// <param name="originalRequest">The original request to clone.</param>
        /// <returns>A cloned HttpRequestMessage.</returns>
        private static async Task<HttpRequestMessage> CloneHttpRequestMessageAsync(HttpRequestMessage originalRequest)
        {
            var clonedRequest = new HttpRequestMessage(originalRequest.Method, originalRequest.RequestUri);
            
            // Copy headers
            foreach (var header in originalRequest.Headers)
            {
                // Skip Authorization header as it will be set by the handler
                if (header.Key != "Authorization")
                {
                    clonedRequest.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
            }

            // Copy content if present
            if (originalRequest.Content != null)
            {
                var contentBytes = await originalRequest.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
                clonedRequest.Content = new ByteArrayContent(contentBytes);
                
                // Copy content headers
                foreach (var header in originalRequest.Content.Headers)
                {
                    clonedRequest.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
            }

            // Copy properties/options (excluding authentication options which will be set separately)
            // Note: We don't copy options to avoid complications with typed keys.
            // Most HttpClient scenarios don't rely on copying all options to retry requests.
#if !NET5_0_OR_GREATER
            foreach (var property in originalRequest.Properties)
            {
                // Skip our authentication options as they will be set separately
                if (!property.Key.Equals("Microsoft.Identity.AuthenticationOptions", StringComparison.Ordinal))
                {
                    clonedRequest.Properties[property.Key] = property.Value;
                }
            }
#endif

            return clonedRequest;
        }
    }
}