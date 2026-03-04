// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web;

namespace MtlsPopSample
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var tokenAcquirerFactory = TokenAcquirerFactory.GetDefaultInstance();

            tokenAcquirerFactory.Services.AddLogging(builder =>
                builder.AddConsole().SetMinimumLevel(LogLevel.Information));

            tokenAcquirerFactory.Services.AddDownstreamApi("WebApi",
                tokenAcquirerFactory.Configuration.GetSection("WebApi"));

            // Register an HttpClient with MicrosoftIdentityMessageHandler for mTLS PoP
            tokenAcquirerFactory.Services.AddHttpClient("MtlsPopHttpClient", client =>
            {
                client.BaseAddress = new Uri(
                    tokenAcquirerFactory.Configuration.GetSection("WebApi")["BaseUrl"]!);
            })
            .AddMicrosoftIdentityMessageHandler(
                tokenAcquirerFactory.Configuration.GetSection("WebApi"),
                "WebApi");

            tokenAcquirerFactory.Services.AddMicrosoftGraph();

            var sp = tokenAcquirerFactory.Build();

            Console.WriteLine("Scenario 1: calling web API with mTLS PoP token via IDownstreamApi...");
            var webApi = sp.GetRequiredService<IDownstreamApi>();
            var result = await webApi.GetForAppAsync<IEnumerable<WeatherForecast>>("WebApi").ConfigureAwait(false);

            Console.WriteLine("Web API result:");
            foreach (var forecast in result!)
            {
                Console.WriteLine($"{forecast.Date}: {forecast.Summary} - {forecast.TemperatureC}C/{forecast.TemperatureF}F");
            }

            Console.WriteLine();
            Console.WriteLine("Scenario 2: Calling Microsoft Graph with Bearer (non mTLS PoP) token...");
            var graphServiceClient = sp.GetRequiredService<GraphServiceClient>();
            var users = await graphServiceClient.Users
                .Request()
                .WithAppOnly()
                .GetAsync();

            Console.WriteLine("Microsoft Graph result:");
            Console.WriteLine($"{users.Count} users");

            Console.WriteLine();
            Console.WriteLine("Scenario 3: calling web API with mTLS PoP token via MicrosoftIdentityMessageHandler...");
            var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
            var httpClient = httpClientFactory.CreateClient("MtlsPopHttpClient");
            var response = await httpClient.GetAsync("WeatherForecast").ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            Console.WriteLine($"HttpClient result: {content}");
        }
    }
}
