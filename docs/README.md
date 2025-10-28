# Microsoft.Identity.Web Documentation

Microsoft.Identity.Web is a set of libraries that simplifies adding authentication and authorization support to applications integrating with the Microsoft identity platform (formerly Azure AD v2.0). It supports:

- **ASP.NET Core** web applications and web APIs
- **OWIN** applications on .NET Framework
- **.NET** daemon applications and background services

Whether you're building web apps that sign in users, web APIs that validate tokens, or background services that call protected APIs, Microsoft.Identity.Web handles the authentication complexity for you.

## 🚀 Quick Start

Choose your scenario:

- **[Web App - Sign in users](./getting-started/quickstart-webapp.md)** - Add authentication to your ASP.NET Core web application
- **[Web API - Protect your API](./getting-started/quickstart-webapi.md)** - Secure your ASP.NET Core web API with bearer tokens
- **[Daemon App - Call APIs](./scenarios/daemon/README.md)** - Build background services that call protected APIs

## 📦 What's Included

Microsoft.Identity.Web provides:

✅ **Simplified Authentication** - Minimal configuration for signing in users and validating tokens
✅ **Downstream API Calls** - Call Microsoft Graph, Azure SDKs, or your own protected APIs with automatic token management
   - **Token Acquisition** - Acquire tokens on behalf of users or your application
   - **Token Cache Management** - Distributed cache support with Redis, SQL Server, Cosmos DB
✅ **Multiple Credential Types** - Support for certificates, managed identities, and certificateless authentication
✅ **Automatic Authorization Headers** - Authentication is handled transparently when calling APIs
✅ **Production-Ready** - Used by thousands of Microsoft and customer applications

### Calling APIs with Automatic Authentication

Microsoft.Identity.Web makes it easy to call protected APIs without manually managing tokens:

- **Microsoft Graph** - Use `GraphServiceClient` with automatic token acquisition
- **Azure SDKs** - Use `TokenCredential` implementations that integrate with Microsoft.Identity.Web
- **Your Own APIs** - Use `IDownstreamApi` or `IAuthorizationHeaderProvider` for seamless API calls
- **Agent Identity APIs** - Call APIs on behalf of managed identities or service principals with automatic credential handling

Authentication headers are automatically added to your requests, and tokens are acquired and cached transparently. See the [Microsoft.Identity.Web.DownstreamApi documentation](./packages/downstream-api.md) and [Agent Identities guide](./packages/agent-identities.md) for complete details.

## ⚙️ Configuration Approaches

Microsoft.Identity.Web supports flexible configuration for all scenarios:

### Configuration by File (Recommended)

All scenarios can be configured using `appsettings.json`:

```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "your-tenant-id",
    "ClientId": "your-client-id"
  }
}
```

**Important for daemon apps and console applications:** Ensure your `appsettings.json` file is copied to the output directory. In Visual Studio, set the **"Copy to Output Directory"** property to **"Copy if newer"** or **"Copy always"**, or add this to your `.csproj`:

```xml
<ItemGroup>
  <None Update="appsettings.json">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </None>
</ItemGroup>
```

### Configuration by Code

You can also configure authentication programmatically:

```csharp
builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(options =>
    {
        options.Instance = "https://login.microsoftonline.com/";
        options.TenantId = "your-tenant-id";
        options.ClientId = "your-client-id";
    });
```

Both approaches are available for all scenarios (web apps, web APIs, and daemon applications).

## 🎯 Core Scenarios

### Web Applications

Build web apps that sign in users with work/school accounts or personal Microsoft accounts.

```csharp
// In Program.cs
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Identity.Web;

var builder = WebApplication.CreateBuilder(args);

// Add authentication with explicit scheme
builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"));

builder.Services.AddRazorPages();
```

**Learn more:** [Web Apps Scenario](./scenarios/web-apps/README.md)

### Protected Web APIs

Secure your APIs and validate access tokens from clients.

```csharp
// In Program.cs
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Web;

var builder = WebApplication.CreateBuilder(args);

// Add authentication with explicit scheme
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));

builder.Services.AddControllers();
```

