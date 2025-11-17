# Generic API

## Scenario

- You have a web API written in any language
- Your web API receives a token and wants to validate it, and call downstream web APIs
- Instead of doing it yourself, your web API will call a service (another web API) that will handle all this.

For instance, assuming this generic service is running on https://localhost:7156:
- to call a downstream Web API which is described in the configuration as "Api2", with the Json content: `{ "property": "value"}`

  ```curl
   curl -X 'GET' \
  'https://localhost:7156/AuthorizationHeader?serviceName=Api2' \
  -H 'accept: text/plain'
  -H `Authorization: bearer xxxyyywww`
  ```

- to get an authorization header for the API described in the configuration as "Api2":

  ```curl
  curl -X 'GET' \
  'https://localhost:7156/DownstreamApi?serviceName=Api2&input=%7B%22property%22%3A%20%22value%22%7D' \
  -H 'accept: text/plain'
  -H `Authorization: bearer xxxyyywww`
  ```
 
This article explains how you can implement such a service, using Microsoft.Identity.Web 2.x.

## Generic service

### Appsettings.json

The appsettings.json has several sections. 
- The "AzureAd" section is usual. It contains the ClientId of your web API, and the client credentials for your wwb API.
- The next section, "DownstreamApis", describes the downstream APIs that you want to call:
  - the name of the service
  - and the parameters describing this service to call. The parameters are of type: [DownstreamApiOptions](https://github.com/AzureAD/microsoft-identity-abstractions-for-dotnet/blob/main/src/Microsoft.Identity.Abstractions/DownstreamApi/DownstreamApiOptions.cs). Among the parameters you'll provide, you'll have the URI of the API to call, the scopes, and all the parameters that are needed for the service to authenticate 

```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "22222222-2222-2222-2222-222222222222",
    "ClientId": "11111111-1111-1111-11111111111111111",
    "ClientCredentials": [
      {
      }
    ],
    "Scopes": "access_as_user",
   },

  "DownstreamApis": {
    "Api1": {
      "BaseUrl": "URL",
      "Scopes": "SCOPES"
    },
    "Api2": {
      "BaseUrl": "https://graph.microsoft.com/v1.0",
      "Scopes": "user.read"
    }
  },

  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

### Program.cs

The program.cs is a classical .NET 7 program.cs for a web API with a couple of additions:
- The token acquisition services are added:
  ```csharp
  // Enable the token acquisition
  builder.Services.AddTokenAcquisition();
  builder.Services.AddInMemoryTokenCaches();
  ```

- A few lines of code read dynamically the "DownstreamApis" section of the appsettings.json
  ```csharp
  // Read the web APIs from the appsettings.json
  Dictionary<string, DownstreamApiOptions> downstreamApiOptions = new Dictionary<string, DownstreamApiOptions>();
  builder.Configuration.GetSection("DownstreamApis").Bind(downstreamApiOptions);
  foreach (var options in downstreamApiOptions)
  {
    builder.Services.AddDownstreamApi(options.Key, 
                                      builder.Configuration.GetSection($"DownstreamApis:{options.Key}"));
  }
  ```

There is the full program.cs

```csharp
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.TokenCacheProviders.InMemory;

var builder = WebApplication.CreateBuilder(args);

// Add services to validate the tokens
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));

// Enable the token acquisition
builder.Services.AddTokenAcquisition();
builder.Services.AddInMemoryTokenCaches();

// Read the web APIs from the appsettings.json
Dictionary<string, DownstreamApiOptions> downstreamApiOptions = new Dictionary<string, DownstreamApiOptions>();
builder.Configuration.GetSection("DownstreamApis").Bind(downstreamApiOptions);
foreach (var options in downstreamApiOptions)
{
    builder.Services.AddDownstreamApi(options.Key,
                                      builder.Configuration.GetSection($"DownstreamApis:{options.Key}"));
}

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
```

## Controllers

The web API exposes two controllers:
- DownstreamApi.Get(string serviceName, string input)
- AuthorizationHeader.Get(string serviceName)

### DownstreamApi controller

The downstream API controller expose one method that calls a downstream API. It delegates to the IDownstreamApi.CallApiAsync method, which gets its parameters from the configuration.

```CSharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web.Resource;

