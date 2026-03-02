# ASP.NET Framework & .NET Standard Support

This guide provides an overview of Microsoft.Identity.Web support for .NET Framework and .NET Standard applications.

---

## Choose Your Scenario

Microsoft.Identity.Web provides different packages and integration patterns depending on your application type:

### 🔷 MSAL.NET with Microsoft.Identity.Web Packages

**For console apps, daemon services, and non-web .NET Framework applications**

Use Microsoft.Identity.Web.TokenCache and Microsoft.Identity.Web.Certificate packages with MSAL.NET for:
- Token cache serialization (SQL Server, Redis, Cosmos DB)
- Certificate loading from KeyVault, certificate store, or file system
- Console applications and daemon services
- .NET Standard 2.0 libraries

**👉 [MSAL.NET with Microsoft.Identity.Web Guide](msal-dotnet-framework.md)**

---

### 🌐 OWIN Integration for ASP.NET MVC/Web API

**For ASP.NET MVC and Web API applications**

Use Microsoft.Identity.Web.OWIN package for full-featured web authentication with:
- TokenAcquirerFactory for automatic token acquisition
- Controller extensions for easy access to Microsoft Graph and downstream APIs
- Distributed token cache support
- Incremental consent handling

**👉 [OWIN Integration Guide](owin.md)**

---

## Quick Comparison

| Feature | MSAL.NET + TokenCache/Certificate | OWIN Integration |
|---------|-----------------------------------|------------------|
| **Package** | Microsoft.Identity.Web.TokenCache<br>Microsoft.Identity.Web.Certificate | Microsoft.Identity.Web.OWIN |
| **Target** | Console apps, daemons, worker services | ASP.NET MVC, ASP.NET Web API |
| **Authentication** | Manual MSAL.NET configuration | Automatic OWIN middleware |
| **Token Acquisition** | Manual with `IConfidentialClientApplication` | Automatic with controller extensions |
| **Token Cache** | ✅ All providers (SQL, Redis, Cosmos) | ✅ All providers (SQL, Redis, Cosmos) |
| **Certificate Loading** | ✅ KeyVault, store, file, Base64 | ✅ Via MSAL.NET configuration |
| **Microsoft Graph** | Manual `GraphServiceClient` setup | ✅ `this.GetGraphServiceClient()` |
| **Downstream APIs** | Manual HTTP calls with tokens | ✅ `this.GetDownstreamApi()` |
| **Incremental Consent** | Manual challenge handling | ✅ Automatic with `MsalUiRequiredException` |

---

## Overview

Starting with **Microsoft.Identity.Web 1.17+**, you have flexible options for using Microsoft Identity libraries in non-ASP.NET Core environments:

### Available Packages

| Package | Purpose | Target Applications |
|---------|---------|---------------------|
| **Microsoft.Identity.Web.TokenCache** | Token cache serializers for MSAL.NET | Console, daemon, worker services |
| **Microsoft.Identity.Web.Certificate** | Certificate loading utilities | Console, daemon, worker services |
| **Microsoft.Identity.Web.OWIN** | OWIN middleware integration | ASP.NET MVC, ASP.NET Web API |

### Why Use Microsoft.Identity.Web Packages?

| Feature | Benefit |
|---------|---------|
| **Token Cache Serialization** | Reusable cache adapters for in-memory, SQL Server, Redis, Cosmos DB |
| **Certificate Helpers** | Simplified certificate loading from KeyVault, file system, or cert stores |
| **OWIN Integration** | Seamless authentication for ASP.NET MVC/Web API |
| **.NET Standard 2.0** | Compatible with .NET Framework 4.7.2+, .NET Core, and .NET 5+ |
| **Minimal Dependencies** | Targeted packages without ASP.NET Core dependencies |

---

## Next Steps

Choose the guide that matches your application type:

- **Console Apps, Daemons, Worker Services** → [MSAL.NET with Microsoft.Identity.Web](msal-dotnet-framework.md)
- **ASP.NET MVC, ASP.NET Web API** → [OWIN Integration](owin.md)

---

## Sample Applications

### MSAL.NET Samples

- [ConfidentialClientTokenCache](https://github.com/Azure-Samples/active-directory-dotnet-v1-to-v2/tree/master/ConfidentialClientTokenCache) - Console app with token cache
- [active-directory-dotnetcore-daemon-v2](https://github.com/Azure-Samples/active-directory-dotnetcore-daemon-v2) - Daemon with certificate from KeyVault

### OWIN Samples

- [ms-identity-aspnet-webapp-openidconnect](https://github.com/Azure-Samples/ms-identity-aspnet-webapp-openidconnect) - ASP.NET MVC with Microsoft.Identity.Web.OWIN

---

## Additional Resources

- [Token Cache Serialization in MSAL.NET](https://learn.microsoft.com/azure/active-directory/develop/msal-net-token-cache-serialization)
- [Using Certificates with Microsoft.Identity.Web](https://github.com/AzureAD/microsoft-identity-web/wiki/Certificates)
- [OWIN Integration Guide](https://github.com/AzureAD/microsoft-identity-web/wiki/OWIN)
- [NuGet Package Dependencies](https://github.com/AzureAD/microsoft-identity-web/wiki/NuGet-package-references)

---

**Supported Frameworks:** .NET Framework 4.7.2+, .NET Standard 2.0