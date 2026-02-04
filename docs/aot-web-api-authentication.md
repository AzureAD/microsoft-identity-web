# AOT-Compatible Web API Authentication

## Overview

This document describes the AOT-compatible Web API authentication overloads introduced in .NET 10+. These new methods provide a pathway for using Microsoft Identity Web in Native AOT scenarios while maintaining full compatibility with token acquisition and On-Behalf-Of (OBO) flows.

## Key Features

- ✅ **AOT-Compatible**: No reflection-based configuration binding at runtime when used programmatically
- ✅ **OBO Support**: Works seamlessly with `ITokenAcquisition` without requiring `EnableTokenAcquisitionToCallDownstreamApi()`
- ✅ **Customer Configuration**: Supports post-configuration via `services.Configure<JwtBearerOptions>()`
- ✅ **Multiple Authentication Types**: Supports AAD, B2C, and CIAM scenarios
- ✅ **.NET 10+ Only**: Available only in .NET 10.0 and later

## API Methods

### 1. Configuration-Based Overload

```csharp
public static AuthenticationBuilder AddMicrosoftIdentityWebApiAot(
    this AuthenticationBuilder builder,
    IConfigurationSection configurationSection,
    string jwtBearerScheme = JwtBearerDefaults.AuthenticationScheme,
    Action<JwtBearerOptions>? configureJwtBearerOptions = null)
```

**Note**: This overload uses `IConfigurationSection.Bind()` which may not be fully AOT-compatible. For full AOT compatibility, use the programmatic overload below.

### 2. Programmatic Overload (Recommended for AOT)

```csharp
public static AuthenticationBuilder AddMicrosoftIdentityWebApiAot(
    this AuthenticationBuilder builder,
    Action<MicrosoftIdentityApplicationOptions> configureOptions,
    string jwtBearerScheme = JwtBearerDefaults.AuthenticationScheme,
    Action<JwtBearerOptions>? configureJwtBearerOptions = null)
```

## Usage Examples

### Basic Configuration-Based Setup

```csharp
// appsettings.json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "your-tenant-id",
    "ClientId": "your-api-client-id"
  }
}

// Program.cs
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApiAot(builder.Configuration.GetSection("AzureAd"));

// Enable token acquisition for OBO scenarios
builder.Services.AddTokenAcquisition();

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
app.Run();
```

### Fully Programmatic Setup (Best for AOT)

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApiAot(options =>
    {
        options.Instance = "https://login.microsoftonline.com/";
        options.TenantId = "your-tenant-id";
        options.ClientId = "your-api-client-id";
    });

builder.Services.AddTokenAcquisition();

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
app.Run();
```

### With Custom JWT Bearer Options

```csharp
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApiAot(
        options =>
        {
            options.Instance = "https://login.microsoftonline.com/";
            options.TenantId = "your-tenant-id";
            options.ClientId = "your-api-client-id";
        },
        configureJwtBearerOptions: options =>
        {
            options.TokenValidationParameters.ClockSkew = TimeSpan.FromMinutes(5);
            options.TokenValidationParameters.ValidateLifetime = true;
        });
```

### B2C Configuration

```csharp
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApiAot(options =>
    {
        options.Instance = "https://your-tenant.b2clogin.com/";
        options.Domain = "your-tenant.onmicrosoft.com";
        options.ClientId = "your-api-client-id";
        options.SignUpSignInPolicyId = "B2C_1_susi";
    });
```

### CIAM Configuration

```csharp
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApiAot(options =>
    {
        options.Authority = "https://your-tenant.ciamlogin.com/";
        options.ClientId = "your-api-client-id";
    });
```

## On-Behalf-Of (OBO) Flow

The AOT-compatible overloads automatically enable OBO token storage without requiring additional configuration. Simply call `AddTokenAcquisition()` after configuring authentication:

```csharp
// Controller example
[Authorize]
[ApiController]
[Route("api/[controller]")]
public class MyApiController : ControllerBase
{
    private readonly ITokenAcquisition _tokenAcquisition;

    public MyApiController(ITokenAcquisition tokenAcquisition)
    {
        _tokenAcquisition = tokenAcquisition;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        // Acquire token on-behalf-of the user
        string token = await _tokenAcquisition.GetAccessTokenForUserAsync(
            new[] { "https://graph.microsoft.com/.default" });

        // Use the token to call downstream APIs
        return Ok("Success");
    }
}
```

## Post-Configuration Support

The AOT overloads support customer post-configuration via `services.Configure<JwtBearerOptions>()`:

```csharp
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApiAot(options =>
    {
        options.Instance = "https://login.microsoftonline.com/";
        options.TenantId = "your-tenant-id";
        options.ClientId = "your-api-client-id";
    });

// Post-configure JWT Bearer options
builder.Services.Configure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
{
    options.TokenValidationParameters.ValidateAudience = true;
    options.Events.OnTokenValidated = async context =>
    {
        // Custom logic after token validation
        Console.WriteLine($"Token validated for user: {context.Principal?.Identity?.Name}");
        await Task.CompletedTask;
    };
});
```

## Architecture

### Component Flow

1. **AddMicrosoftIdentityWebApiAot**: Registers JWT Bearer authentication and core services
2. **MicrosoftIdentityApplicationOptionsToMergedOptionsMerger**: Bridges `MicrosoftIdentityApplicationOptions` to `MergedOptions`
3. **MicrosoftIdentityJwtBearerOptionsPostConfigurator**: Configures JWT Bearer options after customer configuration
4. **OnTokenValidated**: Automatically stores tokens for OBO and validates claims

### Design Decisions

- **Separate Method Name**: Uses `AddMicrosoftIdentityWebApiAot` to avoid signature collisions with existing overloads
- **NET10_0_OR_GREATER Guard**: Ensures the code only compiles for .NET 10+
- **Post-Configuration Pattern**: Uses `IPostConfigureOptions` to ensure our configuration runs after customer configuration
- **Shared Validation**: Reuses validation logic between AOT and non-AOT paths

## Migration from Non-AOT Methods

### Before (Non-AOT)

```csharp
builder.Services.AddAuthentication()
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"))
    .EnableTokenAcquisitionToCallDownstreamApi()
    .AddInMemoryTokenCaches();
```

### After (AOT)

```csharp
builder.Services.AddAuthentication()
    .AddMicrosoftIdentityWebApiAot(builder.Configuration.GetSection("AzureAd"));

builder.Services.AddTokenAcquisition(); // OBO just works!
```

## Limitations

- **NET10_0_OR_GREATER Only**: These methods are only available in .NET 10.0 and later
- **Configuration Binding**: The `IConfigurationSection` overload uses `Bind()` which may not be fully AOT-compatible
- **Diagnostics Events**: The `subscribeToJwtBearerMiddlewareDiagnosticsEvents` parameter has been removed per design decision

## Testing

Unit tests are provided in `WebApiAotExtensionsTests.cs` covering:
- Configuration section delegation
- Programmatic configuration
- Custom JWT Bearer options
- Null argument validation
- MergedOptions population
- OBO token storage

## References

- [Design Specification: Issue #3696](https://github.com/AzureAD/microsoft-identity-web/issues/3696)
- [Microsoft Identity Web Documentation](https://aka.ms/ms-id-web)
- [Native AOT Deployment](https://learn.microsoft.com/dotnet/core/deploying/native-aot/)
