// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Web;
using Microsoft.Graph;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;

#pragma warning disable CA1050 // Declare types in namespaces
public class GraphServiceClientBenchmark
#pragma warning restore CA1050 // Declare types in namespaces
{
    private readonly GraphServiceClient _graphServiceClient;

    public GraphServiceClientBenchmark()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .Build();

        int count = 0; // Initialize count

        // Add necessary services and configurations
        services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
                .AddMicrosoftIdentityWebApp(options =>
                {
                    // Verification of the fix for #2456
                    if (count > 0)
                    {
                        throw new ArgumentException("AddMicrosoftIdentityWebApp(delegate). the delegate" +
                            "is called more than once");
                    }
                    else
                    {
                        count++;
                    }

                    configuration.Bind("AzureAd", options);
                })
                .EnableTokenAcquisitionToCallDownstreamApi()
                .AddMicrosoftGraph(configuration.GetSection("GraphBeta"))
                .AddDownstreamApi("GraphBeta", configuration.GetSection("GraphBeta"))
                .AddInMemoryTokenCaches();

        var serviceProvider = services.BuildServiceProvider();
        _graphServiceClient = serviceProvider.GetRequiredService<GraphServiceClient>();
    }

    [Benchmark]
    public void GetUser()
    {
        _graphServiceClient.Me.ToGetRequestInformation();
    }
}
