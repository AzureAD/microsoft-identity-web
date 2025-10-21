<!-- Follow-up PR tracking (temporary; remove before merge):
Addressed: #2434549342 #2434553565 #2434555474 #2434558476 #2436604554 #2446576057 #2446582808 #2446591268
Clarified / Adjusted: #2434550139 (contrast) #2440660142 (route template) #2440662758 (query params) #2440673357 (resilience)
Deferred/Informational: #2442609155 #2442609361 #2442609463
-->
# Calling Downstream APIs with Microsoft.Identity.Web

This guide helps you choose and implement the right approach for calling downstream APIs (Microsoft Graph, Azure services, or custom APIs) from your ASP.NET Core, OWIN, or other .NET applications using Microsoft.Identity.Web.

## 🎯 Choosing the Right Approach

| API Type / Scenario                  | Decision / Criteria                         | Recommended Client/Class                    |
|-------------------------------------|---------------------------------------------|---------------------------------------------|
| Microsoft Graph                     | Need to call Microsoft Graph APIs           | GraphServiceClient                          |
| Azure SDK (Storage, etc.)           | Need Azure SDK auth                         | MicrosoftIdentityTokenCredential + Azure SDK |
| Custom API                          | Simple configurable REST                    | IDownstreamApi                              |
| Custom API                          | HttpClient + handler pipeline               | MicrosoftIdentityMessageHandler             |
| Custom API                          | Full custom control                         | IAuthorizationHeaderProvider                |

## 📊 Comparison Table

| Approach | Best For | Complexity | Configuration | Flexibility |
|----------|----------|-----------|---------------|-------------|
| GraphServiceClient | Microsoft Graph | Low | Simple | Medium |
| MicrosoftIdentityTokenCredential | Azure SDK clients | Low | Simple | Low |
| IDownstreamApi | Standard REST | Low | JSON + Code | Medium |
| MicrosoftIdentityMessageHandler | HttpClient pipeline | Medium | Code | High |
| IAuthorizationHeaderProvider | Custom logic | High | Code | Very High |

## 🔑 Token Acquisition Scenarios (Updated per review)

- Web apps calling web APIs on behalf of users
- Web APIs calling web APIs on behalf of users (OBO)
- Web apps, web APIs, and daemons calling APIs on their own behalf (application permissions)
- Daemon scenarios with a user identity (Agent user identities)

```mermaid
graph LR
    A[Token Acquisition] --> B[Delegated<br/>On behalf of user]
    A --> C[App-Only<br/>Application permissions]
    A --> D[OBO<br/>Web API exchanges user token]
    B --> B1[Web Apps]
    B --> B2[Agent user identities]
    C --> C1[Daemon Apps]
    C --> C2[Web APIs (app perms)]
    D --> D1[Web APIs calling other APIs]
```

### Delegated Permissions (User Tokens)
Scenario: Web app or agent user identity calling downstream API with user context.  
Methods: `CreateAuthorizationHeaderForUserAsync()`, `GetForUserAsync()`

### Application Permissions (App-Only Tokens)
Scenario: Daemon/background process or API acting without user context.  
Methods: `CreateAuthorizationHeaderForAppAsync()`, `GetForAppAsync()`

### On-Behalf-Of (OBO)
Scenario: Web API exchanges incoming user token for downstream token.  
Methods: `GetForUserAsync()` (incoming token utilized automatically)

## 🚀 Quick Start Examples

> All examples assume you have token acquisition and a token cache configured.

### Microsoft Graph
```csharp
builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"))
    .EnableTokenAcquisitionToCallDownstreamApi()
    .AddInMemoryTokenCaches();

builder.Services.AddTokenAcquisition();
builder.Services.AddMicrosoftGraph();

[Authorize]
[AuthorizeForScopes(Scopes = new[] {"User.Read"})]
public class HomeController : Controller
{
    private readonly GraphServiceClient _graphClient;
    public HomeController(GraphServiceClient graphClient) => _graphClient = graphClient;

    public async Task<IActionResult> Profile()
    {
        var user = await _graphClient.Me.GetAsync();
        var users = await _graphClient.Users.GetAsync(r => r.Options.WithAppOnly());
        return View(user);
    }
}
```
[Learn more about Microsoft Graph integration](microsoft-graph.md)

### Azure SDKs
(See updated Azure SDK DI pattern in `azure-sdks.md` using `AddAzureClients`.)
```csharp
builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"))
    .EnableTokenAcquisitionToCallDownstreamApi()
    .AddInMemoryTokenCaches();

builder.Services.AddTokenAcquisition();
builder.Services.AddMicrosoftIdentityAzureTokenCredential();
// Blob, KeyVault examples removed per review.
```
[Learn more about Azure SDK integration](azure-sdks.md)

