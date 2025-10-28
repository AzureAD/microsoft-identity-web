# ASP.NET Framework Support with Microsoft.Identity.Web

This guide explains how to use Microsoft.Identity.Web libraries in ASP.NET Framework, .NET Standard 2.0, and classic .NET applications (.NET 4.7.2+).

---

## 📋 Table of Contents

- [Overview](#overview)
- [Package Options](#package-options)
- [Token Cache Serialization](#token-cache-serialization)
- [Certificate Management](#certificate-management)
- [OWIN Integration](#owin-integration)
- [Best Practices](#best-practices)

---

## Overview

Starting with **Microsoft.Identity.Web 1.17+**, you have flexible options for using Microsoft Identity libraries in non-ASP.NET Core environments:

### Why Use Microsoft.Identity.Web in ASP.NET Framework?

| Feature | Benefit |
|---------|---------|
| **Token Cache Serialization** | Reusable cache adapters for in-memory, SQL Server, Redis, Cosmos DB |
| **Certificate Helpers** | Simplified certificate loading from KeyVault, file system, or cert stores |
| **Claims Extensions** | Utility methods for `ClaimsPrincipal` manipulation |
| **.NET Standard 2.0** | Compatible with .NET Framework 4.7.2+, .NET Core, and .NET 5+ |
| **Minimal Dependencies** | Targeted packages without ASP.NET Core dependencies |

### Supported Scenarios

- ✅ **ASP.NET MVC/Web API** (NET Framework 4.7.2+)
- ✅ **.NET Framework Console Applications** (daemon scenarios)
- ✅ **Desktop Applications** (.NET Framework)
- ✅ **OWIN-based Web Applications**
- ✅ **.NET Standard 2.0 Libraries** (cross-platform compatibility)

---

## Package Options

### Core Packages for ASP.NET Framework

| Package | Purpose | Dependencies | .NET Target |
|---------|---------|--------------|-------------|
| **Microsoft.Identity.Web.TokenCache** | Token cache serializers, `ClaimsPrincipal` extensions | Minimal | .NET Standard 2.0 |
| **Microsoft.Identity.Web.Certificate** | Certificate loading utilities | Minimal | .NET Standard 2.0 |
| **Microsoft.Identity.Web.OWIN** | OWIN middleware integration | System.Web, OWIN | .NET Framework 4.7.2+ |

### Installation

**Package Manager Console:**
```powershell
# Token cache serialization
Install-Package Microsoft.Identity.Web.TokenCache

# Certificate management
Install-Package Microsoft.Identity.Web.Certificate

# OWIN integration (ASP.NET MVC/Web API)
Install-Package Microsoft.Identity.Web.OWIN
```

**.NET CLI:**
```bash
dotnet add package Microsoft.Identity.Web.TokenCache
dotnet add package Microsoft.Identity.Web.Certificate
dotnet add package Microsoft.Identity.Web.OWIN
```

### Why Not Microsoft.Identity.Web (Core)?

The core `Microsoft.Identity.Web` package includes ASP.NET Core dependencies (`Microsoft.AspNetCore.*`), which:
- Are incompatible with ASP.NET Framework
- Increase package size unnecessarily
- Create dependency conflicts

**Use targeted packages instead** for ASP.NET Framework and .NET Standard scenarios.

---

## Token Cache Serialization

### Overview

Microsoft.Identity.Web provides token cache adapters that work seamlessly with MSAL.NET's `IConfidentialClientApplication` in ASP.NET Framework.

### Pattern: Building Confidential Client with Token Cache

```csharp
using Microsoft.Identity.Client;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.TokenCacheProviders;

public class MsalAppBuilder
{
    private static IConfidentialClientApplication _app;

    public static async Task<IConfidentialClientApplication> BuildConfidentialClientApplication()
    {
        if (_app == null)
        {
            string clientId = ConfigurationManager.AppSettings["AzureAd:ClientId"];
            string clientSecret = ConfigurationManager.AppSettings["AzureAd:ClientSecret"];
            string tenantId = ConfigurationManager.AppSettings["AzureAd:TenantId"];
            string redirectUri = ConfigurationManager.AppSettings["AzureAd:RedirectUri"];

            // Create the confidential client application
            _app = ConfidentialClientApplicationBuilder.Create(clientId)
                .WithClientSecret(clientSecret)
                .WithTenantId(tenantId)
                .WithRedirectUri(redirectUri)
                .Build();

            // Add token cache serialization (choose one option below)
            _app.AddInMemoryTokenCache();
        }

        return _app;
    }
}
```

### Option 1: In-Memory Token Cache

**Simple in-memory cache:**
```csharp
using Microsoft.Identity.Web.TokenCacheProviders;

_app.AddInMemoryTokenCache();
```

**In-memory cache with size limits** (Microsoft.Identity.Web 1.20+):
```csharp
using Microsoft.Extensions.Caching.Memory;

_app.AddInMemoryTokenCache(services =>
{
    // Configure memory cache options
    services.Configure<MemoryCacheOptions>(options =>
    {
        options.SizeLimit = 5000000;  // 5 MB limit
    });
});
```

**Characteristics:**
- ✅ Fast access
- ✅ No external dependencies
- ❌ Not shared across servers (web farm)
- ❌ Lost on app restart

### Option 2: Distributed In-Memory Token Cache

**For multi-server environments with in-memory cache:**
```csharp
_app.AddDistributedTokenCaches(services =>
{
    // Requires: Microsoft.Extensions.Caching.Memory (NuGet)
    services.AddDistributedMemoryCache();
});
```

**Characteristics:**
- ✅ Shared across app instances
- ✅ Better for load-balanced scenarios
- ❌ Requires additional NuGet package
- ❌ Still lost on app restart

### Option 3: SQL Server Token Cache

**For persistent, distributed caching:**
```csharp
using Microsoft.Extensions.Caching.SqlServer;

_app.AddDistributedTokenCaches(services =>
{
    // Requires: Microsoft.Extensions.Caching.SqlServer (NuGet)
    services.AddDistributedSqlServerCache(options =>
    {
        options.ConnectionString = @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=TokenCache;Integrated Security=True;";
        options.SchemaName = "dbo";
        options.TableName = "TokenCache";

        // IMPORTANT: Set expiration above token lifetime
        // Access tokens typically expire after 1 hour
        options.DefaultSlidingExpiration = TimeSpan.FromMinutes(90);
    });
});
```

**Database setup:**
```sql
-- Create the cache table
CREATE TABLE [dbo].[TokenCache] (
    [Id] NVARCHAR(449) NOT NULL,
    [Value] VARBINARY(MAX) NOT NULL,
    [ExpiresAtTime] DATETIMEOFFSET NOT NULL,
    [SlidingExpirationInSeconds] BIGINT NULL,
    [AbsoluteExpiration] DATETIMEOFFSET NULL,
    PRIMARY KEY ([Id])
);

-- Create index for performance
CREATE INDEX [Index_ExpiresAtTime] ON [dbo].[TokenCache] ([ExpiresAtTime]);
```

**Characteristics:**
- ✅ Persistent across restarts
- ✅ Shared across web farm
- ✅ Reliable and scalable
- ⚠️ Requires SQL Server setup

### Option 4: Redis Token Cache

**For high-performance distributed caching:**
```csharp
using StackExchange.Redis;
using Microsoft.Extensions.Caching.StackExchangeRedis;

_app.AddDistributedTokenCaches(services =>
{
    // Requires: Microsoft.Extensions.Caching.StackExchangeRedis (NuGet)
    services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = "localhost:6379";
        options.InstanceName = "TokenCache_";
    });
});
```

**Production configuration:**
```csharp
services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = ConfigurationManager.ConnectionStrings["Redis"].ConnectionString;
    options.InstanceName = "MyApp_";

    // Optional: Configure Redis options
    options.ConfigurationOptions = new ConfigurationOptions
    {
        AbortOnConnectFail = false,
        ConnectTimeout = 5000,
        SyncTimeout = 5000
    };
});
```

**Characteristics:**
- ✅ Extremely fast
- ✅ Shared across web farm
- ✅ Persistent (with Redis persistence enabled)
- ⚠️ Requires Redis server

### Option 5: Cosmos DB Token Cache

**For globally distributed caching:**
```csharp
using Microsoft.Extensions.Caching.Cosmos;

_app.AddDistributedTokenCaches(services =>
{
    // Requires: Microsoft.Extensions.Caching.Cosmos (preview)
    services.AddCosmosCache(options =>
    {
        options.ContainerName = "TokenCache";
        options.DatabaseName = "IdentityCache";
        options.ClientBuilder = new CosmosClientBuilder(
            ConfigurationManager.AppSettings["CosmosConnectionString"]);
        options.CreateIfNotExists = true;
    });
});
```

**Characteristics:**
- ✅ Globally distributed
- ✅ Highly available
- ✅ Automatic scaling
- ⚠️ Higher latency than Redis
- ⚠️ Higher cost

---

## Certificate Management

### Overview

Microsoft.Identity.Web simplifies certificate loading from various sources for client credential flows.

### Pattern: Loading Certificates with DefaultCertificateLoader

```csharp
using Microsoft.Identity.Web;
using Microsoft.Identity.Client;

public class CertificateHelper
{
    public static IConfidentialClientApplication CreateAppWithCertificate()
    {
        string clientId = ConfigurationManager.AppSettings["AzureAd:ClientId"];
        string tenantId = ConfigurationManager.AppSettings["AzureAd:TenantId"];

        // Define certificate source
        var certDescription = CertificateDescription.FromKeyVault(
            keyVaultUrl: "https://my-keyvault.vault.azure.net",
            keyVaultCertificateName: "MyCertificate"
        );

        // Load certificate
        ICertificateLoader certificateLoader = new DefaultCertificateLoader();
        certificateLoader.LoadIfNeeded(certDescription);

        // Create confidential client with certificate
        var app = ConfidentialClientApplicationBuilder.Create(clientId)
            .WithCertificate(certDescription.Certificate)
            .WithTenantId(tenantId)
            .Build();

        return app;
    }
}
```

### Certificate Sources

#### 1. From Azure Key Vault

```csharp
var certDescription = CertificateDescription.FromKeyVault(
    keyVaultUrl: "https://my-keyvault.vault.azure.net",
    keyVaultCertificateName: "MyApplicationCert"
);

ICertificateLoader loader = new DefaultCertificateLoader();
loader.LoadIfNeeded(certDescription);

var app = ConfidentialClientApplicationBuilder.Create(clientId)
    .WithCertificate(certDescription.Certificate)
    .WithTenantId(tenantId)
    .Build();
```

**Prerequisites:**
- Managed Identity or Service Principal with Key Vault access
- `Azure.Identity` NuGet package
- Key Vault permission: `Get` on certificates

#### 2. From Certificate Store

```csharp
var certDescription = CertificateDescription.FromStoreWithDistinguishedName(
    distinguishedName: "CN=MyApp.contoso.com",
    storeName: StoreName.My,
    storeLocation: StoreLocation.CurrentUser
);

ICertificateLoader loader = new DefaultCertificateLoader();
loader.LoadIfNeeded(certDescription);

var app = ConfidentialClientApplicationBuilder.Create(clientId)
    .WithCertificate(certDescription.Certificate)
    .WithTenantId(tenantId)
    .Build();
```

**Or find by thumbprint:**
```csharp
var certDescription = CertificateDescription.FromStoreWithThumbprint(
    thumbprint: "ABCDEF1234567890ABCDEF1234567890ABCDEF12",
    storeName: StoreName.My,
    storeLocation: StoreLocation.LocalMachine
);
```

#### 3. From File System

```csharp
var certDescription = CertificateDescription.FromPath(
    path: @"C:\Certificates\MyAppCert.pfx",
    password: "CertificatePassword123"
);

ICertificateLoader loader = new DefaultCertificateLoader();
loader.LoadIfNeeded(certDescription);

var app = ConfidentialClientApplicationBuilder.Create(clientId)
    .WithCertificate(certDescription.Certificate)
    .WithTenantId(tenantId)
    .Build();
```

**Security note:** Never hardcode passwords. Use secure configuration:
```csharp
string password = ConfigurationManager.AppSettings["Certificate:Password"];
```

#### 4. From Base64-Encoded String

```csharp
var certDescription = CertificateDescription.FromBase64Encoded(
    base64EncodedValue: base64CertString,
    password: "CertificatePassword"  // Optional
);

ICertificateLoader loader = new DefaultCertificateLoader();
loader.LoadIfNeeded(certDescription);
```

### Configuration-Based Certificate Loading

**Web.config or App.config:**
```xml
<appSettings>
  <add key="AzureAd:ClientId" value="your-client-id" />
  <add key="AzureAd:TenantId" value="your-tenant-id" />

  <!-- Option 1: KeyVault -->
  <add key="Certificate:SourceType" value="KeyVault" />
  <add key="Certificate:KeyVaultUrl" value="https://my-vault.vault.azure.net" />
  <add key="Certificate:KeyVaultCertificateName" value="MyCert" />

  <!-- Option 2: Store -->
  <!--
  <add key="Certificate:SourceType" value="StoreWithThumbprint" />
  <add key="Certificate:CertificateThumbprint" value="ABCD..." />
  <add key="Certificate:CertificateStorePath" value="CurrentUser/My" />
  -->
</appSettings>
```

**C# code:**
```csharp
public static CertificateDescription GetCertificateFromConfig()
{
    string sourceType = ConfigurationManager.AppSettings["Certificate:SourceType"];

    return sourceType switch
    {
        "KeyVault" => CertificateDescription.FromKeyVault(
            ConfigurationManager.AppSettings["Certificate:KeyVaultUrl"],
            ConfigurationManager.AppSettings["Certificate:KeyVaultCertificateName"]
        ),

        "StoreWithThumbprint" => CertificateDescription.FromStoreWithThumbprint(
            ConfigurationManager.AppSettings["Certificate:CertificateThumbprint"],
            StoreName.My,
            StoreLocation.CurrentUser
        ),

        _ => throw new ConfigurationErrorsException("Invalid certificate source type")
    };
}
```

---

## OWIN Integration

### Overview

The `Microsoft.Identity.Web.OWIN` package provides seamless integration with ASP.NET MVC and Web API applications using OWIN middleware.

### Setup in Startup.Auth.cs

```csharp
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.OWIN;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.OpenIdConnect;
using Owin;

public partial class Startup
{
    public void ConfigureAuth(IAppBuilder app)
    {
        app.SetDefaultSignInAsAuthenticationType(CookieAuthenticationDefaults.AuthenticationType);

        app.UseCookieAuthentication(new CookieAuthenticationOptions());

        app.UseOpenIdConnectAuthentication(
            new OpenIdConnectAuthenticationOptions
            {
                ClientId = ConfigurationManager.AppSettings["AzureAd:ClientId"],
                Authority = $"https://login.microsoftonline.com/{ConfigurationManager.AppSettings["AzureAd:TenantId"]}",
                RedirectUri = ConfigurationManager.AppSettings["AzureAd:RedirectUri"],
                PostLogoutRedirectUri = ConfigurationManager.AppSettings["AzureAd:PostLogoutRedirectUri"],
                Scope = "openid profile email offline_access",
                ResponseType = "code id_token",

                TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true
                }
            });

        // Configure Microsoft Identity Web services
        var services = CreateOwinServiceCollection();

        services.AddTokenAcquisition();
        services.AddDistributedTokenCaches(cacheServices =>
        {
            cacheServices.AddDistributedMemoryCache();
        });

        // Build service provider
        var serviceProvider = services.BuildServiceProvider();

        // Create token acquirer factory
        var tokenAcquirerFactory = new OwinTokenAcquirerFactory(serviceProvider);
        app.Use<OwinTokenAcquisitionMiddleware>(tokenAcquirerFactory);
    }

    private IServiceCollection CreateOwinServiceCollection()
    {
        var services = new ServiceCollection();

        // Add configuration
        IConfiguration configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .Build();

        services.AddSingleton(configuration);

        return services;
    }
}
```

### Controller Integration

**Extending ControllerBase with Microsoft.Identity.Web:**

```csharp
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.OWIN;
using System.Web.Mvc;

public class HomeController : Controller
{
    [Authorize]
    public async Task<ActionResult> Index()
    {
        // Access Graph Service Client
        var graphClient = this.GetGraphServiceClient();
        var user = await graphClient.Me.GetAsync();

        ViewBag.UserName = user.DisplayName;
        ViewBag.Email = user.Mail;

        return View();
    }

    [Authorize]
    public async Task<ActionResult> CallApi()
    {
        // Access downstream API
        var downstreamApi = this.GetDownstreamApi();

        var result = await downstreamApi.GetForUserAsync<List<TodoItem>>(
            "TodoListService",
            options =>
            {
                options.RelativePath = "api/todolist";
            });

        return View(result);
    }
}
```

### Configuration in Web.config

```xml
<appSettings>
  <add key="AzureAd:Instance" value="https://login.microsoftonline.com/" />
  <add key="AzureAd:TenantId" value="your-tenant-id" />
  <add key="AzureAd:ClientId" value="your-client-id" />
  <add key="AzureAd:ClientSecret" value="your-client-secret" />
  <add key="AzureAd:RedirectUri" value="https://localhost:44368/" />
  <add key="AzureAd:PostLogoutRedirectUri" value="https://localhost:44368/" />

  <!-- Downstream API configuration -->
  <add key="DownstreamApi:TodoListService:BaseUrl" value="https://localhost:44351" />
  <add key="DownstreamApi:TodoListService:Scopes" value="api://todo-api-client-id/.default" />
</appSettings>
```

---

## Best Practices

### ✅ Do's

**1. Use singleton pattern for IConfidentialClientApplication:**
```csharp
private static IConfidentialClientApplication _app;

public static IConfidentialClientApplication GetApp()
{
    if (_app == null)
    {
        _app = ConfidentialClientApplicationBuilder.Create(clientId)
            .WithClientSecret(clientSecret)
            .WithTenantId(tenantId)
            .Build();

        _app.AddDistributedTokenCaches(/* ... */);
    }

    return _app;
}
```

**2. Set appropriate token cache expiration:**
```csharp
// Access tokens typically expire after 1 hour
// Set cache expiration ABOVE token lifetime
options.DefaultSlidingExpiration = TimeSpan.FromMinutes(90);
```

**3. Use secure certificate storage:**
```csharp
// ✅ Azure Key Vault (production)
var cert = CertificateDescription.FromKeyVault(keyVaultUrl, certName);

// ✅ Certificate store with proper permissions
var cert = CertificateDescription.FromStoreWithThumbprint(thumbprint, StoreName.My, StoreLocation.LocalMachine);
```

**4. Implement proper error handling:**
```csharp
try
{
    var result = await app.AcquireTokenForClient(scopes).ExecuteAsync();
}
catch (MsalServiceException ex)
{
    logger.Error($"Token acquisition failed. CorrelationId: {ex.CorrelationId}, ErrorCode: {ex.ErrorCode}");
    throw;
}
```

### ❌ Don'ts

**1. Don't create new IConfidentialClientApplication instances repeatedly:**
```csharp
// ❌ Wrong - creates new instance on every request
public ActionResult Index()
{
    var app = ConfidentialClientApplicationBuilder.Create(clientId).Build();
    // ...
}

// ✅ Correct - use singleton
private static readonly IConfidentialClientApplication _app = BuildApp();
```

**2. Don't hardcode secrets:**
```csharp
// ❌ Wrong
.WithClientSecret("supersecretvalue123")

// ✅ Correct
.WithClientSecret(ConfigurationManager.AppSettings["AzureAd:ClientSecret"])
```

**3. Don't use in-memory cache for web farms:**
```csharp
// ❌ Wrong for multi-server deployments
app.AddInMemoryTokenCache();

// ✅ Correct - use distributed cache
app.AddDistributedTokenCaches(services =>
{
    services.AddDistributedSqlServerCache(/* ... */);
});
```

**4. Don't ignore certificate validation:**
```csharp
// ❌ Wrong - skips validation
ServicePointManager.ServerCertificateValidationCallback = (sender, cert, chain, errors) => true;

// ✅ Correct - validate certificates properly
```

---

## Sample Applications

### Official Microsoft Samples

| Sample | Platform | Description |
|--------|----------|-------------|
| [ConfidentialClientTokenCache](https://github.com/Azure-Samples/active-directory-dotnet-v1-to-v2/tree/master/ConfidentialClientTokenCache) | Console (.NET Framework) | Token cache serialization patterns |
| [ms-identity-aspnet-webapp-openidconnect](https://github.com/Azure-Samples/ms-identity-aspnet-webapp-openidconnect) | ASP.NET MVC (NET 4.7.2) | Full web app with OWIN integration |
| [active-directory-dotnetcore-daemon-v2](https://github.com/Azure-Samples/active-directory-dotnetcore-daemon-v2) | Console (.NET Core) | Certificate loading from Key Vault |

### Quick Start Templates

**ASP.NET MVC with OWIN:**
```bash
# Clone the sample
git clone https://github.com/Azure-Samples/ms-identity-aspnet-webapp-openidconnect

# Key files to review:
# - App_Start/Startup.Auth.cs (authentication configuration)
# - Utils/MsalAppBuilder.cs (MSAL setup with token cache)
# - Controllers/HomeController.cs (authenticated endpoints)
```

---

## Migration Guide

### From ADAL.NET to MSAL.NET with Microsoft.Identity.Web

**ADAL.NET (deprecated):**
```csharp
AuthenticationContext authContext = new AuthenticationContext(authority);
AuthenticationResult result = await authContext.AcquireTokenAsync(resource, credential);
```

**MSAL.NET with Microsoft.Identity.Web:**
```csharp
var app = ConfidentialClientApplicationBuilder.Create(clientId)
    .WithClientSecret(clientSecret)
    .WithTenantId(tenantId)
    .Build();

app.AddInMemoryTokenCache();  // Add token cache

AuthenticationResult result = await app.AcquireTokenForClient(scopes).ExecuteAsync();
```

### Key Differences

| Aspect | ADAL.NET | MSAL.NET + Microsoft.Identity.Web |
|--------|----------|-----------------------------------|
| **Scopes** | Resource-based (`https://graph.microsoft.com`) | Scope-based (`https://graph.microsoft.com/.default`) |
| **Token Cache** | Manual serialization | Built-in adapters |
| **Certificates** | Manual loading | `DefaultCertificateLoader` |
| **Authority** | Fixed at construction | Can be overridden per request |

---

## Additional Resources

- [Token Cache Serialization in MSAL.NET](https://learn.microsoft.com/azure/active-directory/develop/msal-net-token-cache-serialization)
- [Using Certificates with Microsoft.Identity.Web](https://github.com/AzureAD/microsoft-identity-web/wiki/Certificates)
- [OWIN Integration Guide](https://github.com/AzureAD/microsoft-identity-web/wiki/OWIN)
- [NuGet Package Dependencies](https://github.com/AzureAD/microsoft-identity-web/wiki/NuGet-package-references)

---

**Last Updated:** October 27, 2025
**Microsoft.Identity.Web Version:** 3.14.1+
**Supported Frameworks:** .NET Framework 4.7.2+, .NET Standard 2.0
