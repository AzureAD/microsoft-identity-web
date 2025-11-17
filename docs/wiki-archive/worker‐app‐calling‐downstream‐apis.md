# Support for workers based on  Microsoft.NET.Sdk.Worker

The following sample shows a worker app using Microsoft.Identity.Web: [ContosoWorker](https://github.com/AzureAD/microsoft-identity-web/tree/master/tests/DevApps/ContosoWorker). This is like a daemon application in the sense it calls downstream API on behalf of itself (with application permissions)

To create a worker app calling downstream APIs:
1 Create the work app:

  ```PowerShell
  dotnet new worker
  ```

2. add the authentication in the following way:
   
   2.1. Update the project file to add Microsoft.Identity.Web assemblies `Microsoft.Identity.Web.DownstreamApi` and/or `Microsoft.Identity.Web.GraphServiceClient`

   ```diff
   <Project Sdk="Microsoft.NET.Sdk.Worker">
     <PropertyGroup>
       <TargetFrameworks>net8.0</TargetFrameworks>
       <Nullable>enable</Nullable>
       <ImplicitUsings>enable</ImplicitUsings>
       <UserSecretsId>dotnet-ContosoWorker-23d72fcd-07bd-4a04-8613-52547266b761</UserSecretsId>
     </PropertyGroup>

     <ItemGroup>
       <PackageReference Include="Microsoft.Extensions.Hosting" Version="8.0.0" />
   +   <PackageReference Include="Microsoft.Identity.Web.DownstreamApi" Version="2.17.0" />
   +   <PackageReference Include="Microsoft.Identity.Web.GraphServiceClient" Version="2.17.0" />
     </ItemGroup>
   </Project>
   ```
     
   2.2. Update the appsettings.json file (with the usual format for a daemon app)
        For instance this will call the Microsoft Graph users API on behalf of the app, to get the number of users in the tenant

   ```json
   {
    "AzureAd": {
      "Instance": "https://login.microsoftonline.com/",
      "TenantId": "msidentitysamplestesting.onmicrosoft.com",
      "ClientId": "6af093f3-b445-4b7a-beae-046864468ad6",
      "ClientCredentials": [
        {
          "SourceType": "KeyVault",
          "KeyVaultUrl": "https://webappsapistests.vault.azure.net",
          "KeyVaultCertificateName": "Self-Signed-5-5-22"
        }
      ]
    },

   "MyWebApi": {
    "BaseUrl": "https://graph.microsoft.com/v1.0",
    "RelativePath": "/users",
    "RequestAppToken": true,
    "Scopes": [ "https://graph.microsoft.com/.default" ]
   },

   "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.Hosting.Lifetime": "Information"
     }
    }
   }
   ```


   2.3. Update the Program.cs with the authentication part:

   ```diff
   using ContosoWorker;
   using Microsoft.Identity.Abstractions;
   using Microsoft.Identity.Web;
   using Microsoft.Identity.Web.TokenCacheProviders.InMemory;

   var builder = Host.CreateApplicationBuilder(args);

   builder.Services.AddHostedService<Worker>();

   + // In a worker, use a singleton token acquisition
   + builder.Services.AddTokenAcquisition(isTokenAcquisitionSingleton: true)
   +
   + // Configure the options from the configuration file.
   +    .Configure<MicrosoftIdentityApplicationOptions>(builder.Configuration.GetSection("AzureAd"))
   +
   +   // Add a token cache. For other token cache options see https://aka.ms/msal-net-token-cache-serialization
   +    .AddInMemoryTokenCaches()
   +    .AddHttpClient();
   +
   + // If you want to call a downstream API call AddDownstreamApi here, and inject IDownstreamApi in your worker
   + builder.Services.AddDownstreamApi("MyWebApi",
   +                                   builder.Configuration.GetSection("MyWebApi"));
   +
   + // If you want to call Microsoft Graph, call AddMicrosoftGraph here and inject GraphServiceClient in your worker
   + builder.Services.AddMicrosoftGraph(builder.Configuration.GetSection("MyWebApi"));

   var host = builder.Build();

   host.Run();
   ```

   2.4. Inject and use IDownstreamApi and/or GraphServiceClient from the worker itself:

   ```diff
   + using Microsoft.Identity.Abstractions;

   namespace ContosoWorker;

   public class Worker : BackgroundService
   {
      private readonly ILogger<Worker> _logger;
   +   private readonly IDownstreamApi _downstreamApi;

   -  public Worker(ILogger<Worker> logger)
   +  public Worker(ILogger<Worker> logger, IDownstreamApi downstreamApi)
      {
        _logger = logger;
   +    _downstreamApi = downstreamApi;
      }

      protected override async Task ExecuteAsync(CancellationToken stoppingToken)
      {
        while (!stoppingToken.IsCancellationRequested)
        {
   +        var result = await _downstreamApi.CallApiAsync("MyWebApi");

            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
            }
            await Task.Delay(1000, stoppingToken);
        }
      }
   }
   ```