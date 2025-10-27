# Calling Custom APIs

This guide explains the different approaches for calling your own protected APIs using Microsoft.Identity.Web: IDownstreamApi, IAuthorizationHeaderProvider, and MicrosoftIdentityMessageHandler.

## Overview

When calling custom REST APIs, you have three main options depending on your needs:

| Approach | Complexity | Flexibility | Use Case |
|----------|------------|-------------|----------|
| **IDownstreamApi** | Low | Medium | Standard REST APIs with configuration |
| **MicrosoftIdentityMessageHandler** | Medium | High | HttpClient with DI and composable pipeline |
| **IAuthorizationHeaderProvider** | High | Very High | Complete control over HTTP requests |

## IDownstreamApi - Recommended for Most Scenarios

`IDownstreamApi` provides a simple, configuration-driven approach for calling REST APIs with automatic token acquisition.

### Installation

```bash
dotnet add package Microsoft.Identity.Web.DownstreamApi
```

### Configuration

Define your API in appsettings.json:

```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "your-tenant-id",
    "ClientId": "your-client-id",
    "ClientCredentials": [
      {
        "SourceType": "ClientSecret",
        "ClientSecret": "your-client-secret"
      }
    ]
  },
  "DownstreamApis": {
    "MyApi": {
      "BaseUrl": "https://api.example.com",
      "Scopes": ["api://my-api-client-id/read", "api://my-api-client-id/write"],
      "RelativePath": "api/v1",
      "RequestAppToken": false
    },
    "PartnerApi": {
      "BaseUrl": "https://partner.example.com",
      "Scopes": ["api://partner-api-id/.default"],
      "RequestAppToken": true
    }
  }
}
```

### ASP.NET Core Setup

```csharp
using Microsoft.Identity.Web;

var builder = WebApplication.CreateBuilder(args);

// Add authentication
builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"))
    .EnableTokenAcquisitionToCallDownstreamApi()
    .AddInMemoryTokenCaches();

// Register downstream APIs
builder.Services.AddDownstreamApis(
    builder.Configuration.GetSection("DownstreamApis"));

builder.Services.AddControllersWithViews();

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
```

### Basic Usage

```csharp
using Microsoft.Identity.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Authorize]
public class ProductsController : Controller
{
    private readonly IDownstreamApi _api;
    
    public ProductsController(IDownstreamApi api)
    {
        _api = api;
    }
    
    // GET request
    public async Task<IActionResult> Index()
    {
        var products = await _api.GetForUserAsync<List<Product>>(
            "MyApi",
            "products");
        
        return View(products);
    }
    
    // Call downstream API with GET request with query parameters
    public async Task<IActionResult> Details(int id)
    {
        var product = await _api.GetForUserAsync<Product>(
            "MyApi",
            $"products/{id}");
        
        return View(product);
    }
    
    // Call downstream API with POST request
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Product product)
    {
        var created = await _api.PostForUserAsync<Product, Product>(
            "MyApi",
            "products",
            product);
        
        return CreatedAtAction(nameof(Details), new { id = created.Id }, created);
    }
    
    // Call downstream API with PUT request
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] Product product)
    {
        var updated = await _api.PutForUserAsync<Product, Product>(
            "MyApi",
            $"products/{id}",
            product);
        
        return Ok(updated);
    }
    
    // Call downstream API with DELETE request
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _api.DeleteForUserAsync<Product>(
            "MyApi",
            $"products/{id}");
        
        return NoContent();
    }
}
```

### Advanced IDownstreamApi Options

#### Custom Headers and Options

```csharp
public async Task<IActionResult> GetDataWithHeaders()
{
    var options = new DownstreamApiOptions
    {
        CustomizeHttpRequestMessage = message =>
        {
            message.Headers.Add("X-Custom-Header", "MyValue");
            message.Headers.Add("X-Request-Id", Guid.NewGuid().ToString());
            message.Headers.Add("X-Correlation-Id", HttpContext.TraceIdentifier);
        }
    };
    
    var data = await _api.CallApiForUserAsync<MyData>(
        "MyApi",
        options,
        content: null);
    
    return Ok(data);
}
```

#### Override Configuration Per Request

```csharp
public async Task<IActionResult> CallDifferentEndpoint()
{
    var options = new DownstreamApiOptions
    {
        BaseUrl = "https://alternative-api.example.com",
        RelativePath = "v2/data",
        Scopes = new[] { "api://alternative/.default" },
        RequestAppToken = true
    };
    
    var data = await _api.CallApiForAppAsync<MyData>(
        "MyApi",
        options);
    
    return Ok(data);
}
```

#### Query Parameters

