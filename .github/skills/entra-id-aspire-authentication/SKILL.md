---
name: entra-id-aspire-authentication
description: Guide for adding Microsoft Entra ID (Azure AD) authentication to .NET Aspire applications. Use this when asked to add authentication, Entra ID, Azure AD, OIDC, or identity to an Aspire app, or when working with Microsoft.Identity.Web in Aspire projects.
license: MIT
---

# Entra ID Authentication for .NET Aspire Applications

This skill helps you integrate **Microsoft Entra ID** (Azure AD) authentication into **.NET Aspire** distributed applications using **Microsoft.Identity.Web**.

## When to Use This Skill

- Adding user authentication to Aspire apps
- Protecting APIs with JWT Bearer authentication
- Configuring OIDC sign-in for Blazor Server
- Setting up token acquisition for downstream API calls
- Implementing service-to-service authentication

## Architecture Overview

```
User Browser → Blazor Server (OIDC) → Entra ID → Access Token → Protected API (JWT)
```

**Key Components:**
- **Blazor Frontend**: Uses `AddMicrosoftIdentityWebApp` for OIDC + `MicrosoftIdentityMessageHandler` for token attachment
- **API Backend**: Uses `AddMicrosoftIdentityWebApi` for JWT validation
- **Aspire**: Service discovery with `https+http://servicename` URLs

---

## Step-by-Step Implementation

### Prerequisites

1. Azure AD tenant with two app registrations:
   - **Web app** (Blazor): with redirect URI `{app-url}/signin-oidc`
   - **API app**: exposing scopes (App ID URI like `api://<client-id>`)
2. Client credentials for the web app (secret or certificate)

### Part 1: Protect the API with JWT Bearer

**1.1 Add Package:**
```powershell
cd MyService.ApiService
dotnet add package Microsoft.Identity.Web
```

**1.2 Configure `appsettings.json`:**
```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "<your-tenant-id>",
    "ClientId": "<your-api-client-id>",
    "Audiences": ["api://<your-api-client-id>"]
  }
}
```

**1.3 Update `Program.cs`:**
```csharp
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Web;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

// Add JWT Bearer authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));

builder.Services.AddAuthorization();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

// Protect endpoints
app.MapGet("/weatherforecast", () => { /* ... */ })
    .RequireAuthorization();

app.Run();
```

### Part 2: Configure Blazor Frontend

**2.1 Add Package:**
```powershell
cd MyService.Web
dotnet add package Microsoft.Identity.Web
```

**2.2 Configure `appsettings.json`:**
```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "Domain": "<your-tenant>.onmicrosoft.com",
    "TenantId": "<tenant-guid>",
    "ClientId": "<web-app-client-id>",
    "CallbackPath": "/signin-oidc",
    "ClientCredentials": [
      {
        "SourceType": "ClientSecret",
        "ClientSecret": "<your-client-secret>"
      }
    ]
  },
  "WeatherApi": {
    "Scopes": ["api://<api-client-id>/.default"]
  }
}
```

**2.3 Update `Program.cs`:**
```csharp
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Identity.Web;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

// Authentication + token acquisition
builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"))
    .EnableTokenAcquisitionToCallDownstreamApi()
    .AddInMemoryTokenCaches();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddServerSideBlazor().AddMicrosoftIdentityConsentHandler();

// HttpClient with automatic token attachment
builder.Services.AddHttpClient<WeatherApiClient>(client =>
{
    client.BaseAddress = new("https+http://apiservice"); // Aspire service discovery
})
.AddMicrosoftIdentityMessageHandler(builder.Configuration.GetSection("WeatherApi"));

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
app.MapGroup("/authentication").MapLoginAndLogout();

app.Run();
```

