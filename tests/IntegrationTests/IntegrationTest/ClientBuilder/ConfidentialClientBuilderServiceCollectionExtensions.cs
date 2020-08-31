

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;

namespace IntegrationTest.ClientBuilder
{
    /// <summary>
    /// Dependency injection extensions for MSAL.
    /// </summary>
    public static class ConfidentialClientBuilderServiceCollectionExtensions
    {
        /// <summary>
        /// Adds Microsoft Identity confidential client instance.
        /// </summary>
        /// <param name="services">Services collection.</param>
        /// <param name="configureClientBuilderOptions">Action to configure the client builder.</param>
        /// <returns>Updated services collection.</returns>
        public static IServiceCollection AddMicrosoftIdentityConfidentialClient(
            this IServiceCollection services,
            Action<ClientApplicationBuilderOptions> configureClientBuilderOptions)
        {
            services.Configure(configureClientBuilderOptions);

            services
                .AddHttpClient(MsalHttpClientFactory.HttpClientFactoryName)
                .SetHandlerLifetime(TimeSpan.FromHours(1));

            services.AddSingleton<MsalHttpClientFactory>();
            services.AddSingleton<MsalTokenCacheHandler>();

            services.AddSingleton<IConfidentialClientApplication>(serviceProvider =>
            {
                ClientApplicationBuilderOptions clientApplicationBuilderOptions = 
                    serviceProvider.GetRequiredService<IOptions<ClientApplicationBuilderOptions>>().Value;
                MsalHttpClientFactory msalHttpClientFactory = serviceProvider.GetRequiredService<MsalHttpClientFactory>();
                MsalTokenCacheHandler tokenCacheHandler = serviceProvider.GetRequiredService<MsalTokenCacheHandler>();
                ILogger<IConfidentialClientApplication> logger = serviceProvider.GetRequiredService<ILogger<IConfidentialClientApplication>>();
                MsalLogger msalLogger = new MsalLogger(logger);

                ConfidentialClientApplicationOptions confidentialClientApplicationOptions = new ConfidentialClientApplicationOptions
                {
                    ClientId = clientApplicationBuilderOptions.ClientId,
                };

                ConfidentialClientApplicationBuilder builder = ConfidentialClientApplicationBuilder
                    .Create(clientApplicationBuilderOptions.ClientId);

                IConfidentialClientApplication confidentialClientApplication = builder
                    .WithClientId(clientApplicationBuilderOptions.ClientId)
                    .WithClientSecret(clientApplicationBuilderOptions.ClientSecret)
                    .WithHttpClientFactory(msalHttpClientFactory)
                    .Build();

                // Attach to distributed cache.
                tokenCacheHandler.RegisterWithMsalClient(confidentialClientApplication.UserTokenCache);
                tokenCacheHandler.RegisterWithMsalClient(confidentialClientApplication.AppTokenCache);

                return confidentialClientApplication;
            });

            return services;
        }
    }
}
