# Calling Custom APIs with MicrosoftIdentityMessageHandler

This guide explains how to use `MicrosoftIdentityMessageHandler` for HttpClient integration to call custom downstream APIs with automatic Microsoft Identity authentication.

## Table of Contents

- [Overview](#overview)
- [MicrosoftIdentityMessageHandler - For HttpClient Integration](#microsoftidentitymessagehandler---for-httpclient-integration)
  - [Before: Manual Setup](#before-manual-setup)
  - [After: Using Extension Methods](#after-using-extension-methods)
  - [Configuration Examples](#configuration-examples)
- [Per-Request Options](#per-request-options)
- [Advanced Scenarios](#advanced-scenarios)

## Overview

`MicrosoftIdentityMessageHandler` is a `DelegatingHandler` that automatically adds authorization headers to outgoing HTTP requests. The new `AddMicrosoftIdentityMessageHandler` extension methods make it easy to configure HttpClient instances with automatic Microsoft Identity authentication.

## MicrosoftIdentityMessageHandler - For HttpClient Integration

### Before: Manual Setup

Previously, you had to manually configure the message handler:

```csharp
// In Program.cs or Startup.cs
services.AddHttpClient("MyApiClient", client =>
{
    client.BaseAddress = new Uri("https://api.example.com");
})
.AddHttpMessageHandler(serviceProvider => new MicrosoftIdentityMessageHandler(
    serviceProvider.GetRequiredService<IAuthorizationHeaderProvider>(),
    new MicrosoftIdentityMessageHandlerOptions 
    { 
        Scopes = { "https://api.example.com/.default" }
    }));
```

### After: Using Extension Methods

Now you can use the convenient extension methods:

#### 1. Parameterless Overload (Per-Request Configuration)

Use this when you want to configure authentication options on a per-request basis:

```csharp
services.AddHttpClient("FlexibleClient")
    .AddMicrosoftIdentityMessageHandler();

// Later, in a service:
var request = new HttpRequestMessage(HttpMethod.Get, "/api/data")
    .WithAuthenticationOptions(options =>
    {
        options.Scopes.Add("https://api.example.com/.default");
    });

var response = await httpClient.SendAsync(request);
```

#### 2. Options Instance Overload

Use this when you have a pre-configured options object:

```csharp
var options = new MicrosoftIdentityMessageHandlerOptions
{
    Scopes = { "https://graph.microsoft.com/.default" }
};
options.WithAgentIdentity("agent-application-id");

services.AddHttpClient("GraphClient", client =>
{
    client.BaseAddress = new Uri("https://graph.microsoft.com");
})
.AddMicrosoftIdentityMessageHandler(options);
```

#### 3. Action Delegate Overload (Inline Configuration)

Use this for inline configuration - the most common scenario:

```csharp
services.AddHttpClient("MyApiClient", client =>
{
    client.BaseAddress = new Uri("https://api.example.com");
})
.AddMicrosoftIdentityMessageHandler(options =>
{
    options.Scopes.Add("https://api.example.com/.default");
    options.RequestAppToken = true;
});
```

#### 4. IConfiguration Overload (Configuration from appsettings.json)

Use this to configure from appsettings.json:

**appsettings.json:**
```json
{
  "DownstreamApi": {
    "Scopes": ["https://api.example.com/.default"]
  },
  "GraphApi": {
    "Scopes": ["https://graph.microsoft.com/.default", "User.Read"]
  }
}
```

**Program.cs:**
```csharp
services.AddHttpClient("DownstreamApiClient", client =>
{
    client.BaseAddress = new Uri("https://api.example.com");
})
.AddMicrosoftIdentityMessageHandler(
    configuration.GetSection("DownstreamApi"),
    "DownstreamApi");

services.AddHttpClient("GraphClient", client =>
{
    client.BaseAddress = new Uri("https://graph.microsoft.com");
})
.AddMicrosoftIdentityMessageHandler(
    configuration.GetSection("GraphApi"),
    "GraphApi");
```

### Configuration Examples

#### Example 1: Simple Web API Client

```csharp
// Configure in Program.cs
services.AddHttpClient("WeatherApiClient", client =>
{
    client.BaseAddress = new Uri("https://api.weather.com");
})
.AddMicrosoftIdentityMessageHandler(options =>
{
    options.Scopes.Add("https://api.weather.com/.default");
});

// Use in a controller or service
public class WeatherService
{
    private readonly HttpClient _httpClient;
    
    public WeatherService(IHttpClientFactory factory)
    {
        _httpClient = factory.CreateClient("WeatherApiClient");
    }
    
    public async Task<WeatherForecast> GetForecastAsync(string city)
    {
        var response = await _httpClient.GetAsync($"/forecast/{city}");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<WeatherForecast>();
    }
}
```

#### Example 2: Multiple API Clients

```csharp
// Configure multiple clients in Program.cs
services.AddHttpClient("ApiClient1")
    .AddMicrosoftIdentityMessageHandler(options =>
    {
        options.Scopes.Add("https://api1.example.com/.default");
    });

services.AddHttpClient("ApiClient2")
    .AddMicrosoftIdentityMessageHandler(options =>
    {
        options.Scopes.Add("https://api2.example.com/.default");
        options.RequestAppToken = true;
    });

// Use in a service
public class MultiApiService
{
    private readonly HttpClient _client1;
    private readonly HttpClient _client2;
    
    public MultiApiService(IHttpClientFactory factory)
    {
        _client1 = factory.CreateClient("ApiClient1");
        _client2 = factory.CreateClient("ApiClient2");
    }
    
    public async Task<string> GetFromBothApisAsync()
    {
        var data1 = await _client1.GetStringAsync("/data");
        var data2 = await _client2.GetStringAsync("/data");
        return $"{data1} | {data2}";
    }
}
```

#### Example 3: Configuration from appsettings.json with Complex Options

**appsettings.json:**
```json
{
  "DownstreamApis": {
    "CustomerApi": {
      "Scopes": ["api://customer-api/.default"]
    },
    "OrderApi": {
      "Scopes": ["api://order-api/.default"]
    },
    "InventoryApi": {
      "Scopes": ["api://inventory-api/.default"]
    }
  }
}
```

**Program.cs:**
```csharp
var downstreamApis = configuration.GetSection("DownstreamApis");

services.AddHttpClient("CustomerApiClient", client =>
{
    client.BaseAddress = new Uri("https://customer-api.example.com");
})
.AddMicrosoftIdentityMessageHandler(
    downstreamApis.GetSection("CustomerApi"),
    "CustomerApi");

services.AddHttpClient("OrderApiClient", client =>
{
    client.BaseAddress = new Uri("https://order-api.example.com");
})
.AddMicrosoftIdentityMessageHandler(
    downstreamApis.GetSection("OrderApi"),
    "OrderApi");

services.AddHttpClient("InventoryApiClient", client =>
{
    client.BaseAddress = new Uri("https://inventory-api.example.com");
})
.AddMicrosoftIdentityMessageHandler(
    downstreamApis.GetSection("InventoryApi"),
    "InventoryApi");
```

## Per-Request Options

You can override default options on a per-request basis using the `WithAuthenticationOptions` extension method:

```csharp
// Configure client with default options
services.AddHttpClient("ApiClient")
    .AddMicrosoftIdentityMessageHandler(options =>
    {
        options.Scopes.Add("https://api.example.com/.default");
    });

// Override for specific requests
public class MyService
{
    private readonly HttpClient _httpClient;
    
    public MyService(IHttpClientFactory factory)
    {
        _httpClient = factory.CreateClient("ApiClient");
    }
    
    public async Task<string> GetSensitiveDataAsync()
    {
        // Override scopes for this specific request
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/sensitive")
            .WithAuthenticationOptions(options =>
            {
                options.Scopes.Clear();
                options.Scopes.Add("https://api.example.com/sensitive.read");
                options.RequestAppToken = true;
            });
        
        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }
}
```

## Advanced Scenarios

### Agent Identity

Use agent identity when your application needs to act on behalf of another application:

```csharp
services.AddHttpClient("AgentClient")
    .AddMicrosoftIdentityMessageHandler(options =>
    {
        options.Scopes.Add("https://graph.microsoft.com/.default");
        options.WithAgentIdentity("agent-application-id");
        options.RequestAppToken = true;
    });
```

### Composing with Other Handlers

You can chain multiple handlers in the pipeline:

```csharp
services.AddHttpClient("ApiClient")
    .AddMicrosoftIdentityMessageHandler(options =>
    {
        options.Scopes.Add("https://api.example.com/.default");
    })
    .AddHttpMessageHandler<LoggingHandler>()
    .AddHttpMessageHandler<RetryHandler>();
```

### WWW-Authenticate Challenge Handling

`MicrosoftIdentityMessageHandler` automatically handles WWW-Authenticate challenges for Conditional Access scenarios:

```csharp
// No additional code needed - automatic handling
services.AddHttpClient("ProtectedApiClient")
    .AddMicrosoftIdentityMessageHandler(options =>
    {
        options.Scopes.Add("https://api.example.com/.default");
    });

// The handler will automatically:
// 1. Detect 401 responses with WWW-Authenticate challenges
// 2. Extract required claims from the challenge
// 3. Acquire a new token with the additional claims
// 4. Retry the request with the new token
```

### Error Handling

```csharp
public class MyService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<MyService> _logger;
    
    public MyService(IHttpClientFactory factory, ILogger<MyService> logger)
    {
        _httpClient = factory.CreateClient("ApiClient");
        _logger = logger;
    }
    
    public async Task<string> GetDataWithErrorHandlingAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/data");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }
        catch (MicrosoftIdentityAuthenticationException authEx)
        {
            _logger.LogError(authEx, "Authentication failed: {Message}", authEx.Message);
            throw;
        }
        catch (HttpRequestException httpEx)
        {
            _logger.LogError(httpEx, "HTTP request failed: {Message}", httpEx.Message);
            throw;
        }
    }
}
```

## Summary

The `AddMicrosoftIdentityMessageHandler` extension methods provide a clean, flexible way to configure HttpClient with automatic Microsoft Identity authentication:

- **Parameterless**: For per-request configuration flexibility
- **Options instance**: For pre-configured options objects
- **Action delegate**: For inline configuration (most common)
- **IConfiguration**: For configuration from appsettings.json

Choose the overload that best fits your scenario and enjoy automatic authentication for your downstream API calls!