```csharp
public async Task<IActionResult> Search(string query, int page, int pageSize)
{
    var options = new DownstreamApiOptions
    {
        RelativePath = $"search?q={Uri.EscapeDataString(query)}&page={page}&pageSize={pageSize}"
    };
    
    var results = await _api.GetForUserAsync<SearchResults>(
        "MyApi",
        options);
    
    return Ok(results);
}
```

You can also use the options.ExtraQueryParameters dictionary.

#### Handling Response Headers

```csharp
public async Task<IActionResult> GetWithHeaders()
{
    var response = await _api.CallApiAsync<MyData>(
        "MyApi",
        options =>
        {
            options.RelativePath = "data";
        });
    
    // Access response headers
    if (response.Headers.TryGetValues("X-RateLimit-Remaining", out var values))
    {
        var remaining = values.FirstOrDefault();
        _logger.LogInformation("Rate limit remaining: {Remaining}", remaining);
    }
    
    return Ok(response.Content);
}
```

### App-Only Tokens with IDownstreamApi

```csharp
[ApiController]
[Route("api/[controller]")]
public class DataController : ControllerBase
{
    private readonly IDownstreamApi _api;
    
    public DataController(IDownstreamApi api)
    {
        _api = api;
    }
    
    [HttpGet("batch")]
    public async Task<ActionResult> GetBatchData()
    {
        // Call with application permissions
        var data = await _api.GetForAppAsync<BatchData>(
            "MyApi",
            "batch/process");
        
        return Ok(data);
    }
}
```

## MicrosoftIdentityMessageHandler - For HttpClient Integration

`MicrosoftIdentityMessageHandler` is a `DelegatingHandler` that adds authentication to HttpClient requests. Use this when you need full HttpClient functionality with automatic token acquisition.

### When to Use

- You need fine-grained control over HTTP requests
- You want to compose multiple message handlers
- You're integrating with existing HttpClient-based code
- You need access to raw HttpResponseMessage

### Configuration

```csharp
using Microsoft.Identity.Web;
using Microsoft.Identity.Abstractions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"))
    .EnableTokenAcquisitionToCallDownstreamApi()
    .AddInMemoryTokenCaches();

// Configure named HttpClient with authentication
builder.Services.AddHttpClient("MyApiClient", client =>
{
    client.BaseAddress = new Uri("https://api.example.com");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
    client.Timeout = TimeSpan.FromSeconds(30);
})
.AddHttpMessageHandler(sp =>
{
    var authProvider = sp.GetRequiredService<IAuthorizationHeaderProvider>();
    return new MicrosoftIdentityMessageHandler(
        authProvider,
        new MicrosoftIdentityMessageHandlerOptions
        {
            Scopes = new[] { "api://my-api-client-id/read" },
            RequestAppToken = false // Use delegated token
        });
});

builder.Services.AddControllersWithViews();

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
```

### Basic Usage

```csharp
using System.Net.Http.Json;

[Authorize]
public class ProductsController : Controller
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ProductsController> _logger;
    
    public ProductsController(
        IHttpClientFactory httpClientFactory,
        ILogger<ProductsController> logger)
    {
        _httpClient = httpClientFactory.CreateClient("MyApiClient");
        _logger = logger;
    }
    
    public async Task<IActionResult> Index()
    {
        // Token automatically added by MicrosoftIdentityMessageHandler
        var response = await _httpClient.GetAsync("api/products");
        response.EnsureSuccessStatusCode();
        
        var products = await response.Content.ReadFromJsonAsync<List<Product>>();
        return View(products);
    }
    
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Product product)
    {
        var response = await _httpClient.PostAsJsonAsync("api/products", product);
        response.EnsureSuccessStatusCode();
        
        var created = await response.Content.ReadFromJsonAsync<Product>();
        return CreatedAtAction(nameof(Details), new { id = created.Id }, created);
    }
}
```

### Per-Request Authentication Options

Override authentication options for specific requests:

```csharp
public async Task<IActionResult> GetAdminData()
{
    var request = new HttpRequestMessage(HttpMethod.Get, "api/admin/data");
    
    // Override authentication options for this request
    request.WithAuthenticationOptions(options =>
    {
        options.Scopes = new[] { "api://my-api/.default" };
        options.RequestAppToken = true; // Use app token instead
    });
    
    var response = await _httpClient.SendAsync(request);
    response.EnsureSuccessStatusCode();
    
    var data = await response.Content.ReadFromJsonAsync<AdminData>();
    return Ok(data);
}
```

### Composing Multiple Message Handlers

```csharp
builder.Services.AddHttpClient("MyApiClient", client =>
{
    client.BaseAddress = new Uri("https://api.example.com");
})
.AddHttpMessageHandler(sp =>
{
    // First handler: Add authentication
    var authProvider = sp.GetRequiredService<IAuthorizationHeaderProvider>();
    return new MicrosoftIdentityMessageHandler(authProvider, new MicrosoftIdentityMessageHandlerOptions
    {
        Scopes = new[] { "api://my-api/read" }
    });
})
.AddHttpMessageHandler<LoggingHandler>() // Second handler: Logging
.AddHttpMessageHandler<RetryHandler>()   // Third handler: Retry logic
.AddPolicyHandler(GetRetryPolicy());      // Polly retry policy
```

