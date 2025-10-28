# MSAL.NET with Microsoft.Identity.Web in .NET Framework

This guide explains how to use Microsoft.Identity.Web token cache and certificate packages with MSAL.NET in .NET Framework, .NET Standard 2.0, and classic .NET applications (.NET 4.7.2+).

---

## 📋 Table of Contents

- [Overview](#overview)
- [Package Options](#package-options)
- [Token Cache Serialization](#token-cache-serialization)
- [Certificate Management](#certificate-management)
- [Sample Applications](#sample-applications)
- [Best Practices](#best-practices)

---

## Overview

Starting with **Microsoft.Identity.Web 1.17+**, you can use Microsoft.Identity.Web utility packages with MSAL.NET in non-ASP.NET Core environments.

### Why Use These Packages?

| Feature | Benefit |
|---------|---------|
| **Token Cache Serialization** | Reusable cache adapters for in-memory, SQL Server, Redis, Cosmos DB |
| **Certificate Helpers** | Simplified certificate loading from KeyVault, file system, or cert stores |
| **Claims Extensions** | Utility methods for `ClaimsPrincipal` manipulation |
| **.NET Standard 2.0** | Compatible with .NET Framework 4.7.2+, .NET Core, and .NET 5+ |
| **Minimal Dependencies** | Targeted packages without ASP.NET Core dependencies |

### Supported Scenarios

- ✅ **.NET Framework Console Applications** (daemon scenarios)
- ✅ **Desktop Applications** (.NET Framework)
- ✅ **Worker Services** (.NET Framework)
- ✅ **.NET Standard 2.0 Libraries** (cross-platform compatibility)
- ✅ **Non-web MSAL.NET applications**

> **Note:** For ASP.NET MVC/Web API applications, see [OWIN Integration](owin.md) instead.

---

## Package Options

### Core Packages for MSAL.NET

| Package | Purpose | Dependencies | .NET Target |
|---------|---------|--------------|-------------|
| **Microsoft.Identity.Web.TokenCache** | Token cache serializers, `ClaimsPrincipal` extensions | Minimal | .NET Standard 2.0 |
| **Microsoft.Identity.Web.Certificate** | Certificate loading utilities | Minimal | .NET Standard 2.0 |

### Installation

**Package Manager Console:**
```powershell
# Token cache serialization
Install-Package Microsoft.Identity.Web.TokenCache

# Certificate management
Install-Package Microsoft.Identity.Web.Certificate
```

**.NET CLI:**
```bash
dotnet add package Microsoft.Identity.Web.TokenCache
dotnet add package Microsoft.Identity.Web.Certificate
```

### Why Not Microsoft.Identity.Web (Core)?

The core `Microsoft.Identity.Web` package includes ASP.NET Core dependencies (`Microsoft.AspNetCore.*`), which:
- Are incompatible with ASP.NET Framework
- Increase package size unnecessarily
- Create dependency conflicts

**Use targeted packages instead** for .NET Framework and .NET Standard scenarios.

---

## Token Cache Serialization

### Overview

Microsoft.Identity.Web provides token cache adapters that work seamlessly with MSAL.NET's `IConfidentialClientApplication`.

### Pattern: Building Confidential Client with Token Cache

```csharp
using Microsoft.Identity.Client;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.TokenCacheProviders;

public class MsalAppBuilder
{
    private static IConfidentialClientApplication _app;

    public static IConfidentialClientApplication BuildConfidentialClientApplication()
    {
        if (_app == null)
        {
            string clientId = ConfigurationManager.AppSettings["AzureAd:ClientId"];
            string clientSecret = ConfigurationManager.AppSettings["AzureAd:ClientSecret"];
            string tenantId = ConfigurationManager.AppSettings["AzureAd:TenantId"];

            // Create the confidential client application
            _app = ConfidentialClientApplicationBuilder.Create(clientId)
                .WithClientSecret(clientSecret)
                .WithTenantId(tenantId)
                .WithAuthority(AzureCloudInstance.AzurePublic, tenantId)
                .Build();

            // Add token cache serialization (choose one option below)
            _app.AddInMemoryTokenCache();
        }

        return _app;
    }
}
```

### Token Cache Options

#### Option 1: In-Memory Token Cache

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
- ❌ Not shared across processes
- ❌ Lost on app restart

**Use case:** Single-instance console apps, desktop applications

---

#### Option 2: Distributed In-Memory Token Cache

**For multi-instance environments with in-memory cache:**
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

**Use case:** Multi-instance services with acceptable token re-acquisition

---

#### Option 3: SQL Server Token Cache

**For persistent, distributed caching:**
```csharp
using Microsoft.Extensions.Caching.SqlServer;

_app.AddDistributedTokenCaches(services =>
{
    // Requires: Microsoft.Extensions.Caching.SqlServer (NuGet)
    services.AddDistributedSqlServerCache(options =>
    {
        options.ConnectionString = ConfigurationManager.ConnectionStrings["TokenCache"].ConnectionString;
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
- ✅ Shared across multiple instances
- ✅ Reliable and scalable
- ⚠️ Requires SQL Server setup

**Use case:** Production daemon services, scheduled tasks, multi-instance workers

---

#### Option 4: Redis Token Cache

**For high-performance distributed caching:**
```csharp
using StackExchange.Redis;
using Microsoft.Extensions.Caching.StackExchangeRedis;

_app.AddDistributedTokenCaches(services =>
{
    // Requires: Microsoft.Extensions.Caching.StackExchangeRedis (NuGet)
    services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = ConfigurationManager.AppSettings["Redis:ConnectionString"];
        options.InstanceName = "TokenCache_";
    });
});
```

**Production configuration:**
```csharp
services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = ConfigurationManager.AppSettings["Redis:ConnectionString"];
    options.InstanceName = "MyDaemonApp_";

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
- ✅ Shared across instances
- ✅ Persistent (with Redis persistence enabled)
- ⚠️ Requires Redis server

**Use case:** High-volume daemon apps, distributed systems, microservices

---

#### Option 5: Cosmos DB Token Cache

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

**Use case:** Global daemon services, geo-distributed applications

---

### Complete Example: Daemon Application

```csharp
using Microsoft.Identity.Client;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.TokenCacheProviders;
using System;
using System.Threading.Tasks;

namespace DaemonApp
{
    class Program
    {
        private static IConfidentialClientApplication _app;

        static async Task Main(string[] args)
        {
            // Build confidential client with token cache
            _app = BuildConfidentialClient();

            // Acquire token for app-only access
            string[] scopes = new[] { "https://graph.microsoft.com/.default" };

            try
            {
                var result = await _app.AcquireTokenForClient(scopes)
                    .ExecuteAsync();

                Console.WriteLine($"Token acquired successfully!");
                Console.WriteLine($"Token source: {result.AuthenticationResultMetadata.TokenSource}");
                Console.WriteLine($"Expires on: {result.ExpiresOn}");

                // Use token to call API
                await CallProtectedApi(result.AccessToken);
            }
            catch (MsalServiceException ex)
            {
                Console.WriteLine($"Error acquiring token: {ex.ErrorCode}");
                Console.WriteLine($"CorrelationId: {ex.CorrelationId}");
            }
        }

        private static IConfidentialClientApplication BuildConfidentialClient()
        {
            var app = ConfidentialClientApplicationBuilder
                .Create(ConfigurationManager.AppSettings["ClientId"])
                .WithClientSecret(ConfigurationManager.AppSettings["ClientSecret"])
                .WithTenantId(ConfigurationManager.AppSettings["TenantId"])
                .Build();

            // Add SQL Server token cache for persistence
            app.AddDistributedTokenCaches(services =>
            {
                services.AddDistributedSqlServerCache(options =>
                {
                    options.ConnectionString = ConfigurationManager
                        .ConnectionStrings["TokenCache"].ConnectionString;
                    options.SchemaName = "dbo";
                    options.TableName = "TokenCache";
                    options.DefaultSlidingExpiration = TimeSpan.FromMinutes(90);
                });
            });

            return app;
        }

        private static async Task CallProtectedApi(string accessToken)
        {
            // Your API call logic
        }
    }
}
```

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

        // Add token cache
        app.AddInMemoryTokenCache();

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

---

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

---

#### 3. From File System

```csharp
var certDescription = CertificateDescription.FromPath(
    path: @"C:\Certificates\MyAppCert.pfx",
    password: ConfigurationManager.AppSettings["Certificate:Password"]
);

ICertificateLoader loader = new DefaultCertificateLoader();
loader.LoadIfNeeded(certDescription);

var app = ConfidentialClientApplicationBuilder.Create(clientId)
    .WithCertificate(certDescription.Certificate)
    .WithTenantId(tenantId)
    .Build();
```

**Security note:** Never hardcode passwords. Use secure configuration.

---

#### 4. From Base64-Encoded String

```csharp
string base64Cert = ConfigurationManager.AppSettings["Certificate:Base64"];

var certDescription = CertificateDescription.FromBase64Encoded(
    base64EncodedValue: base64Cert,
    password: ConfigurationManager.AppSettings["Certificate:Password"]  // Optional
);

ICertificateLoader loader = new DefaultCertificateLoader();
loader.LoadIfNeeded(certDescription);
```

---

### Configuration-Based Certificate Loading

**App.config:**
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

<connectionStrings>
  <add name="TokenCache"
       connectionString="Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=TokenCache;Integrated Security=True;" />
</connectionStrings>
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

## Sample Applications

### Official Microsoft Samples

| Sample | Platform | Description |
|--------|----------|-------------|
| [ConfidentialClientTokenCache](https://github.com/Azure-Samples/active-directory-dotnet-v1-to-v2/tree/master/ConfidentialClientTokenCache) | Console (.NET Framework) | Token cache serialization patterns |
| [active-directory-dotnetcore-daemon-v2](https://github.com/Azure-Samples/active-directory-dotnetcore-daemon-v2) | Console (.NET Core) | Certificate loading from Key Vault |

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
var cert = CertificateDescription.FromStoreWithThumbprint(
    thumbprint, StoreName.My, StoreLocation.LocalMachine);
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

**5. Use distributed cache for production:**
```csharp
// ✅ Correct for daemon services
app.AddDistributedTokenCaches(services =>
{
    services.AddDistributedSqlServerCache(/* ... */);
});
```

### ❌ Don'ts

**1. Don't create new IConfidentialClientApplication instances repeatedly:**
```csharp
// ❌ Wrong - creates new instance every time
public void AcquireToken()
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

**3. Don't use in-memory cache for multi-instance services:**
```csharp
// ❌ Wrong for services with multiple instances
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

## Migration from ADAL.NET

### Key Differences

| Aspect | ADAL.NET (deprecated) | MSAL.NET + Microsoft.Identity.Web |
|--------|----------------------|-----------------------------------|
| **Scopes** | Resource-based (`https://graph.microsoft.com`) | Scope-based (`https://graph.microsoft.com/.default`) |
| **Token Cache** | Manual serialization required | Built-in adapters via extension methods |
| **Certificates** | Manual X509Certificate2 loading | `DefaultCertificateLoader` with multiple sources |
| **Authority** | Fixed at construction | Can be overridden per request |

### Migration Example

**ADAL.NET (Old):**
```csharp
AuthenticationContext authContext = new AuthenticationContext(authority);
ClientCredential credential = new ClientCredential(clientId, clientSecret);
AuthenticationResult result = await authContext.AcquireTokenAsync(resource, credential);
```

**MSAL.NET with Microsoft.Identity.Web (New):**
```csharp
var app = ConfidentialClientApplicationBuilder.Create(clientId)
    .WithClientSecret(clientSecret)
    .WithTenantId(tenantId)
    .Build();

app.AddInMemoryTokenCache();  // Add token cache

string[] scopes = new[] { "https://graph.microsoft.com/.default" };
AuthenticationResult result = await app.AcquireTokenForClient(scopes).ExecuteAsync();
```

---

## See Also

- **[OWIN Integration](owin.md)** - For ASP.NET MVC and Web API applications
- **[ASP.NET Framework Overview](aspnet-framework.md)** - Choose the right package for your scenario
- **[Credentials Guide](../authentication/credentials/README.md)** - Certificate and client secret management
- **[Logging & Diagnostics](../advanced/logging.md)** - Troubleshoot token acquisition issues

---

## Additional Resources

- [Token Cache Serialization in MSAL.NET](https://learn.microsoft.com/azure/active-directory/develop/msal-net-token-cache-serialization)
- [Using Certificates with Microsoft.Identity.Web](https://github.com/AzureAD/microsoft-identity-web/wiki/Certificates)
- [NuGet Package Dependencies](https://github.com/AzureAD/microsoft-identity-web/wiki/NuGet-package-references)
- [MSAL.NET Documentation](https://learn.microsoft.com/azure/active-directory/develop/msal-overview)

---

**Last Updated:** October 27, 2025
**Microsoft.Identity.Web Version:** 3.14.1+
**Supported Frameworks:** .NET Framework 4.7.2+, .NET Standard 2.0