**Learn more:** [Web APIs Scenario](./scenarios/web-apis/README.md)

### Daemon Applications

Build background services and console apps that call APIs using their own identity.

```csharp
// In Program.cs
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web;

// Get the Token acquirer factory instance
var tokenAcquirerFactory = TokenAcquirerFactory.GetDefaultInstance();

// Configure downstream API
tokenAcquirerFactory.Services.AddDownstreamApi("MyApi",
    tokenAcquirerFactory.Configuration.GetSection("MyWebApi"));

var sp = tokenAcquirerFactory.Build();

// Call API - authentication is automatic
var api = sp.GetRequiredService<IDownstreamApi>();
var result = await api.GetForAppAsync<IEnumerable<MyData>>("MyApi");
```

**Configuration (appsettings.json):**

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
  "MyWebApi": {
    "BaseUrl": "https://myapi.example.com/",
    "RelativePath": "api/data",
    "RequestAppToken": true,
    "Scopes": [ "api://your-api-id/.default" ]
  }
}
```

> **Note:** `ClientCredentials` supports multiple authentication methods including certificates, Key Vault, managed identities, and certificateless authentication (FIC+MSI). See the [Credentials Guide](./authentication/credentials/README.md) for all options.

> **Alternative:** For more control over HTTP calls, use `IAuthorizationHeaderProvider` to get authorization headers and manage HTTP requests yourself. See [Daemon Scenario](./scenarios/daemon/README.md) for details.

**Learn more:** [Daemon Scenario](./scenarios/daemon/README.md)

## 🏗️ Package Architecture

Microsoft.Identity.Web is composed of several NuGet packages to support different scenarios:

| Package | Purpose | Target Frameworks |
|---------|---------|-------------------|
| **[Microsoft.Identity.Web](https://www.nuget.org/packages/Microsoft.Identity.Web)** | Core library for ASP.NET Core web apps | .NET 6.0+, .NET Framework 4.6.2+ |
| **[Microsoft.Identity.Web.TokenAcquisition](https://www.nuget.org/packages/Microsoft.Identity.Web.TokenAcquisition)** | Token acquisition services | .NET 6.0+ |
| **[Microsoft.Identity.Web.TokenCache](https://www.nuget.org/packages/Microsoft.Identity.Web.TokenCache)** | Token cache serialization | .NET Standard 2.0+ |
| **[Microsoft.Identity.Web.DownstreamApi](https://www.nuget.org/packages/Microsoft.Identity.Web.DownstreamApi)** | Helper for calling downstream APIs | .NET 6.0+ |
| **[Microsoft.Identity.Web.UI](https://www.nuget.org/packages/Microsoft.Identity.Web.UI)** | UI components for web apps | .NET 6.0+ |
| **[Microsoft.Identity.Web.GraphServiceClient](https://www.nuget.org/packages/Microsoft.Identity.Web.GraphServiceClient)** | Microsoft Graph SDK integration | .NET 6.0+ |
| **[Microsoft.Identity.Web.Certificate](https://www.nuget.org/packages/Microsoft.Identity.Web.Certificate)** | Certificate loading helpers | .NET Standard 2.0+ |
| **[Microsoft.Identity.Web.Certificateless](https://www.nuget.org/packages/Microsoft.Identity.Web.Certificateless)** | Certificateless authentication | .NET 6.0+ |
| **[Microsoft.Identity.Web.OWIN](https://www.nuget.org/packages/Microsoft.Identity.Web.OWIN)** | OWIN/ASP.NET Framework support | .NET Framework 4.6.2+ |

**Learn more:** [Package Reference Guide](./packages/README.md)

## 🔐 Authentication Credentials

Microsoft.Identity.Web supports multiple ways to authenticate your application:

**Recommended for Production:**
- **[Certificateless (FIC + Managed Identity)](./authentication/credentials/certificateless.md)** ⭐ - Zero certificate management, automatic rotation
- **[Certificates from Key Vault](./authentication/credentials/certificates.md#key-vault)** - Centralized certificate management with Azure Key Vault

**For Development:**
- **[Client Secrets](./authentication/credentials/client-secrets.md)** - Simple shared secrets (not for production)
- **[Certificates from Files](./authentication/credentials/certificates.md#file-path)** - PFX/P12 files on disk

**See:** [Credential Decision Guide](./authentication/credentials/README.md) for choosing the right approach.

## 🌐 Supported .NET Versions

| .NET Version | Support Status | Notes |
|--------------|----------------|-------|
| **.NET 9** | ✅ Supported | Latest release, recommended for new projects |
| **.NET 8** | ✅ Supported (LTS) | Long-term support until November 2026 |
| **.NET 6** | ⚠️ Deprecated | Support ending in version 4.0.0 (use .NET 8 LTS) |
| **.NET 7** | ⚠️ Deprecated | Support ending in version 4.0.0 |
| **.NET Framework 4.7.2** | ✅ Supported | For OWIN applications (via specific packages) |
| **.NET Framework 4.6.2** | ✅ Supported | For OWIN applications (via specific packages) |

**Current stable version:** 3.14.1
**Upcoming:** Version 4.0.0 will remove .NET 6.0 and .NET 7.0 support

## 📖 Documentation Structure

### Getting Started
- [Quickstart: Web App](./getting-started/quickstart-webapp.md) - Sign in users in 10 minutes
- [Quickstart: Web API](./getting-started/quickstart-webapi.md) - Protect your API in 10 minutes
- [Why Microsoft.Identity.Web?](./getting-started/why-microsoft-identity-web.md)

### Scenarios
- [Web Applications](./scenarios/web-apps/README.md) - Sign-in users, call APIs
- [Web APIs](./scenarios/web-apis/README.md) - Protect APIs, call downstream services
- [Daemon Applications](./scenarios/daemon/README.md) - Background services with app identity
- [Azure Functions](./scenarios/azure-functions/README.md) - Serverless authentication

### Authentication & Tokens
- [Credentials Guide](./authentication/credentials/README.md) - Choose and configure credentials
- [Token Cache](./authentication/token-cache/README.md) - Configure distributed caching
- [Token Decryption](./authentication/token-cache/token-decryption.md) - Decrypt encrypted tokens

### Advanced Topics
- [Multiple Authentication Schemes](./advanced/multiple-auth-schemes.md)
- [Incremental Consent & Conditional Access](./advanced/incremental-consent-ca.md)
- [Long-Running Processes](./advanced/long-running-processes.md)
- [APIs Behind Gateways](./advanced/api-gateways.md)
- [Performance Optimization](./advanced/performance.md)
- [Logging & Diagnostics](./advanced/logging.md)
- [Customization](./advanced/customization.md)

### Deployment
- [Azure App Service](./deployment/azure-app-service.md)
- [Containers & Docker](./deployment/containers.md)

### Migration Guides
- [Migrating from 1.x to 2.x](./migration/v1-to-v2.md)
- [Migrating from 2.x to 3.x](./migration/v2-to-v3.md)

## 🔗 External Resources

- **[NuGet Packages](https://www.nuget.org/packages?q=Microsoft.Identity.Web)** - Download packages
- **[API Reference](https://learn.microsoft.com/dotnet/api/microsoft.identity.web)** - Complete API documentation
- **[Samples Repository](https://github.com/Azure-Samples/active-directory-aspnetcore-webapp-openidconnect-v2)** - Working code examples
- **[GitHub Issues](https://github.com/AzureAD/microsoft-identity-web/issues)** - Report bugs or request features
- **[Stack Overflow](https://stackoverflow.com/questions/tagged/microsoft-identity-web)** - Community support

## 🤝 Contributing

We welcome contributions! See our [Contributing Guide](https://github.com/AzureAD/microsoft-identity-web/blob/master/CONTRIBUTING.md) for details.

## 📄 License

This project is licensed under the MIT License. See [LICENSE](https://github.com/AzureAD/microsoft-identity-web/blob/master/LICENSE) for details.

---

**Need help?** Start with our [Quickstart Guides](./getting-started/) or explore [Scenarios](./scenarios/) to find your use case.