### Custom Message Handlers

```csharp
public class LoggingHandler : DelegatingHandler
{
    private readonly ILogger<LoggingHandler> _logger;
    
    public LoggingHandler(ILogger<LoggingHandler> logger)
    {
        _logger = logger;
    }
    
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Request: {Method} {Uri}", request.Method, request.RequestUri);
        
        var response = await base.SendAsync(request, cancellationToken);
        
        _logger.LogInformation("Response: {StatusCode}", response.StatusCode);
        
        return response;
    }
}
```

## IAuthorizationHeaderProvider - Maximum Control

`IAuthorizationHeaderProvider` gives you direct access to authorization headers for complete control over HTTP requests.

### When to Use

- You need complete control over HTTP request construction
- You're integrating with non-standard HTTP APIs
- You need to use HttpClient without DI
- You're building custom HTTP abstractions

### Basic Usage

```csharp
using Microsoft.Identity.Abstractions;

[Authorize]
public class CustomApiController : Controller
{
    private readonly IAuthorizationHeaderProvider _headerProvider;
    private readonly ILogger<CustomApiController> _logger;
    
    public CustomApiController(
        IAuthorizationHeaderProvider headerProvider,
        ILogger<CustomApiController> logger)
    {
        _headerProvider = headerProvider;
        _logger = logger;
    }
    
    public async Task<IActionResult> GetData()
    {
        // Get authorization header (includes "Bearer " prefix)
        var authHeader = await _headerProvider.CreateAuthorizationHeaderForUserAsync(
            scopes: new[] { "api://my-api/read" });
        
        using var client = new HttpClient();
        client.DefaultRequestHeaders.Add("Authorization", authHeader);
        client.DefaultRequestHeaders.Add("X-Custom-Header", "MyValue");
        
        var response = await client.GetAsync("https://api.example.com/data");
        response.EnsureSuccessStatusCode();
        
        var content = await response.Content.ReadAsStringAsync();
        return Content(content, "application/json");
    }
}
```

### App-Only Tokens

```csharp
public async Task<IActionResult> GetBackgroundData()
{
    // Get app-only authorization header
    var authHeader = await _headerProvider.CreateAuthorizationHeaderForAppAsync(
        scopes: new[] { "api://my-api/.default" });
    
    using var client = new HttpClient();
    client.DefaultRequestHeaders.Add("Authorization", authHeader);
    
    var response = await client.GetAsync("https://api.example.com/background");
    var data = await response.Content.ReadFromJsonAsync<BackgroundData>();
    
    return Ok(data);
}
```

### With Custom HTTP Libraries

```csharp
public async Task<IActionResult> CallWithRestSharp()
{
    var authHeader = await _headerProvider.CreateAuthorizationHeaderForUserAsync(
        scopes: new[] { "api://my-api/read" });
    
    // Example with RestSharp
    var client = new RestClient("https://api.example.com");
    var request = new RestRequest("data", Method.Get);
    request.AddHeader("Authorization", authHeader);
    
    var response = await client.ExecuteAsync<MyData>(request);
    
    return Ok(response.Data);
}
```

### Advanced Options

```csharp
public async Task<IActionResult> GetDataWithOptions()
{
    var options = new AuthorizationHeaderProviderOptions
    {
        Scopes = new[] { "api://my-api/read" },
        RequestAppToken = false,
        AcquireTokenOptions = new AcquireTokenOptions
        {
            AuthenticationOptionsName = JwtBearerDefaults.AuthenticationScheme,
            ForceRefresh = false,
            Claims = null
        }
    };
    
    var authHeader = await _headerProvider.CreateAuthorizationHeaderAsync(options);
    
    using var client = new HttpClient();
    client.DefaultRequestHeaders.Add("Authorization", authHeader);
    
    var response = await client.GetAsync("https://api.example.com/data");
    var data = await response.Content.ReadFromJsonAsync<MyData>();
    
    return Ok(data);
}
```

## Comparison and Decision Guide

### Use IDownstreamApi When:

✅ Calling standard REST APIs  
✅ Want configuration-driven approach  
✅ Need automatic serialization/deserialization  
✅ Want minimal code  
✅ Following Microsoft.Identity.Web patterns  

**Example:**
```csharp
var product = await _api.GetForUserAsync<Product>("MyApi", "products/123");
```

### Use MicrosoftIdentityMessageHandler When:

✅ Need full HttpClient capabilities  
✅ Want to compose multiple handlers  
✅ Using HttpClientFactory patterns  
✅ Need access to HttpResponseMessage  
✅ Integrating with existing HttpClient code  