namespace webApi.Controllers;

[Authorize]
[ApiController]
[Route("[controller]")]
[RequiredScope(RequiredScopesConfigurationKey = "AzureAd:Scopes")]
public class DownstreamApi : ControllerBase
{
    private readonly ILogger<DownstreamApi> _logger;

    private readonly IDownstreamApi _downstreamApi;

    public DownstreamApi(ILogger<DownstreamApi> logger,
                         IDownstreamApi downstreamApi)
    {
        _logger = logger;
        _downstreamApi = downstreamApi;
    }

    /// <summary>
    /// Call downstream API
    /// </summary>
    /// <param name="serviceName">Name of the service to call. This is the name of the downstream API
    /// options in the appsettings.json file.</param>
    /// <param name="input"></param>
    /// <returns></returns>
    /// <exception cref="HttpRequestException"></exception>
    [HttpGet(Name = "CallDownstreamWebApi")]
    public async Task<string> CallDownstreamWebApi(string serviceName, string input)
    {
        using var response = await _downstreamApi.CallApiAsync(serviceName, 
                                                               content:new StringContent(input)).ConfigureAwait(false);
        if (response.StatusCode == System.Net.HttpStatusCode.OK)
        {
            var apiResult = await response.Content.ReadAsStringAsync()
                                                  .ConfigureAwait(false);
            return apiResult;
        }
        else
        {
            var error = await response.Content.ReadAsStringAsync()
                                              .ConfigureAwait(false);
            throw new HttpRequestException($"Invalid status code in the HttpResponseMessage: {response.StatusCode}: {error}");
        }
    }
}

```

### AuthorizationHeader controller

The AuthorizationHeader controller exposes one endpoint that provides the authorization header to call a downstream web API. This can be "Bearer token" or "Pop token", or more complex protocols.

This endpoint:
- Reads the DownstreamApiOptions from the configuration, based on the serviceName parameter.
- leverages the IAuthenticationHeaderProvider injected interface to compute the authorization header, using the parameters from the configuraation.

The code is the following:

```CSharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web.Resource;

namespace webApi.Controllers;

[Authorize]
[ApiController]
[Route("[controller]")]
[RequiredScope(RequiredScopesConfigurationKey = "AzureAd:Scopes")]
public class AuthorizationHeader : ControllerBase
{
    private readonly ILogger<AuthorizationHeader> _logger;

    private readonly IAuthorizationHeaderProvider _authorizationHeaderProvider;

    private readonly IConfiguration _configuration;

    public AuthorizationHeader(ILogger<AuthorizationHeader> logger,
                                     IAuthorizationHeaderProvider authorizationHeaderProvider,
                                     IConfiguration configuration)
    {
        _logger = logger;
        _authorizationHeaderProvider = authorizationHeaderProvider;
        _configuration = configuration;
    }


    [HttpGet(Name = "GetAuthorizationHeader")]
    public async Task<string> GetAuthorizationHeader(string serviceName)
    {
        Dictionary<string, DownstreamApiOptions> downstreamApiOptions = new Dictionary<string, DownstreamApiOptions>();
        _configuration.GetSection("DownstreamApis").Bind(downstreamApiOptions);

        if (!downstreamApiOptions.ContainsKey(serviceName))
        {
            throw new ArgumentException($"The downstream API {serviceName} is not configured.");
        }

        var serviceOptions = downstreamApiOptions[serviceName];
        if (serviceOptions.RequestAppToken)
        {
            return await _authorizationHeaderProvider.CreateAuthorizationHeaderForAppAsync(serviceOptions.Scopes?.FirstOrDefault()!, serviceOptions);
        }
        else
        {
            return await _authorizationHeaderProvider.CreateAuthorizationHeaderForUserAsync(serviceOptions.Scopes!, serviceOptions);
        }
    }

}

```