### IDownstreamApi
```csharp
builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"))
    .EnableTokenAcquisitionToCallDownstreamApi()
    .AddInMemoryTokenCaches();

builder.Services.AddTokenAcquisition();
builder.Services.AddDownstreamApis(builder.Configuration.GetSection("DownstreamApis"));

public class ApiService
{
    private readonly IDownstreamApi _api;
    public ApiService(IDownstreamApi api) => _api = api;

    public Task<Product> GetProductAsync(int id) =>
        _api.GetForUserAsync<Product>("MyApi", $"api/products/{id}");

    public Task<List<Product>> GetAllProductsAsync() =>
        _api.GetForAppAsync<List<Product>>("MyApi", "api/products");
}
```
[Learn more about IDownstreamApi](custom-apis.md)

### MicrosoftIdentityMessageHandler
```csharp
builder.Services.AddHttpClient("MyApiClient", client =>
{
    client.BaseAddress = new Uri("https://myapi.example.com/");
})
.AddHttpMessageHandler(sp => new MicrosoftIdentityMessageHandler(
    sp.GetRequiredService<IAuthorizationHeaderProvider>(),
    new MicrosoftIdentityMessageHandlerOptions{ Scopes = new[] {"api://myapi/.default"} }));
```
[Learn more](custom-apis.md#microsoftidentitymessagehandler)

### IAuthorizationHeaderProvider
```csharp
public class CustomAuthService
{
    private readonly IAuthorizationHeaderProvider _provider;
    public CustomAuthService(IAuthorizationHeaderProvider provider) => _provider = provider;

    public async Task<string> CallApiAsync()
    {
        var authHeader = await _provider.CreateAuthorizationHeaderForUserAsync(new[]{"api://myapi/.default"});
        using var client = new HttpClient();
        client.DefaultRequestHeaders.Add("Authorization", authHeader);
        return await client.GetStringAsync("https://myapi.example.com/data");
    }
}
```
[Learn more](custom-apis.md#iauthorizationheaderprovider)

## 📑 Configuration Patterns

```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "your-tenant-id",
    "ClientId": "your-client-id",
    "ClientCredentials": [ { "SourceType": "SignedAssertionFromManagedIdentity" } ]
  },
  "DownstreamApis": {
    "MicrosoftGraph": { "BaseUrl": "https://graph.microsoft.com/v1.0", "Scopes": ["User.Read"] },
    "MyApi": { "BaseUrl": "https://myapi.example.com", "Scopes": ["api://myapi/read"] }
  }
}
```
[Credentials configuration guide](../authentication/credentials/README.md)

Code-based:
```csharp
builder.Services.Configure<MicrosoftIdentityApplicationOptions>(o =>
{
    o.Instance = "https://login.microsoftonline.com/";
    o.TenantId = "your-tenant-id";
    o.ClientId = "your-client-id";
});
builder.Services.AddDownstreamApis(builder.Configuration.GetSection("DownstreamApis"));
```

Daemon / Console note: Ensure `appsettings.json` is copied to output (Copy if newer).

## 🧪 Scenario Guides
- [From Web Apps](from-web-apps.md)
- [From Web APIs](from-web-apis.md)
- [Daemon applications](../README.md#daemon-applications)

## ⚠️ Error Handling
```csharp
try
{
    var result = await _api.GetForUserAsync<Data>("MyApi", "api/data");
}
catch (MicrosoftIdentityWebChallengeUserException)
{
    // triggers consent/challenge
    throw;
}
catch (HttpRequestException ex)
{
    _logger.LogError(ex, "API call failed");
}
```

| Exception | Meaning | Solution |
|-----------|---------|----------|
| MicrosoftIdentityWebChallengeUserException | User consent required | Redirect (web app) or 401 (API) |
| MsalUiRequiredException | Interactive auth required | Challenge user |
| MsalServiceException | Service/config error | Retry / verify settings |
| HttpRequestException | Downstream API error | Inspect response |

## 🛡️ Resilience (HTTP + Azure)
Use built-in .NET resilience handlers instead of manual retry loops:
```csharp
builder.Services.AddHttpClient("MyApi")
    .AddStandardResilienceHandler(o => o.Retry.MaxRetries = 3);
```
For Azure SDK calls, rely on SDK internal retries or wrap operations with Polly policies if needed.

## 🔗 Related Documentation
- [Credentials Configuration](../authentication/credentials/README.md)
- [Web App Scenarios](../scenarios/web-apps/README.md)
- [Web API Scenarios](../scenarios/web-apis/README.md)
- [Agent Identities](../scenarios/agent-identities/README.md)

## 📦 NuGet Packages
| Package | Purpose | When to Use |
|---------|---------|------------|
| Microsoft.Identity.Web.TokenAcquisition | Token acquisition | Always |
| Microsoft.Identity.Web.DownstreamApi | REST abstraction | Custom APIs |
| Microsoft.Identity.Web.GraphServiceClient | Graph integration | Microsoft Graph |
| Microsoft.Identity.Web.Azure | Azure SDK integration | Azure services |
| Microsoft.Identity.Web | ASP.NET Core web apps/APIs | ASP.NET Core |
| Microsoft.Identity.Web.OWIN | OWIN apps/APIs | OWIN |

## ✅ Next Steps
1. Choose your approach
2. Read scenario guide
3. Configure credentials
4. Implement & test
5. Handle errors gracefully

---
**Version Support**: Microsoft.Identity.Web 3.14.1+ (.NET 8 / .NET 9)