**2.4 Create Login/Logout Endpoints (`LoginLogoutEndpointRouteBuilderExtensions.cs`):**
```csharp
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Mvc;

namespace MyService.Web;

internal static class LoginLogoutEndpointRouteBuilderExtensions
{
    internal static IEndpointConventionBuilder MapLoginAndLogout(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("");

        group.MapGet("/login", (string? returnUrl) => TypedResults.Challenge(GetAuthProperties(returnUrl)))
            .AllowAnonymous();

        group.MapPost("/logout", ([FromForm] string? returnUrl) => TypedResults.SignOut(GetAuthProperties(returnUrl),
            [CookieAuthenticationDefaults.AuthenticationScheme, OpenIdConnectDefaults.AuthenticationScheme]));

        return group;
    }

    private static AuthenticationProperties GetAuthProperties(string? returnUrl)
    {
        const string pathBase = "/";
        if (string.IsNullOrEmpty(returnUrl)) returnUrl = pathBase;
        else if (returnUrl.StartsWith("//", StringComparison.Ordinal)) returnUrl = pathBase; // Prevent protocol-relative redirects
        else if (!Uri.IsWellFormedUriString(returnUrl, UriKind.Relative)) returnUrl = new Uri(returnUrl, UriKind.Absolute).PathAndQuery;
        else if (returnUrl[0] != '/') returnUrl = $"{pathBase}{returnUrl}";
        return new AuthenticationProperties { RedirectUri = returnUrl };
    }
}
```

---

## Common Patterns

### Protect Blazor Pages
```razor
@page "/weather"
@attribute [Authorize]
```

### Scope Validation in API
```csharp
app.MapGet("/weatherforecast", () => { /* ... */ })
    .RequireAuthorization()
    .RequireScope("access_as_user");
```

### App-Only Tokens (Service-to-Service)
```csharp
.AddMicrosoftIdentityMessageHandler(options =>
{
    options.Scopes.Add("api://<api-client-id>/.default");
    options.RequestAppToken = true;
});
```

### Override Scopes Per Request
```csharp
var request = new HttpRequestMessage(HttpMethod.Get, "/endpoint")
    .WithAuthenticationOptions(options =>
    {
        options.Scopes.Clear();
        options.Scopes.Add("api://<client-id>/specific.scope");
    });
```

### Production: Use Managed Identity
```json
{
  "AzureAd": {
    "ClientCredentials": [
      {
        "SourceType": "SignedAssertionFromManagedIdentity",
        "ManagedIdentityClientId": "<user-assigned-mi-client-id>"
      }
    ]
  }
}
```

### On-Behalf-Of (API calling downstream APIs)
```csharp
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"))
    .EnableTokenAcquisitionToCallDownstreamApi()
    .AddInMemoryTokenCaches();

builder.Services.AddDownstreamApi("GraphApi", builder.Configuration.GetSection("GraphApi"));
```

---

## Troubleshooting

| Issue | Solution |
|-------|----------|
| 401 on API calls | Verify scopes match the API's App ID URI |
| OIDC redirect fails | Add `/signin-oidc` to Azure AD redirect URIs |
| Token not attached | Ensure `AddMicrosoftIdentityMessageHandler` is configured |
| AADSTS65001 | Admin consent required - grant in Azure Portal |

---

## Key Files to Modify

| Project | File | Purpose |
|---------|------|---------|
| ApiService | `Program.cs` | JWT auth + `RequireAuthorization()` |
| ApiService | `appsettings.json` | AzureAd config (ClientId, TenantId) |
| Web | `Program.cs` | OIDC + token acquisition + message handler |
| Web | `appsettings.json` | AzureAd config + downstream API scopes |

---

## Resources

- 📖 **[Full Aspire Integration Guide](https://github.com/AzureAD/microsoft-identity-web/blob/master/docs/frameworks/aspire.md)** - Comprehensive documentation with diagrams, detailed explanations, and advanced scenarios
- [Microsoft.Identity.Web Documentation](https://github.com/AzureAD/microsoft-identity-web/tree/master/docs)
- [MicrosoftIdentityMessageHandler Guide](https://github.com/AzureAD/microsoft-identity-web/blob/master/docs/calling-downstream-apis/custom-apis.md#microsoftidentitymessagehandler---for-httpclient-integration)
- [.NET Aspire Service Discovery](https://learn.microsoft.com/dotnet/aspire/service-discovery/overview)
- [Credentials Guide](https://github.com/AzureAD/microsoft-identity-web/blob/master/docs/authentication/credentials/credentials-README.md)
