// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web.Diagnostics;

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
        private readonly ICredentialsProvider? _credentialsProvider;
        private readonly IMsalMtlsHttpClientFactory? _mtlsHttpClientFactory;
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
            : this(headerProvider, defaultOptions, mtlsHttpClientFactory: null, logger)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MicrosoftIdentityMessageHandler"/> class with mTLS PoP token binding support.
        /// </summary>
        /// <param name="headerProvider">
        /// The <see cref="IAuthorizationHeaderProvider"/> used to acquire authorization headers for outgoing requests.
        /// This is typically obtained from the dependency injection container.
        /// </param>
        /// <param name="defaultOptions">
        /// Default authentication options that will be used for all requests unless overridden per-request.
        /// If <see langword="null"/>, each request must specify its own authentication options or an exception will be thrown.
        /// </param>
        /// <param name="mtlsHttpClientFactory">
        /// Optional factory for creating HTTP clients configured with mTLS client certificates for token binding
        /// (mTLS PoP) scenarios. When provided and the <see cref="AuthorizationHeaderProviderOptions.ProtocolScheme"/>
        /// is set to <c>"MTLS_POP"</c>, the handler will use this factory to create an HTTP client with the binding
        /// certificate and send requests through it.
        /// </param>
        /// <param name="logger">
        /// Optional logger for debugging and monitoring authentication operations.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="headerProvider"/> is <see langword="null"/>.
        /// </exception>
        /// <remarks>
        /// <para>
        /// mTLS PoP (Mutual TLS Proof-of-Possession) token binding, as described in
        /// <see href="https://datatracker.ietf.org/doc/html/rfc8705">RFC 8705</see>,
        /// cryptographically binds access tokens to a specific X.509 certificate. When enabled,
        /// the handler acquires a bound token with the certificate thumbprint in the <c>cnf</c> claim,
        /// creates an mTLS HTTP client with the binding certificate, and sends requests through the mTLS channel.
        /// </para>
        /// <para>
        /// Token binding currently supports only application (app-only) tokens. Set
        /// <see cref="AuthorizationHeaderProviderOptions.RequestAppToken"/> to <see langword="true"/>.
        /// </para>
        /// <para>
        /// Prefer using the <see cref="MicrosoftIdentityHttpClientBuilderExtensions"/> extension methods
        /// to configure this handler through dependency injection rather than instantiating it directly.
        /// </para>
        /// </remarks>
        public MicrosoftIdentityMessageHandler(
            IAuthorizationHeaderProvider headerProvider,
            MicrosoftIdentityMessageHandlerOptions? defaultOptions,
            IMsalMtlsHttpClientFactory? mtlsHttpClientFactory,
            ILogger<MicrosoftIdentityMessageHandler>? logger = null)
            : this(headerProvider, defaultOptions, mtlsHttpClientFactory, null, logger)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MicrosoftIdentityMessageHandler"/> class with mTLS and mTLS_Pop support.
        /// </summary>
        /// <param name="headerProvider">
        /// The <see cref="IAuthorizationHeaderProvider"/> used to acquire authorization headers for outgoing requests.
        /// This is typically obtained from the dependency injection container.
        /// </param>
        /// <param name="defaultOptions">
        /// Default authentication options that will be used for all requests unless overridden per-request.
        /// If <see langword="null"/>, each request must specify its own authentication options or an exception will be thrown.
        /// </param>
        /// <param name="mtlsHttpClientFactory">
        /// Optional factory for creating HTTP clients configured with mTLS client certificates for token binding
        /// (mTLS PoP) scenarios. When provided and the <see cref="AuthorizationHeaderProviderOptions.ProtocolScheme"/>
        /// is set to <c>"MTLS_POP"</c>, the handler will use this factory to create an HTTP client with the binding
        /// certificate and send requests through it.
        /// </param>
        /// <param name="credentialsProvider">
        /// Optional provider for certificates. This is required for mTLS-only authentication purposes.
        /// </param>
        /// <param name="logger">
        /// Optional logger for debugging and monitoring authentication operations.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="headerProvider"/> is <see langword="null"/>.
        /// </exception>
        /// <remarks>
        /// <para>
        /// mTLS PoP (Mutual TLS Proof-of-Possession) token binding, as described in
        /// <see href="https://datatracker.ietf.org/doc/html/rfc8705">RFC 8705</see>,
        /// cryptographically binds access tokens to a specific X.509 certificate. When enabled,
        /// the handler acquires a bound token with the certificate thumbprint in the <c>cnf</c> claim,
        /// creates an mTLS HTTP client with the binding certificate, and sends requests through the mTLS channel.
        /// </para>
        /// <para>
        /// Token binding currently supports only application (app-only) tokens. Set
        /// <see cref="AuthorizationHeaderProviderOptions.RequestAppToken"/> to <see langword="true"/>.
        /// </para>
        /// <para>
        /// Prefer using the <see cref="MicrosoftIdentityHttpClientBuilderExtensions"/> extension methods
        /// to configure this handler through dependency injection rather than instantiating it directly.
        /// </para>
        /// </remarks>
        public MicrosoftIdentityMessageHandler(
            IAuthorizationHeaderProvider headerProvider,
            MicrosoftIdentityMessageHandlerOptions? defaultOptions,
            IMsalMtlsHttpClientFactory? mtlsHttpClientFactory,
            ICredentialsProvider? credentialsProvider,
            ILogger<MicrosoftIdentityMessageHandler>? logger = null)
        {
            _headerProvider = headerProvider ?? throw new ArgumentNullException(nameof(headerProvider));
            _defaultOptions = defaultOptions;
            _credentialsProvider = credentialsProvider;
            _logger = logger;

            // If no factory is provided, create a default that can only handle mTLS.
            // This instance should never need non-mTLS calls as it is attached to an HttpClient instance that is expected to be used for non-mTLS scenarios.
            _mtlsHttpClientFactory = mtlsHttpClientFactory ?? MsalMtlsHttpClientFactory.CreateMtlsOnly(); 

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
            var scopes = options.Scopes ?? [];

            if (!scopes.Any() &&
                !string.Equals(options.ProtocolScheme, Constants.MtlsProtocolScheme, StringComparison.OrdinalIgnoreCase))
            {
                throw new MicrosoftIdentityAuthenticationException(
                    "Authentication scopes must be configured in the options.Scopes property.");
            }

            // Send the request with authentication (handles cert-failure retry internally for mTLS scenarios).
            var response = await SendWithCertRetryAsync(request, options, scopes, cancellationToken).ConfigureAwait(false);

            // Handle WWW-Authenticate challenge if present
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                // Use MSAL's WWW-Authenticate parser to extract claims from challenge headers
                string? challengeClaims = WwwAuthenticateParameters.GetClaimChallengeFromResponseHeaders(response.Headers);

                // Claims-challenge retry is only meaningful for token-bearing protocols
                // (Bearer, MTLS_POP). For pure mTLS there is no token to refresh with the
                // claims, and SendWithCertRetryAsync already retried credential acquisition
                // on this 401, so a second outer retry would just re-invoke GetCredentialAsync
                // (potentially hitting disk or Key Vault) for no benefit.
                bool canRetryWithClaims = !string.IsNullOrEmpty(challengeClaims)
                    && !string.Equals(options.ProtocolScheme, Constants.MtlsProtocolScheme, StringComparison.OrdinalIgnoreCase);

                if (canRetryWithClaims)
                {
                    _logger?.LogInformation(
                        "Received WWW-Authenticate challenge with claims. Attempting token refresh.");

                    // Create a new options instance with the challenge claims
                    var challengeOptions = CreateOptionsWithChallengeClaims(options, challengeClaims);

                    // Clone the original request for retry
                    using var retryRequest = await CloneHttpRequestMessageAsync(request).ConfigureAwait(false);

                    // Attempt to get a new token with the challenge claims.
                    var retryResponse = await SendWithCertRetryAsync(retryRequest, challengeOptions, scopes, cancellationToken).ConfigureAwait(false);

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
                else if (!string.IsNullOrEmpty(challengeClaims))
                {
                    _logger?.LogInformation(
                        "Received 401 with WWW-Authenticate claims challenge on an mTLS-only request; skipping outer claims-challenge retry because there is no token to refresh.");
                }
                else
                {
                    _logger?.LogWarning("Received 401 Unauthorized but no WWW-Authenticate challenge with claims found.");
                }
            }

            return response;
        }

        /// <summary>
        /// Captures the artifacts needed to authenticate and dispatch a single outgoing HTTP request.
        /// </summary>
        /// <remarks>
        /// Produced once per acquisition cycle by <see cref="AcquireAuthArtifactsAsync"/> and consumed
        /// by <see cref="SendOnceAsync"/>. Bundling them lets <see cref="SendWithCertRetryAsync"/>
        /// orchestrate the bounded retry without re-deriving state across attempts.
        /// </remarks>
        private readonly struct AuthArtifacts
        {
            public AuthArtifacts(
                string? authHeader,
                X509Certificate2? bindingCertificate,
                CredentialSourceLoaderParameters? loaderParameters,
                CredentialDescription? credentialDescription)
            {
                AuthHeader = authHeader;
                BindingCertificate = bindingCertificate;
                LoaderParameters = loaderParameters;
                CredentialDescription = credentialDescription;
            }

            public string? AuthHeader { get; }

            public X509Certificate2? BindingCertificate { get; }

            public CredentialSourceLoaderParameters? LoaderParameters { get; }

            public CredentialDescription? CredentialDescription { get; }
        }

        /// <summary>
        /// Acquires the authorization header and/or binding certificate required to dispatch a request.
        /// </summary>
        /// <param name="options">The authentication options to use.</param>
        /// <param name="scopes">The scopes for token acquisition.</param>
        /// <param name="cancellationToken">A cancellation token to cancel operation.</param>
        /// <returns>The authentication artifacts to apply to the outgoing request.</returns>
        /// <exception cref="MicrosoftIdentityAuthenticationException">Thrown when token acquisition fails.</exception>
        private async Task<AuthArtifacts> AcquireAuthArtifactsAsync(
            MicrosoftIdentityMessageHandlerOptions options,
            IList<string> scopes,
            CancellationToken cancellationToken)
        {
            try
            {
                // mTLS PoP (Proof-of-Possession) token binding: bound bearer header + binding certificate.
                // Prefer the IAuthorizationHeaderProvider2 surface (Abstractions 12.3.0+); fall back to the
                // legacy IBoundAuthorizationHeaderProvider for custom providers that haven't been updated yet.
                bool isTokenBinding = string.Equals(options.ProtocolScheme, Constants.TokenBindingProtocolScheme, StringComparison.OrdinalIgnoreCase);
                if (isTokenBinding
                    && _headerProvider is IAuthorizationHeaderProvider2 boundProviderV2)
                {
                    var downstreamApiOptions = CreateDownstreamApiOptions(options, scopes);
                    var boundResult = await boundProviderV2.CreateAuthorizationHeaderInformationAsync(
                        downstreamApiOptions.Scopes ?? Enumerable.Empty<string>(),
                        downstreamApiOptions,
                        claimsPrincipal: null,
                        cancellationToken).ConfigureAwait(false);

                    if (!boundResult.Succeeded)
                    {
                        throw new MicrosoftIdentityAuthenticationException(
                            "Failed to acquire bound authorization header for mTLS PoP.");
                    }

                    return new AuthArtifacts(
                        authHeader: boundResult.Result?.AuthorizationHeaderValue!,
                        bindingCertificate: boundResult.Result?.BindingCertificate,
                        loaderParameters: null,
                        credentialDescription: null);
                }

                // for backwards compatibility.
                else if (isTokenBinding
                    && _headerProvider is IBoundAuthorizationHeaderProvider boundProvider)
                {
                    var downstreamApiOptions = CreateDownstreamApiOptions(options, scopes);
                    var boundResult = await boundProvider.CreateBoundAuthorizationHeaderAsync(
                        downstreamApiOptions, claimsPrincipal: null, cancellationToken).ConfigureAwait(false);

                    if (!boundResult.Succeeded)
                    {
                        throw new MicrosoftIdentityAuthenticationException(
                            "Failed to acquire bound authorization header for mTLS PoP.");
                    }

                    return new AuthArtifacts(
                        authHeader: boundResult.Result?.AuthorizationHeaderValue!,
                        bindingCertificate: boundResult.Result?.BindingCertificate,
                        loaderParameters: null,
                        credentialDescription: null);
                }

                // Pure mTLS: client certificate only, no bearer header.
                if (string.Equals(options.ProtocolScheme, Constants.MtlsProtocolScheme, StringComparison.OrdinalIgnoreCase))
                {
                    if (_credentialsProvider is null)
                    {
                        throw new InvalidOperationException("mTLS authentication requires a Credentials Provider object to be registered, but no such service was found. See aka.ms/idweb/mtls for details.");
                    }

                    // Authority and Client ID are not used in mTLS authentication, so set them to empty strings.
                    // In the future, setting them to the correct values would be useful in case the loader wants to log these values for diagnostic purposes.
                    // However, they are very tricky to obtain correctly in this layer of code.
                    var loaderParameters = new CredentialSourceLoaderParameters(string.Empty, string.Empty)
                    {
                        ApiUrl = options.GetApiUrl(),
                        Protocol = Constants.MtlsProtocolScheme,
                    };

                    var credentialDescription = await _credentialsProvider.GetCredentialAsync(
                        loaderParameters,
                        cancellationToken);

                    if (credentialDescription == null || credentialDescription.Certificate == null)
                    {
                        throw new InvalidOperationException("mTLS authentication requires a certificate, but no certificate was found. See aka.ms/idweb/mtls for details.");
                    }

                    return new AuthArtifacts(
                        authHeader: null,
                        bindingCertificate: credentialDescription.Certificate,
                        loaderParameters: loaderParameters,
                        credentialDescription: credentialDescription);
                }

                // Standard bearer token path.
                var authHeader = await _headerProvider.CreateAuthorizationHeaderAsync(
                    scopes, options, cancellationToken: cancellationToken).ConfigureAwait(false);
                return new AuthArtifacts(
                    authHeader: authHeader,
                    bindingCertificate: null,
                    loaderParameters: null,
                    credentialDescription: null);
            }
            catch (Exception ex) when (ex is not MicrosoftIdentityAuthenticationException)
            {
                var message = "Failed to acquire authorization artifacts (header and/or mTLS certificate).";
                _logger?.LogError(ex, message);
                throw new MicrosoftIdentityAuthenticationException(message, ex);
            }
        }

        /// <summary>
        /// Applies the supplied <see cref="AuthArtifacts"/> to <paramref name="request"/> and dispatches it
        /// either through the mTLS-configured HTTP client (when a binding certificate is present) or through
        /// the base <see cref="DelegatingHandler"/> pipeline.
        /// </summary>
        /// <param name="request">The HTTP request message.</param>
        /// <param name="scopes">The scopes the request is authenticated for (used for logging only).</param>
        /// <param name="artifacts">The authentication artifacts to apply.</param>
        /// <param name="cancellationToken">A cancellation token to cancel operation.</param>
        /// <returns>The HTTP response message.</returns>
        private async Task<HttpResponseMessage> SendOnceAsync(
            HttpRequestMessage request,
            IList<string> scopes,
            AuthArtifacts artifacts,
            CancellationToken cancellationToken)
        {
            // Remove any pre-existing Authorization header so caller-supplied values and prior
            // attempts cannot leak into this send.
            if (request.Headers.Contains("Authorization"))
            {
                request.Headers.Remove("Authorization");
            }

            if (artifacts.AuthHeader != null)
            {
                request.Headers.Add("Authorization", artifacts.AuthHeader);

                _logger?.LogDebug(
                    "Added Authorization header for scopes: {Scopes}",
                    string.Join(", ", scopes));
            }

            // If a binding certificate is present (mTLS PoP / mTLS), send through the mTLS HTTP client.
            // This bypasses the normal handler pipeline because the underlying HttpClientHandler
            // must be configured with the client certificate for mutual TLS authentication.
            // We must clone the request because HttpRequestMessage cannot be sent twice, and the
            // mTLS client may be invoked again on retry.
            if (artifacts.BindingCertificate is not null)
            {
                if (_mtlsHttpClientFactory is null)
                {
                    throw new InvalidOperationException("Authentication using mTLS requires a MtlsHttpClientFactory object to be registered, but no such service was found. See aka.ms/idweb/mtls for details.");
                }

                var mtlsClient = _mtlsHttpClientFactory.GetHttpClient(artifacts.BindingCertificate);
                using var mtlsRequest = await CloneHttpRequestMessageAsync(request).ConfigureAwait(false);

                // Re-add the Authorization header that CloneHttpRequestMessageAsync intentionally
                // skips (for retry scenarios). The mTLS clone needs the auth header we just set.
                if (request.Headers.Contains("Authorization"))
                {
                    mtlsRequest.Headers.TryAddWithoutValidation("Authorization", request.Headers.GetValues("Authorization"));
                }

                return await mtlsClient.SendAsync(mtlsRequest, cancellationToken).ConfigureAwait(false);
            }

            // Send the request through the normal handler pipeline.
            return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Sends an HTTP request with authentication, applying a bounded retry policy on
        /// certificate-related auth failures.
        /// </summary>
        /// <param name="request">The HTTP request message.</param>
        /// <param name="options">The authentication options to use.</param>
        /// <param name="scopes">The scopes for token acquisition.</param>
        /// <param name="cancellationToken">A cancellation token to cancel operation.</param>
        /// <returns>The HTTP response message.</returns>
        /// <exception cref="MicrosoftIdentityAuthenticationException">Thrown when token acquisition fails.</exception>
        /// <remarks>
        /// <para>
        /// When token binding (mTLS PoP) is configured via <see cref="AuthorizationHeaderProviderOptions.ProtocolScheme"/>,
        /// uses <see cref="IAuthorizationHeaderProvider2"/> (or, as a fallback, the legacy
        /// <see cref="IBoundAuthorizationHeaderProvider"/>) to acquire a bound token and sends the request
        /// through an mTLS-configured HTTP client with the binding certificate.
        /// </para>
        /// <para>
        /// For pure mTLS scenarios, the <see cref="ICredentialsProvider"/> is notified of certificate
        /// usage on every send. On an auth-related failure, the method attempts a single bounded retry
        /// with freshly-acquired credentials, giving the provider an opportunity to rotate the
        /// certificate. Non-mTLS scenarios are not retried here (claims-challenge retries are
        /// orchestrated by <see cref="SendAsync"/>).
        /// </para>
        /// </remarks>
        private async Task<HttpResponseMessage> SendWithCertRetryAsync(
            HttpRequestMessage request,
            MicrosoftIdentityMessageHandlerOptions options,
            IList<string> scopes,
            CancellationToken cancellationToken)
        {
            const int MaxAttempts = 2;
            HttpRequestMessage currentRequest = request;
            HttpRequestMessage? clonedRequest = null;

            try
            {
                for (int attempt = 1; attempt <= MaxAttempts; attempt++)
                {
                    var artifacts = await AcquireAuthArtifactsAsync(options, scopes, cancellationToken).ConfigureAwait(false);

                    var response = await SendOnceAsync(currentRequest, scopes, artifacts, cancellationToken).ConfigureAwait(false);

                    // Cert-usage notifications and the cert-failure retry policy only apply to pure mTLS,
                    // which is the only path that produces both a CredentialDescription and a BindingCertificate.
                    if (_credentialsProvider is null
                        || artifacts.CredentialDescription is null
                        || artifacts.BindingCertificate is null)
                    {
                        return response;
                    }

                    if (response.IsSuccessStatusCode)
                    {
                        _credentialsProvider.NotifyCertificateUsed(
                            artifacts.LoaderParameters,
                            artifacts.CredentialDescription,
                            artifacts.BindingCertificate,
                            true,
                            null);
                        return response;
                    }

                    if (!Constants.AuthFailureHttpStatusCodes.Contains(response.StatusCode))
                    {
                        // Non-auth failure: don't blame the certificate, don't retry.
                        return response;
                    }

                    // Auth-related failure: notify so the credentials provider can rotate the certificate.
                    _credentialsProvider.NotifyCertificateUsed(
                        artifacts.LoaderParameters,
                        artifacts.CredentialDescription,
                        artifacts.BindingCertificate,
                        false,
                        new UnauthorizedHttpRequestException($"Response has status {response.StatusCode} - {response.ReasonPhrase}"));

                    if (attempt == MaxAttempts)
                    {
                        // Bounded loop exhausted: surface the failed response to the caller.
                        return response;
                    }

                    // Retry: discard the failed response and resend with a fresh clone, since the
                    // original HttpRequestMessage cannot be sent twice.
                    response.Dispose();
                    clonedRequest?.Dispose();
                    clonedRequest = await CloneHttpRequestMessageAsync(request).ConfigureAwait(false);
                    currentRequest = clonedRequest;
                }

                // Unreachable: the loop above always returns within MaxAttempts iterations.
                throw new InvalidOperationException("SendWithCertRetryAsync exited the bounded retry loop without returning a response.");
            }
            finally
            {
                clonedRequest?.Dispose();
            }
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
            var challengeOptions = new MicrosoftIdentityMessageHandlerOptions(originalOptions);

            // Set challenge claims and force refresh
            if (challengeOptions.AcquireTokenOptions != null)
            {
                challengeOptions.AcquireTokenOptions.Claims = challengeClaims;
                challengeOptions.AcquireTokenOptions.ForceRefresh = true;
            }
            else
            {
                challengeOptions.AcquireTokenOptions = new AcquireTokenOptions
                {
                    Claims = challengeClaims,
                    ForceRefresh = true
                };
            }

            return challengeOptions;
        }

        /// <summary>
        /// Creates a <see cref="DownstreamApiOptions"/> from <see cref="AuthorizationHeaderProviderOptions"/>
        /// (the common base class shared by both <see cref="DownstreamApiOptions"/> and
        /// <see cref="MicrosoftIdentityMessageHandlerOptions"/>) for use with
        /// <see cref="IAuthorizationHeaderProvider2.CreateAuthorizationHeaderInformationAsync"/>
        /// (or, as a fallback, the legacy
        /// <see cref="IBoundAuthorizationHeaderProvider.CreateBoundAuthorizationHeaderAsync"/>).
        /// </summary>
        /// <param name="options">The authorization header provider options (common base class).</param>
        /// <param name="scopes">The scopes for token acquisition.</param>
        /// <returns>A <see cref="DownstreamApiOptions"/> instance.</returns>
        private static DownstreamApiOptions CreateDownstreamApiOptions(
            AuthorizationHeaderProviderOptions options, IEnumerable<string> scopes)
        {
            return new DownstreamApiOptions
            {
                // AuthorizationHeaderProviderOptions base properties
                BaseUrl = options.BaseUrl,
                RelativePath = options.RelativePath,
                HttpMethod = options.HttpMethod,
                CustomizeHttpRequestMessage = options.CustomizeHttpRequestMessage,
                AcquireTokenOptions = options.AcquireTokenOptions,
                ProtocolScheme = options.ProtocolScheme,
                RequestAppToken = options.RequestAppToken,
                // DownstreamApiOptions-specific property
                Scopes = scopes,
            };
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
