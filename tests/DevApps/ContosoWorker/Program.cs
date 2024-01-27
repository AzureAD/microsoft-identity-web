using ContosoWorker;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.TokenCacheProviders.InMemory;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddHostedService<Worker>();

// In a worker, use a singleton token acquisition
builder.Services.AddTokenAcquisition(isTokenAcquisitionSingleton:true)
       .Configure<MicrosoftIdentityApplicationOptions>(builder.Configuration.GetSection("AzureAd"))
       .AddDownstreamApi("MyWebApi",
                         builder.Configuration.GetSection("MyWebApi"))
       .AddInMemoryTokenCaches()
       .AddHttpClient();          

var host = builder.Build();

host.Run();
