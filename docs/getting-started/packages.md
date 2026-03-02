# Microsoft.Identity.Web NuGet Packages

Microsoft.Identity.Web is a set of libraries that simplifies adding authentication and authorization support to applications integrating with the Microsoft identity platform. This page provides an overview of all NuGet packages produced by this project.

## 📦 Package Overview

### Core Packages

These packages provide the fundamental functionality for authentication and token management.

| Package | Description |
|---------|-------------|
| **Microsoft.Identity.Web** | The main package that enables ASP.NET Core web apps and web APIs to use the Microsoft identity platform. Used for web applications that sign in users and protected web APIs that optionally call downstream web APIs. |
| **Microsoft.Identity.Web.UI** | Provides UI components for ASP.NET Core web apps that use Microsoft.Identity.Web, including sign-in/sign-out controllers and views. |
| **Microsoft.Identity.Web.TokenAcquisition** | Implementation for higher-level API for confidential client applications (ASP.NET Core and SDK/.NET). Handles token acquisition and management. |
| **Microsoft.Identity.Web.TokenCache** | Provides token cache serializers for MSAL.NET confidential client applications. Supports in-memory, distributed, and session-based caching. |

### Credential Management Packages

These packages handle different authentication credential types.

| Package | Description |
|---------|-------------|
| **Microsoft.Identity.Web.Certificate** | Provides certificate management capabilities for MSAL.NET, including loading certificates from Azure Key Vault and local stores. |
| **Microsoft.Identity.Web.Certificateless** | Enables certificateless authentication scenarios such as managed identities and workload identity federation. |

### Downstream API & Integration Packages

These packages help you call protected APIs and integrate with Azure services.

| Package | Description |
|---------|-------------|
| **Microsoft.Identity.Web.DownstreamApi** | Provides a higher-level interface for calling downstream protected APIs from confidential client applications with automatic token management. |
| **Microsoft.Identity.Web.Azure** | Enables ASP.NET Core web apps and web APIs to use the Azure SDKs with the Microsoft identity platform, providing `TokenCredential` implementations. |
| **Microsoft.Identity.Web.OWIN** | Enables ASP.NET web apps (OWIN/Katana) and web APIs on .NET Framework to use the Microsoft identity platform. Specifically for web applications that sign in users and protected web APIs that optionally call downstream web APIs. |

### Microsoft Graph Packages

These packages provide integration with Microsoft Graph for calling Microsoft 365 services.

| Package | Description |
|---------|-------------|
| **Microsoft.Identity.Web.MicrosoftGraph** | Enables web applications and web APIs to call Microsoft Graph using the Microsoft Graph SDK v4. For web apps that sign in users and call Microsoft Graph, and protected web APIs that call Microsoft Graph. |
| **Microsoft.Identity.Web.MicrosoftGraphBeta** | Enables web applications and web APIs to call Microsoft Graph Beta using the Microsoft Graph SDK v4. For accessing preview features not yet available in the production Graph API. |
| **Microsoft.Identity.Web.GraphServiceClient** | Enables web applications and web APIs to call Microsoft Graph using the Microsoft Graph SDK v5 and above. Recommended for new projects using the latest Graph SDK. |
| **Microsoft.Identity.Web.GraphServiceClientBeta** | Enables web applications and web APIs to call Microsoft Graph Beta using the Microsoft Graph SDK v5 and above. For accessing preview features with the latest Graph SDK. |

### Advanced Scenarios Packages

These packages support specialized authentication scenarios.

| Package | Description |
|---------|-------------|
| **Microsoft.Identity.Web.Diagnostics** | Provides diagnostic and logging support for troubleshooting authentication issues in Microsoft.Identity.Web. |
| **Microsoft.Identity.Web.OidcFIC** | Implementation for Cloud Federation Identity Credential (FIC) credential provider. Enables cross-cloud authentication scenarios. |
| **Microsoft.Identity.Web.AgentIdentities** | Helper methods for Agent identity blueprint to act as agent identities. Enables building autonomous agents and copilot scenarios. |

## 🎯 Choosing the Right Package

### For Web Applications (Sign in users)

```bash
dotnet add package Microsoft.Identity.Web
dotnet add package Microsoft.Identity.Web.UI
```

### For Protected Web APIs

```bash
dotnet add package Microsoft.Identity.Web
```

### For Daemon Applications / Background Services

```bash
dotnet add package Microsoft.Identity.Web.TokenAcquisition
```

### For Calling Microsoft Graph

**Using Graph SDK v5+ (Recommended):**
```bash
dotnet add package Microsoft.Identity.Web.GraphServiceClient
```

**Using Graph SDK v4:**
```bash
dotnet add package Microsoft.Identity.Web.MicrosoftGraph
```

### For Using Azure SDKs

```bash
dotnet add package Microsoft.Identity.Web.Azure
```

### For Calling Custom Downstream APIs

```bash
dotnet add package Microsoft.Identity.Web.DownstreamApi
```

### For Agent/Copilot Scenarios

```bash
dotnet add package Microsoft.Identity.Web.AgentIdentities
```

### For OWIN Applications (.NET Framework)

```bash
dotnet add package Microsoft.Identity.Web.OWIN
```

## 📚 Related Documentation

- [Quick Start: Sign in users in a Web App](./quickstart-webapp.md)
- [Quick Start: Protect a Web API](./quickstart-webapi.md)
- [Daemon Applications & Agent Identities](./daemon-app.md)
- [Calling Downstream APIs](../calling-downstream-apis/calling-downstream-apis-README.md)
- [Agent Identities Guide](../calling-downstream-apis/AgentIdentities-Readme.md)

## 🔗 NuGet Gallery

All packages are available on [NuGet.org](https://www.nuget.org/packages?q=Microsoft.Identity.Web).
