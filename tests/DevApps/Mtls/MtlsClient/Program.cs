// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web;

namespace MtlsSample
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

            var sp = tokenAcquirerFactory.Build();

            Console.WriteLine("Calling web API with pure mTLS...");
            var webApi = sp.GetRequiredService<IDownstreamApi>();
            var result = await webApi.GetForAppAsync<IEnumerable<WeatherForecast>>("WebApi").ConfigureAwait(false);

            Console.WriteLine("Web API result:");
            foreach (var forecast in result!)
            {
                Console.WriteLine($"{forecast.Date}: {forecast.Summary} - {forecast.TemperatureC}C/{forecast.TemperatureF}F");
            }
        }
    }
}
