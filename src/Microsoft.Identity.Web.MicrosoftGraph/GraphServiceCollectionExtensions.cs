// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Graph;

namespace Microsoft.Identity.Web
{
    public static class GraphServiceCollectionExtensions
    {
        public static IServiceCollection AddMicrosoftGraph(this IServiceCollection services)
        {
            services.AddTokenAcquisition();
            services.AddHttpClient();
            return services.AddMicrosoftGraph(options => { });
        }

        public static IServiceCollection AddMicrosoftGraph(this IServiceCollection services, Action<MicrosoftGraphOptions> configureMicrosoftGraphOptions)
        {
            // https://docs.microsoft.com/en-us/dotnet/standard/microservices-architecture/implement-resilient-applications/use-httpclientfactory-to-implement-resilient-http-requests
            services.AddOptions<MicrosoftGraphOptions>().Configure(configureMicrosoftGraphOptions);

            services.AddScoped<GraphServiceClient, GraphServiceClient>(serviceProvider =>
            {
                var tokenAquisitionService = serviceProvider.GetRequiredService<ITokenAcquisition>();
                var options = serviceProvider.GetRequiredService<IOptions<MicrosoftGraphOptions>>();

                var microsoftGraphOptions = options.Value;
                if (microsoftGraphOptions.Scopes == null)
                {
                    throw new ArgumentException(IDWebErrorMessage.CalledApiScopesAreNull);
                }

                string graphBaseUrl = microsoftGraphOptions.BaseUrl;
                string[] initialScopes = microsoftGraphOptions.Scopes.Split(' ');

                GraphServiceClient client = string.IsNullOrWhiteSpace(graphBaseUrl) ?
                            new GraphServiceClient(new TokenAcquisitionAuthenticationProvider(
                                tokenAquisitionService,
                                new TokenAcquisitionAuthenticationProviderOption() { Scopes = initialScopes.ToArray() })) :
                            new GraphServiceClient(graphBaseUrl,
                                new TokenAcquisitionAuthenticationProvider(
                                    tokenAquisitionService,
                                    new TokenAcquisitionAuthenticationProviderOption() { Scopes = initialScopes.ToArray() }));
                return client;
            });
            return services;
        }
    }

}
