# Logging and Diagnostics in Microsoft.Identity.Web

This guide explains how to configure and use logging in Microsoft.Identity.Web to troubleshoot authentication and token acquisition issues.

---

## 📋 Table of Contents

- [Overview](#overview)
- [Quick Start](#quick-start)
- [Configuration](#configuration)
- [Log Levels](#log-levels)
- [PII Logging](#pii-logging)
- [Correlation IDs](#correlation-ids)
- [Token Cache Logging](#token-cache-logging)
- [Troubleshooting](#troubleshooting)

---

## Overview

Microsoft.Identity.Web integrates with ASP.NET Core's logging infrastructure, providing visibility into:

- **Authentication flows** - Sign-in, sign-out, token validation
- **Token acquisition** - Token cache hits/misses, MSAL operations
- **Downstream API calls** - HTTP requests, token acquisition for APIs
- **Error conditions** - Exceptions, validation failures

### What Gets Logged?

| Component | Log Source | Purpose |
|-----------|-----------|---------|
| **Microsoft.Identity.Web** | Core authentication logic | Configuration, token acquisition, API calls |
| **MSAL.NET** | `Microsoft.Identity.Client` | Token cache operations, authority validation |
| **IdentityModel** | Token validation | JWT parsing, signature validation, claims extraction |
| **ASP.NET Core Auth** | `Microsoft.AspNetCore.Authentication` | Cookie operations, challenge/forbid actions |

---

## Quick Start

### Minimal Configuration

Add to `appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.Identity": "Information"
    }
  }
}
```

This enables **Information-level** logging for Microsoft.Identity.Web and its dependencies (MSAL.NET, IdentityModel).

### Development Configuration

For detailed diagnostics during development:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.Identity": "Debug",
      "Microsoft.AspNetCore.Authentication": "Information"
    }
  },
  "AzureAd": {
    "EnablePiiLogging": true  // Development only!
  }
}
```

### Production Configuration

For production, minimize log volume while capturing errors:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft": "Warning",
      "Microsoft.Identity": "Warning"
    }
  },
  "AzureAd": {
    "EnablePiiLogging": false  // Never true in production
  }
}
```

---

## Configuration

### Namespace-Based Filtering

Control log verbosity by namespace:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",

      // General Microsoft namespaces
      "Microsoft": "Warning",
      "Microsoft.AspNetCore": "Warning",

      // Identity-specific namespaces
      "Microsoft.Identity": "Information",
      "Microsoft.Identity.Web": "Information",
      "Microsoft.Identity.Client": "Information",

      // ASP.NET Core authentication
      "Microsoft.AspNetCore.Authentication": "Information",
      "Microsoft.AspNetCore.Authentication.JwtBearer": "Information",
      "Microsoft.AspNetCore.Authentication.OpenIdConnect": "Debug",

      // Token validation
      "Microsoft.IdentityModel": "Warning"
    }
  }
}
```

### Disable Specific Logging

To silence noisy components without affecting others:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.Identity.Web": "None",  // Completely disable
      "Microsoft.Identity.Client": "Warning"  // Only errors/warnings
    }
  }
}
```

### Environment-Specific Configuration

Use `appsettings.{Environment}.json` for per-environment settings:

**appsettings.Development.json:**
```json
{
  "Logging": {
    "LogLevel": {
      "Microsoft.Identity": "Debug"
    }
  },
  "AzureAd": {
    "EnablePiiLogging": true
  }
}
```

**appsettings.Production.json:**
```json
{
  "Logging": {
    "LogLevel": {
      "Microsoft.Identity": "Warning"
    }
  },
  "AzureAd": {
    "EnablePiiLogging": false
  }
}
```

---

## Log Levels

### ASP.NET Core Log Levels

| Level | Usage | Volume | Production? |
|-------|-------|--------|-------------|
| **Trace** | Most detailed, every operation | Very High | ❌ No |
| **Debug** | Detailed flow, useful for dev | High | ❌ No |
| **Information** | General flow, key events | Moderate | ⚠️ Selective |
| **Warning** | Unexpected but handled conditions | Low | ✅ Yes |
| **Error** | Errors and exceptions | Very Low | ✅ Yes |
| **Critical** | Unrecoverable failures | Very Low | ✅ Yes |
| **None** | Disable logging | None | ⚠️ Selective |

### MSAL.NET to ASP.NET Core Mapping

| MSAL.NET Level | ASP.NET Core Equivalent | Description |
|----------------|------------------------|-------------|
| `Verbose` | `Debug` or `Trace` | Most detailed messages |
| `Info` | `Information` | Key authentication events |
| `Warning` | `Warning` | Abnormal but handled conditions |
| `Error` | `Error` or `Critical` | Errors and exceptions |

### Recommended Settings by Environment

**Development:**
```json
{
  "Logging": {
    "LogLevel": {
      "Microsoft.Identity": "Debug",
      "Microsoft.Identity.Client": "Information"
    }
  }
}
```

**Staging:**
```json
{
  "Logging": {
    "LogLevel": {
      "Microsoft.Identity": "Information",
      "Microsoft.Identity.Client": "Warning"
    }
  }
}
```

**Production:**
```json
{
  "Logging": {
    "LogLevel": {
      "Microsoft.Identity": "Warning",
      "Microsoft.Identity.Client": "Error"
    }
  }
}
```

---

## PII Logging

### What is PII?

**Personally Identifiable Information (PII)** includes:
- Usernames, email addresses
- Display names
- Object IDs, tenant IDs
- IP addresses
- Token values, claims

### Security Warning

> ⚠️ **WARNING**: You and your application are responsible for complying with all applicable regulatory requirements including those set forth by [GDPR](https://www.microsoft.com/trust-center/privacy/gdpr-overview). Before enabling PII logging, ensure you can safely handle this potentially highly sensitive data.

### Enable PII Logging (Development Only)

**appsettings.Development.json:**
```json
{
  "AzureAd": {
    "EnablePiiLogging": true  // ⚠️ Development/Testing ONLY
  },
  "Logging": {
    "LogLevel": {
      "Microsoft.Identity": "Debug"
    }
  }
}
```

### Programmatic PII Control

For conditional PII logging based on environment:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<MicrosoftIdentityOptions>(options =>
{
    // Only enable PII in Development
    options.EnablePiiLogging = builder.Environment.IsDevelopment();
});
```

### What Changes with PII Enabled?

**Without PII logging:**
```
[Information] Token validation succeeded for user '{hidden}'
[Information] Acquired token from cache for scopes '{hidden}'
```

**With PII enabled:**
```
[Information] Token validation succeeded for user 'john.doe@contoso.com'
[Information] Acquired token from cache for scopes 'user.read api://my-api/.default'
```

### PII Redaction in Logs

When PII logging is disabled, sensitive data is replaced with:
- `{hidden}` - Hides user identifiers
- `{hash:XXXX}` - Shows hash instead of actual value
- `***` - Obscures tokens

---

## Correlation IDs

Correlation IDs trace authentication requests across Microsoft's services, critical for support scenarios.

### What Are Correlation IDs?

A correlation ID is a **GUID** that uniquely identifies an authentication/token acquisition request across:
- Your application
- Microsoft Identity platform
- MSAL.NET library
- Microsoft backend services

### Obtaining Correlation IDs

**Method 1: From AuthenticationResult**

```csharp
using Microsoft.Identity.Web;

public class TodoController : ControllerBase
{
    private readonly ITokenAcquisition _tokenAcquisition;
    private readonly ILogger<TodoController> _logger;

    public TodoController(
        ITokenAcquisition tokenAcquisition,
        ILogger<TodoController> logger)
    {
        _tokenAcquisition = tokenAcquisition;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetTodos()
    {
        var result = await _tokenAcquisition.GetAuthenticationResultForUserAsync(
            new[] { "user.read" });

        _logger.LogInformation(
            "Token acquired. CorrelationId: {CorrelationId}, Source: {TokenSource}",
            result.CorrelationId,
            result.AuthenticationResultMetadata.TokenSource);

        return Ok(result.CorrelationId);
    }
}
```

**Method 2: From MsalServiceException**

```csharp
using Microsoft.Identity.Client;

try
{
    var token = await _tokenAcquisition.GetAccessTokenForUserAsync(
        new[] { "user.read" });
}
catch (MsalServiceException ex)
{
    _logger.LogError(ex,
        "Token acquisition failed. CorrelationId: {CorrelationId}, ErrorCode: {ErrorCode}",
        ex.CorrelationId,
        ex.ErrorCode);

    // Return correlation ID to user for support
    return StatusCode(500, new {
        error = "authentication_failed",
        correlationId = ex.CorrelationId
    });
}
```

**Method 3: Set Custom Correlation ID**

```csharp
[HttpGet("{id}")]
public async Task<IActionResult> GetTodo(int id)
{
    // Use request trace ID as correlation ID
    var correlationId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;

    var todo = await _downstreamApi.GetForUserAsync<Todo>(
        "TodoListService",
        options =>
        {
            options.RelativePath = $"api/todolist/{id}";
            options.TokenAcquisitionOptions = new TokenAcquisitionOptions
            {
                CorrelationId = Guid.Parse(correlationId)
            };
        });

    _logger.LogInformation(
        "Called downstream API. TraceId: {TraceId}, CorrelationId: {CorrelationId}",
        HttpContext.TraceIdentifier,
        correlationId);

    return Ok(todo);
}
```

### Using Correlation IDs for Support

When contacting Microsoft support, provide:

1. **Correlation ID** - From logs or exception
2. **Timestamp** - When the error occurred (UTC)
3. **Tenant ID** - Your Azure AD tenant
4. **Error code** - If applicable (e.g., `AADSTS50058`)

**Example support request:**
```
Subject: Token acquisition failing for user.read scope

Correlation ID: 12345678-1234-1234-1234-123456789012
Timestamp: 2025-01-15 14:32:45 UTC
Tenant ID: contoso.onmicrosoft.com
Error Code: AADSTS50058
```

---

## Token Cache Logging

### Enable Token Cache Diagnostics

For .NET Framework or .NET Core apps using distributed token caches:

```csharp
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Web.TokenCacheProviders;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDistributedTokenCaches();

// Enable detailed token cache logging
builder.Services.AddLogging(configure =>
{
    configure.AddConsole();
    configure.AddDebug();
})
.Configure<LoggerFilterOptions>(options =>
{
    options.MinLevel = LogLevel.Debug;  // Detailed cache operations
});
```

### Token Cache Log Examples

**Cache hit:**
```
[Debug] Token cache: Token found in cache for scopes 'user.read'
[Information] Token source: Cache
```

**Cache miss:**
```
[Debug] Token cache: No token found in cache for scopes 'user.read'
[Information] Token source: IdentityProvider
[Debug] Token cache: Token stored in cache
```

### Distributed Cache Troubleshooting

**Redis cache:**
```csharp
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration["Redis:ConnectionString"];
});

// Enable Redis logging
builder.Services.AddLogging(configure =>
{
    configure.AddFilter("Microsoft.Extensions.Caching", LogLevel.Debug);
});
```

**SQL Server cache:**
```csharp
builder.Services.AddDistributedSqlServerCache(options =>
{
    options.ConnectionString = builder.Configuration["SqlCache:ConnectionString"];
    options.SchemaName = "dbo";
    options.TableName = "TokenCache";
});

// Enable SQL cache logging
builder.Services.AddLogging(configure =>
{
    configure.AddFilter("Microsoft.Extensions.Caching.SqlServer", LogLevel.Information);
});
```

---

## Troubleshooting

### Common Logging Scenarios

#### Scenario 1: Token Validation Failures

**Symptom:** 401 Unauthorized responses

**Enable detailed logging:**
```json
{
  "Logging": {
    "LogLevel": {
      "Microsoft.AspNetCore.Authentication.JwtBearer": "Debug",
      "Microsoft.IdentityModel": "Information"
    }
  }
}
```

**Look for:**
```
[Information] Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerHandler:
  Failed to validate the token.
[Debug] Microsoft.IdentityModel.Tokens: IDX10230: Lifetime validation failed.
  The token is expired.
```

#### Scenario 2: Token Acquisition Failures

**Symptom:** `MsalServiceException` or `MsalUiRequiredException`

**Enable detailed logging:**
```json
{
  "Logging": {
    "LogLevel": {
      "Microsoft.Identity.Web": "Debug",
      "Microsoft.Identity.Client": "Information"
    }
  }
}
```

**Look for:**
```
[Error] Microsoft.Identity.Web: Token acquisition failed.
  ErrorCode: invalid_grant, CorrelationId: {guid}
[Information] Microsoft.Identity.Client: MSAL returned exception:
  AADSTS50058: Silent sign-in failed.
```

#### Scenario 3: Downstream API Call Failures

**Enable detailed logging:**
```json
{
  "Logging": {
    "LogLevel": {
      "Microsoft.Identity.Abstractions": "Debug",
      "System.Net.Http": "Information"
    }
  }
}
```

**Custom logging in controllers:**
```csharp
[HttpGet]
public async Task<IActionResult> GetUserProfile()
{
    try
    {
        _logger.LogInformation("Acquiring token for Microsoft Graph");

        var user = await _downstreamApi.GetForUserAsync<User>(
            "MicrosoftGraph",
            options => options.RelativePath = "me");

        _logger.LogInformation(
            "Successfully retrieved user profile for {UserPrincipalName}",
            user.UserPrincipalName);

        return Ok(user);
    }
    catch (MsalUiRequiredException ex)
    {
        _logger.LogWarning(ex,
            "User interaction required. CorrelationId: {CorrelationId}",
            ex.CorrelationId);
        return Challenge();
    }
    catch (HttpRequestException ex)
    {
        _logger.LogError(ex, "Failed to call Microsoft Graph API");
        return StatusCode(502, "Downstream API error");
    }
}
```

### Interpreting Log Patterns

**Successful authentication flow:**
```
[Info] Authentication scheme OpenIdConnect: Authorization response received
[Debug] Correlation id: {guid}
[Info] Authorization code received
[Info] Token validated successfully
[Info] Authentication succeeded for user: {user}
```

**Consent required:**
```
[Warning] Microsoft.Identity.Web: Incremental consent required
[Info] AADSTS65001: User consent is required for scopes: {scopes}
[Info] Redirecting to consent page
```

**Token refresh:**
```
[Debug] Token expired, attempting silent token refresh
[Info] Token source: IdentityProvider
[Info] Token refreshed successfully
```

### Log Aggregation Best Practices

**Application Insights integration:**
```csharp
using Microsoft.ApplicationInsights.Extensibility;

builder.Services.AddApplicationInsightsTelemetry();

// Enrich telemetry with correlation IDs
builder.Services.AddSingleton<ITelemetryInitializer, CorrelationIdTelemetryInitializer>();
```

**Serilog integration:**
```csharp
using Serilog;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft.Identity", Serilog.Events.LogEventLevel.Debug)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/identity-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();
```

---

## Best Practices

### ✅ Do's

**1. Use structured logging:**
```csharp
_logger.LogInformation(
    "Token acquired for user {UserId} with scopes {Scopes}",
    userId, string.Join(" ", scopes));
```

**2. Log correlation IDs:**
```csharp
_logger.LogError(ex,
    "Operation failed. CorrelationId: {CorrelationId}",
    ex.CorrelationId);
```

**3. Use appropriate log levels:**
```csharp
_logger.LogDebug("Detailed diagnostic info");      // Development
_logger.LogInformation("Key application events");  // Selective production
_logger.LogWarning("Unexpected but handled");      // Production
_logger.LogError(ex, "Operation failed");          // Production
```

**4. Sanitize logs in production:**
```csharp
var sanitizedEmail = environment.IsProduction()
    ? MaskEmail(email)
    : email;
_logger.LogInformation("Processing request for {Email}", sanitizedEmail);
```

### ❌ Don'ts

**1. Don't enable PII in production:**
```csharp
// ❌ Wrong
"EnablePiiLogging": true  // In production config!

// ✅ Correct
"EnablePiiLogging": false
```

**2. Don't log secrets:**
```csharp
// ❌ Wrong
_logger.LogInformation("Token: {Token}", accessToken);

// ✅ Correct
_logger.LogInformation("Token acquired, expires: {ExpiresOn}", expiresOn);
```

**3. Don't use verbose logging in production:**
```csharp
// ❌ Wrong - production appsettings.json
"Microsoft.Identity": "Debug"

// ✅ Correct
"Microsoft.Identity": "Warning"
```

---

## See Also

- **[Customization Guide](customization.md)** - Configure authentication options and event handlers
- **[Authorization Guide](../authentication/authorization.md)** - Troubleshoot scope validation and authorization issues
- **[Token Cache Troubleshooting](../authentication/token-cache/troubleshooting.md)** - Debug token cache issues
- **[Calling Downstream APIs](../calling-downstream-apis/calling-downstream-apis-README.md)** - Troubleshoot API calls and token acquisition

---

## Additional Resources

- [MSAL.NET Logging](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/wiki/logging)
- [ASP.NET Core Logging](https://learn.microsoft.com/aspnet/core/fundamentals/logging/)
- [Application Insights](https://learn.microsoft.com/azure/azure-monitor/app/app-insights-overview)
- [Microsoft Identity Platform Error Codes](https://learn.microsoft.com/azure/active-directory/develop/reference-aadsts-error-codes)

---

**Last Updated:** October 27, 2025
**Microsoft.Identity.Web Version:** 3.14.1+
