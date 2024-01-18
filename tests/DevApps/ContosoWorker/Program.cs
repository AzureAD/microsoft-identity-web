using ContosoWorker;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.TokenCacheProviders.InMemory;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddHostedService<Worker>();

builder.Services.AddTokenAcquisition(isTokenAcquisitionSingleton:true)
       .AddDownstreamApi("MyWebApi",
                         builder.Configuration.GetSection("MyWebApi"))
       .AddInMemoryTokenCaches()
       .Configure<MicrosoftIdentityApplicationOptions>(builder.Configuration.GetSection("AzureAd"))
       .AddHttpClient();          

var host = builder.Build();

host.Run();
