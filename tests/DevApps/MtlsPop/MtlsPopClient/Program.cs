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

            tokenAcquirerFactory.Services.AddMicrosoftGraph();

            var sp = tokenAcquirerFactory.Build();

            Console.WriteLine("Scenario 1: calling web API with mTLS PoP token...");
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
        }
    }
}