**Example:**
```csharp
var response = await _httpClient.GetAsync("api/products/123");
var product = await response.Content.ReadFromJsonAsync<Product>();
```

### Use IAuthorizationHeaderProvider When:

✅ Need complete control over HTTP requests  
✅ Using custom HTTP libraries  
✅ Building custom abstractions  
✅ Can't use HttpClientFactory  
✅ Need to manually construct requests  

**Example:**
```csharp
var authHeader = await _headerProvider.CreateAuthorizationHeaderForUserAsync(scopes);
client.DefaultRequestHeaders.Add("Authorization", authHeader);
```

## Error Handling

### IDownstreamApi Errors

```csharp
try
{
    var data = await _api.GetForUserAsync<MyData>("MyApi", "data");
}
catch (MicrosoftIdentityWebChallengeUserException ex)
{
    // User needs to consent
    _logger.LogWarning(ex, "Consent required for scopes: {Scopes}", string.Join(", ", ex.Scopes));
    throw; // Let ASP.NET Core handle consent flow
}
catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
{
    return NotFound("Resource not found");
}
catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized)
{
    return Unauthorized("API returned 401");
}
catch (Exception ex)
{
    _logger.LogError(ex, "API call failed");
    return StatusCode(500, "An error occurred");
}
```

### MicrosoftIdentityMessageHandler Errors

```csharp
try
{
    var response = await _httpClient.GetAsync("api/data");
    
    if (!response.IsSuccessStatusCode)
    {
        var error = await response.Content.ReadAsStringAsync();
        _logger.LogError("API returned {StatusCode}: {Error}", response.StatusCode, error);
        return StatusCode((int)response.StatusCode, error);
    }
    
    var data = await response.Content.ReadFromJsonAsync<MyData>();
    return Ok(data);
}
catch (HttpRequestException ex)
{
    _logger.LogError(ex, "HTTP request failed");
    return StatusCode(500, "Failed to call API");
}
```

## Best Practices

### 1. Configure Timeout Values

```csharp
builder.Services.AddDownstreamApi("MyApi", options =>
{
    options.BaseUrl = "https://api.example.com";
    options.HttpClientName = "MyApi";
});

builder.Services.AddHttpClient("MyApi", client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
});
```

### 2. Use Typed Clients

```csharp
public interface IProductApiClient
{
    Task<List<Product>> GetProductsAsync();
    Task<Product> GetProductAsync(int id);
    Task<Product> CreateProductAsync(Product product);
}

public class ProductApiClient : IProductApiClient
{
    private readonly IDownstreamApi _api;
    
    public ProductApiClient(IDownstreamApi api)
    {
        _api = api;
    }
    
    public Task<List<Product>> GetProductsAsync() =>
        _api.GetForUserAsync<List<Product>>("MyApi", "products");
    
    public Task<Product> GetProductAsync(int id) =>
        _api.GetForUserAsync<Product>("MyApi", $"products/{id}");
    
    public Task<Product> CreateProductAsync(Product product) =>
        _api.PostForUserAsync<Product, Product>("MyApi", "products", product);
}

// Register
builder.Services.AddScoped<IProductApiClient, ProductApiClient>();
```

### 3. Log Request Details

```csharp
public async Task<IActionResult> GetDataWithLogging()
{
    _logger.LogInformation("Calling MyApi for data");
    
    var stopwatch = Stopwatch.StartNew();
    
    try
    {
        var data = await _api.GetForUserAsync<MyData>("MyApi", "data");
        
        stopwatch.Stop();
        _logger.LogInformation("API call succeeded in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
        
        return Ok(data);
    }
    catch (Exception ex)
    {
        stopwatch.Stop();
        _logger.LogError(ex, "API call failed after {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
        throw;
    }
}
```

## OWIN Implementation

```csharp
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.OWIN;
using Owin;

public class Startup
{
    public void Configuration(IAppBuilder app)
    {
      OwinTokenAcquirerFactory factory = TokenAcquirerFactory.GetDefaultInstance<OwinTokenAcquirerFactory>();

      app.AddMicrosoftIdentityWebApp(factory);
      factory.Services
        .AddDownstreamApis(factory.Configuration.GetSection("DownstreamAPI"))
        .AddInMemoryTokenCaches();
        factory.Build();
    }
}
```

## Related Documentation

- [IDownstreamApi Reference](../api-reference/idownstreamapi.md)
- [Calling from Web Apps](from-web-apps.md)
- [Calling from Web APIs](from-web-apis.md)
- [Microsoft Graph Integration](microsoft-graph.md)
- [Agent Identities](../scenarios/agent-identities/README.md)

---

**Next Steps**: Review the [main documentation](README.md) for decision tree and comparison of all approaches.
