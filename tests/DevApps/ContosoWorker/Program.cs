using ContosoWorker;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.TokenCacheProviders.InMemory;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddHostedService<Worker>();

// In a worker, use a singleton token acquisition
builder.Services.AddTokenAcquisition(isTokenAcquisitionSingleton: true)

       // Configure the options from the configuration file.
       .Configure<MicrosoftIdentityApplicationOptions>(builder.Configuration.GetSection("AzureAd"))

       // Add a token cache. For other token cache options see https://aka.ms/msal-net-token-cache-serialization
       .AddInMemoryTokenCaches()
       .AddHttpClient();

// If you want to call a downstream API call AddDownstreamApi here, and inject IDownstreamApi in your worker
builder.Services.AddDownstreamApi("MyWebApi",
                                  builder.Configuration.GetSection("MyWebApi"));

 // If you want to call Microsoft Graph, call AddMicrosoftGraph here and inject GraphServiceClient in your worker
 builder.Services.AddMicrosoftGraph(builder.Configuration.GetSection("MyWebApi"));

var host = builder.Build();

host.Run();
