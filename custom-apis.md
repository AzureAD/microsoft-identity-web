# Calling Custom APIs

Approaches: `IDownstreamApi`, `MicrosoftIdentityMessageHandler`, `IAuthorizationHeaderProvider`.

## Overview
| Approach | Complexity | Flexibility | Use Case |
|----------|------------|-------------|----------|
| IDownstreamApi | Low | Medium | Config-driven REST |
| MicrosoftIdentityMessageHandler | Medium | High | HttpClient pipeline |
| IAuthorizationHeaderProvider | High | Very High | Full manual control |

## Unified Registration
```csharp
builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"))
    .EnableTokenAcquisitionToCallDownstreamApi()
    .AddInMemoryTokenCaches();

builder.Services.AddTokenAcquisition();
builder.Services.AddDownstreamApis(builder.Configuration.GetSection("DownstreamApis"));
```

## Route Template Clarification
```csharp
[HttpGet("{id:int}")]
public async Task<IActionResult> Details(int id) =>
    await _api.GetForUserAsync<Product>("MyApi", $"products/{id}");
```

## Query Parameters (Use ExtraQueryParameters)
```csharp
var options = new DownstreamApiOptions
{
    RelativePath = "search",
    ExtraQueryParameters = new Dictionary<string,string>
    {
        ["q"] = query,
        ["page"] = page.ToString(),
        ["pageSize"] = pageSize.ToString()
    }
};
var results = await _api.GetForUserAsync<SearchResults>("MyApi", options);
```

## App vs User Token Switch
```csharp
var appCall = await _api.GetForAppAsync<BatchData>(
    "MyApi",
    new DownstreamApiOptions { RelativePath = "batch/process", RequestAppToken = true });
```

## Resilience (Standard Handler)
```csharp
builder.Services.AddHttpClient("MyApi")
    .AddStandardResilienceHandler(o => o.Retry.MaxRetries = 3);
```

## Headers Example
```csharp
var opts = new DownstreamApiOptions
{
    RelativePath = "data",
    CustomizeHttpRequestMessage = msg =>
        msg.Headers.Add("X-Correlation-Id", HttpContext.TraceIdentifier)
};
var data = await _api.GetForUserAsync<MyData>("MyApi", opts);
```

## Incremental Consent
```csharp
[Authorize]
[AuthorizeForScopes(Scopes = new[]{"api://my-api-client-id/read"})]
public async Task<IActionResult> Index()
{
    var products = await _api.GetForUserAsync<List<Product>>("MyApi", "products");
    return View(products);
}
```

## IAuthorizationHeaderProvider
```csharp
var header = await _headerProvider.CreateAuthorizationHeaderForUserAsync(
    scopes: new[]{"api://my-api-client-id/read"});
```

## OWIN Pattern (Updated)
```csharp
OwinTokenAcquirerFactory factory =
    TokenAcquirerFactory.GetDefaultInstance<OwinTokenAcquirerFactory>();

app.AddMicrosoftIdentityWebApp(factory);
factory.Services
    .AddMicrosoftGraph()
    .AddDownstreamApi("DownstreamAPI", factory.Configuration.GetSection("DownstreamAPI"));
factory.Build();
```

## Related
- Web Apps
- Web APIs
- Graph integration

---
Follow main README for decision tree.