// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Abstractions;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Extension methods for <see cref="IHttpClientBuilder"/> to add <see cref="MicrosoftIdentityMessageHandler"/>
    /// to the HTTP client pipeline with various configuration options.
    /// </summary>
    /// <remarks>
    /// <para>
    /// These extension methods provide a convenient way to configure HttpClient instances with automatic
    /// Microsoft Identity authentication using <see cref="MicrosoftIdentityMessageHandler"/>. The handler
    /// will automatically add authorization headers to outgoing HTTP requests based on the configured options.
    /// </para>
    /// <para>
    /// Four overloads are provided to support different configuration scenarios:
    /// </para>
    /// <list type="bullet">
    /// <item><description>Parameterless: For scenarios where options are set per-request</description></item>
    /// <item><description>Options instance: For programmatic configuration with a pre-built options object</description></item>
    /// <item><description>Action delegate: For inline configuration using a configuration delegate</description></item>
    /// <item><description>IConfiguration: For configuration from appsettings.json or other configuration sources</description></item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <para><strong>Basic usage with inline configuration:</strong></para>
    /// <code>
    /// services.AddHttpClient("MyApiClient", client =>
    /// {
    ///     client.BaseAddress = new Uri("https://api.example.com");
    /// })
    /// .AddMicrosoftIdentityMessageHandler(options =>
    /// {
    ///     options.Scopes.Add("https://api.example.com/.default");
    /// });
    /// </code>
    /// 
    /// <para><strong>Configuration from appsettings.json:</strong></para>
    /// <code>
    /// // In appsettings.json:
    /// // {
    /// //   "DownstreamApi": {
    /// //     "Scopes": ["https://graph.microsoft.com/.default"]
    /// //   }
    /// // }
    /// 
    /// services.AddHttpClient("GraphClient")
    ///     .AddMicrosoftIdentityMessageHandler(
    ///         configuration.GetSection("DownstreamApi"),
    ///         "DownstreamApi");
    /// </code>
    /// 
    /// <para><strong>Parameterless for per-request configuration:</strong></para>
    /// <code>
    /// services.AddHttpClient("FlexibleClient")
    ///     .AddMicrosoftIdentityMessageHandler();
    /// 
    /// // Later, in a service:
    /// var request = new HttpRequestMessage(HttpMethod.Get, "/api/data")
    ///     .WithAuthenticationOptions(options =>
    ///     {
    ///         options.Scopes.Add("custom.scope");
    ///     });
    /// var response = await httpClient.SendAsync(request);
    /// </code>
    /// </example>
    /// <seealso cref="MicrosoftIdentityMessageHandler"/>
    /// <seealso cref="MicrosoftIdentityMessageHandlerOptions"/>
    /// <seealso cref="HttpRequestMessageAuthenticationExtensions"/>
    public static class MicrosoftIdentityHttpClientBuilderExtensions
    {
        /// <summary>
        /// Adds a <see cref="MicrosoftIdentityMessageHandler"/> to the HTTP client pipeline with no default options.
        /// Options must be configured per-request using <see cref="HttpRequestMessageAuthenticationExtensions.WithAuthenticationOptions(System.Net.Http.HttpRequestMessage, MicrosoftIdentityMessageHandlerOptions)"/>.
        /// </summary>
        /// <param name="builder">The <see cref="IHttpClientBuilder"/> to configure.</param>
        /// <returns>The <see cref="IHttpClientBuilder"/> for method chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> is <see langword="null"/>.</exception>
        /// <remarks>
        /// <para>
        /// This overload is useful when you need maximum flexibility to configure authentication options
        /// on a per-request basis. Since no default options are provided, every request must include
        /// authentication options via the <c>WithAuthenticationOptions</c> extension method.
        /// </para>
        /// <para>
        /// The handler will resolve <see cref="IAuthorizationHeaderProvider"/> from the service provider
        /// at runtime to acquire authorization headers for outgoing requests.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// // Configure the HTTP client
        /// services.AddHttpClient("ApiClient")
        ///     .AddMicrosoftIdentityMessageHandler();
        /// 
        /// // Use the client with per-request configuration
        /// public class MyService
        /// {
        ///     private readonly HttpClient _httpClient;
        ///     
        ///     public MyService(IHttpClientFactory factory)
        ///     {
        ///         _httpClient = factory.CreateClient("ApiClient");
        ///     }
        ///     
        ///     public async Task&lt;string&gt; GetDataAsync()
        ///     {
        ///         var request = new HttpRequestMessage(HttpMethod.Get, "/api/data")
        ///             .WithAuthenticationOptions(options =>
        ///             {
        ///                 options.Scopes.Add("https://api.example.com/.default");
        ///             });
        ///         
        ///         var response = await _httpClient.SendAsync(request);
        ///         response.EnsureSuccessStatusCode();
        ///         return await response.Content.ReadAsStringAsync();
        ///     }
        /// }
        /// </code>
        /// </example>
        public static IHttpClientBuilder AddMicrosoftIdentityMessageHandler(
            this IHttpClientBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return builder.AddHttpMessageHandler(sp =>
            {
                var headerProvider = sp.GetRequiredService<IAuthorizationHeaderProvider>();
                return new MicrosoftIdentityMessageHandler(headerProvider);
            });
        }

        /// <summary>
        /// Adds a <see cref="MicrosoftIdentityMessageHandler"/> to the HTTP client pipeline with the specified options.
        /// </summary>
        /// <param name="builder">The <see cref="IHttpClientBuilder"/> to configure.</param>
        /// <param name="options">The authentication options to use for all requests made by this client.</param>
        /// <returns>The <see cref="IHttpClientBuilder"/> for method chaining.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="builder"/> or <paramref name="options"/> is <see langword="null"/>.
        /// </exception>
        /// <remarks>
        /// <para>
        /// This overload is useful when you have a pre-configured <see cref="MicrosoftIdentityMessageHandlerOptions"/>
        /// instance that should be used for all requests made by this HTTP client. Individual requests can still
        /// override these default options using the per-request extension methods.
        /// </para>
        /// <para>
        /// The handler will resolve <see cref="IAuthorizationHeaderProvider"/> from the service provider
        /// at runtime to acquire authorization headers for outgoing requests.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// // Pre-configure options
        /// var options = new MicrosoftIdentityMessageHandlerOptions
        /// {
        ///     Scopes = { "https://graph.microsoft.com/.default" }
        /// };
        /// options.WithAgentIdentity("agent-application-id");
        /// 
        /// // Configure the HTTP client with the pre-built options
        /// services.AddHttpClient("GraphClient", client =>
        /// {
        ///     client.BaseAddress = new Uri("https://graph.microsoft.com");
        /// })
        /// .AddMicrosoftIdentityMessageHandler(options);
        /// 
        /// // Use the client - authentication is automatic
        /// public class GraphService
        /// {
        ///     private readonly HttpClient _httpClient;
        ///     
        ///     public GraphService(IHttpClientFactory factory)
        ///     {
        ///         _httpClient = factory.CreateClient("GraphClient");
        ///     }
        ///     
        ///     public async Task&lt;string&gt; GetUserProfileAsync()
        ///     {
        ///         var response = await _httpClient.GetAsync("/v1.0/me");
        ///         response.EnsureSuccessStatusCode();
        ///         return await response.Content.ReadAsStringAsync();
        ///     }
        /// }
        /// </code>
        /// </example>
        public static IHttpClientBuilder AddMicrosoftIdentityMessageHandler(
            this IHttpClientBuilder builder,
            MicrosoftIdentityMessageHandlerOptions options)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            return builder.AddHttpMessageHandler(sp =>
            {
                var headerProvider = sp.GetRequiredService<IAuthorizationHeaderProvider>();
                return new MicrosoftIdentityMessageHandler(headerProvider, options);
            });
        }

        /// <summary>
        /// Adds a <see cref="MicrosoftIdentityMessageHandler"/> to the HTTP client pipeline with options configured via delegate.
        /// </summary>
        /// <param name="builder">The <see cref="IHttpClientBuilder"/> to configure.</param>
        /// <param name="configureOptions">A delegate to configure the authentication options.</param>
        /// <returns>The <see cref="IHttpClientBuilder"/> for method chaining.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="builder"/> or <paramref name="configureOptions"/> is <see langword="null"/>.
        /// </exception>
        /// <remarks>
        /// <para>
        /// This overload is useful for inline configuration of authentication options. The delegate is called
        /// once during service configuration to create the default options for the HTTP client.
        /// Individual requests can still override these default options using the per-request extension methods.
        /// </para>
        /// <para>
        /// The handler will resolve <see cref="IAuthorizationHeaderProvider"/> from the service provider
        /// at runtime to acquire authorization headers for outgoing requests.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// // Configure the HTTP client with inline options configuration
        /// services.AddHttpClient("MyApiClient", client =>
        /// {
        ///     client.BaseAddress = new Uri("https://api.example.com");
        /// })
        /// .AddMicrosoftIdentityMessageHandler(options =>
        /// {
        ///     options.Scopes.Add("https://api.example.com/.default");
        ///     options.RequestAppToken = true;
        /// });
        /// 
        /// // Use the client - authentication is automatic
        /// public class ApiService
        /// {
        ///     private readonly HttpClient _httpClient;
        ///     
        ///     public ApiService(IHttpClientFactory factory)
        ///     {
        ///         _httpClient = factory.CreateClient("MyApiClient");
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
        /// <para><strong>With agent identity:</strong></para>
        /// <code>
        /// services.AddHttpClient("AgentClient")
        ///     .AddMicrosoftIdentityMessageHandler(options =>
        ///     {
        ///         options.Scopes.Add("https://graph.microsoft.com/.default");
        ///         options.WithAgentIdentity("agent-application-id");
        ///         options.RequestAppToken = true;
        ///     });
        /// </code>
        /// </example>
        public static IHttpClientBuilder AddMicrosoftIdentityMessageHandler(
            this IHttpClientBuilder builder,
            Action<MicrosoftIdentityMessageHandlerOptions> configureOptions)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (configureOptions == null)
            {
                throw new ArgumentNullException(nameof(configureOptions));
            }

            return builder.AddHttpMessageHandler(sp =>
            {
                var options = new MicrosoftIdentityMessageHandlerOptions();
                configureOptions(options);

                var headerProvider = sp.GetRequiredService<IAuthorizationHeaderProvider>();
                return new MicrosoftIdentityMessageHandler(headerProvider, options);
            });
        }

        /// <summary>
        /// Adds a <see cref="MicrosoftIdentityMessageHandler"/> to the HTTP client pipeline with options bound from IConfiguration.
        /// </summary>
        /// <param name="builder">The <see cref="IHttpClientBuilder"/> to configure.</param>
        /// <param name="configuration">The configuration section containing the authentication options.</param>
        /// <param name="sectionName">The name of the configuration section (used for diagnostics).</param>
        /// <returns>The <see cref="IHttpClientBuilder"/> for method chaining.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="builder"/>, <paramref name="configuration"/>, or <paramref name="sectionName"/> is <see langword="null"/>.
        /// </exception>
        /// <remarks>
        /// <para>
        /// This overload is useful when you want to configure authentication options from appsettings.json
        /// or other configuration sources. The configuration section is bound to a new 
        /// <see cref="MicrosoftIdentityMessageHandlerOptions"/> instance using standard configuration binding.
        /// Individual requests can still override these default options using the per-request extension methods.
        /// </para>
        /// <para>
        /// The handler will resolve <see cref="IAuthorizationHeaderProvider"/> from the service provider
        /// at runtime to acquire authorization headers for outgoing requests.
        /// </para>
        /// </remarks>
        /// <example>
        /// <para><strong>Configuration in appsettings.json:</strong></para>
        /// <code>
        /// {
        ///   "DownstreamApi": {
        ///     "Scopes": ["https://api.example.com/.default"]
        ///   },
        ///   "GraphApi": {
        ///     "Scopes": ["https://graph.microsoft.com/.default", "User.Read"]
        ///   }
        /// }
        /// </code>
        /// 
        /// <para><strong>Configure the HTTP client:</strong></para>
        /// <code>
        /// // In Program.cs or Startup.cs
        /// services.AddHttpClient("DownstreamApiClient", client =>
        /// {
        ///     client.BaseAddress = new Uri("https://api.example.com");
        /// })
        /// .AddMicrosoftIdentityMessageHandler(
        ///     configuration.GetSection("DownstreamApi"),
        ///     "DownstreamApi");
        /// 
        /// services.AddHttpClient("GraphClient", client =>
        /// {
        ///     client.BaseAddress = new Uri("https://graph.microsoft.com");
        /// })
        /// .AddMicrosoftIdentityMessageHandler(
        ///     configuration.GetSection("GraphApi"),
        ///     "GraphApi");
        /// </code>
        /// 
        /// <para><strong>Use the clients:</strong></para>
        /// <code>
        /// public class MyService
        /// {
        ///     private readonly HttpClient _apiClient;
        ///     private readonly HttpClient _graphClient;
        ///     
        ///     public MyService(IHttpClientFactory factory)
        ///     {
        ///         _apiClient = factory.CreateClient("DownstreamApiClient");
        ///         _graphClient = factory.CreateClient("GraphClient");
        ///     }
        ///     
        ///     public async Task&lt;string&gt; GetApiDataAsync()
        ///     {
        ///         var response = await _apiClient.GetAsync("/api/data");
        ///         response.EnsureSuccessStatusCode();
        ///         return await response.Content.ReadAsStringAsync();
        ///     }
        ///     
        ///     public async Task&lt;string&gt; GetUserProfileAsync()
        ///     {
        ///         var response = await _graphClient.GetAsync("/v1.0/me");
        ///         response.EnsureSuccessStatusCode();
        ///         return await response.Content.ReadAsStringAsync();
        ///     }
        /// }
        /// </code>
        /// </example>
        public static IHttpClientBuilder AddMicrosoftIdentityMessageHandler(
            this IHttpClientBuilder builder,
            IConfiguration configuration,
            string sectionName)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            if (sectionName == null)
            {
                throw new ArgumentNullException(nameof(sectionName));
            }

            return builder.AddHttpMessageHandler(sp =>
            {
                var options = new MicrosoftIdentityMessageHandlerOptions();
                configuration.Bind(options);

                var headerProvider = sp.GetRequiredService<IAuthorizationHeaderProvider>();
                return new MicrosoftIdentityMessageHandler(headerProvider, options);
            });
        }
    }
}
