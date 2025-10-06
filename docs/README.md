# Microsoft Identity Web - Documentation Home Page

## Introduction
Welcome to the Microsoft Identity Web documentation! This guide covers the use of Microsoft Identity for ASP.NET Core, OWIN, and .NET applications of all types. Whether you're building web applications, APIs, or background services, this documentation will help you understand how to integrate Microsoft identity features effectively.

## What's Included
- **Token Acquisition**
  - Learn how to acquire tokens for downstream APIs.

## Calling APIs
- Understand how to call APIs using agent identities, which allows your application to authenticate and call APIs on behalf of users or as itself.

## Configuration Approaches
To set up your application, copy the following example `appsettings.json` configuration:

```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "Domain": "YOUR_DOMAIN",
    "TenantId": "YOUR_TENANT_ID",
    "ClientId": "YOUR_CLIENT_ID",
    "ClientSecret": "YOUR_CLIENT_SECRET"
  }
}
```

## Code Examples
Here are some updated code examples demonstrating explicit authentication schemes:

```csharp
// Example of using explicit authentication
var token = await app.AcquireTokenForClient(scopes).ExecuteAsync();
```

## Daemon Scenario
For daemon applications, ensure you use the `TokenAcquirerFactory` to manage token acquisition effectively.

## Package Architecture
| Package Name | Description |
|--------------|-------------|
| Microsoft.Identity.Web | Core package for ASP.NET authentication. |
| Microsoft.Identity.Web.MicrosoftGraph | Integration with Microsoft Graph. |

## Credentials Guide
Refer to the following links for guidance on managing and securing your credentials:
- [Managing Azure AD Credentials](link)
- [Best Practices for Credential Management](link)

## .NET Version Support
| .NET Version | Supported |
|--------------|-----------|
| .NET 5      | Yes       |
| .NET 6      | Yes       |
| .NET 7      | Planned   |

## Documentation Structure Navigation
- [Getting Started](link)
- [Authentication Scenarios](link)
- [API Reference](link)