---
name: entra-id-aspire-authentication
description: |
  Guide for adding Microsoft Entra ID (Azure AD) authentication to .NET Aspire applications.
  Use this when asked to add authentication, Entra ID, Azure AD, OIDC, or identity to an Aspire app,
  or when working with Microsoft.Identity.Web in Aspire projects.
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

## Pre-Implementation Checklist

Before starting, the agent MUST:

### 1. Detect Project Types

Scan each project's `Program.cs` to identify its type:

```powershell
# Find all Program.cs files in solution
Get-ChildItem -Recurse -Filter "Program.cs" | ForEach-Object {
    $content = Get-Content $_.FullName -Raw
    $projectDir = Split-Path $_.FullName -Parent
    $projectName = Split-Path $projectDir -Leaf
    
    # Skip AppHost and ServiceDefaults
    if ($projectName -match "AppHost|ServiceDefaults") { return }
    
    $isWebApp = $content -match "AddRazorComponents|MapRazorComponents|AddServerSideBlazor"
    $isApi = $content -match "MapGet|MapPost|MapPut|MapDelete|AddControllers"
    
    if ($isWebApp) {
        Write-Host "WEB APP: $projectName (has Razor/Blazor components)"
    } elseif ($isApi) {
        Write-Host "API: $projectName (exposes endpoints)"
    }
}
```

**Detection rules:**
| Pattern in `Program.cs` | Project Type |
|------------------------|--------------|
| `AddRazorComponents` / `MapRazorComponents` / `AddServerSideBlazor` | **Blazor Web App** |
| `MapGet` / `MapPost` / `AddControllers` (without Razor) | **Web API** |

> **Note:** APIs can call other APIs (downstream). The Aspire `.WithReference()` shows service dependencies, not necessarily web-to-API relationships.

### 2. Confirm with User

**AGENT: Show detected topology and ask for confirmation:**
> "I detected:
> - **Web App** (Blazor): `{webProjectName}`
> - **API**: `{apiProjectName}`
> 
> The web app will authenticate users and call the API. Is this correct?"

### 3. Establish Workflow

**AGENT: Explain the two-phase approach:**
> "I'll implement authentication in two phases:
> 
> **Phase 1 (now):** Add authentication code with placeholder values. The app will **build** but won't **run** until app registrations are configured.
> 
> **Phase 2 (after):** Use the `entra-id-aspire-provisioning` skill to create Entra ID app registrations and update the configuration with real values.
> 
> Ready to proceed with Phase 1?"

---

## Implementation Checklist

**CRITICAL: Complete ALL steps in order. Do not skip any step.**

### API Project Steps
- [ ] Step 1.1: Add Microsoft.Identity.Web package
- [ ] Step 1.2: Update appsettings.json with AzureAd section
- [ ] Step 1.3: Update Program.cs with JWT Bearer authentication
- [ ] Step 1.4: Add RequireAuthorization() to protected endpoints

### Web/Blazor Project Steps
- [ ] Step 2.1: Add Microsoft.Identity.Web package (v3.3.0+ includes Blazor helpers)
- [ ] Step 2.2: Update appsettings.json with AzureAd and scopes
- [ ] Step 2.3: Update Program.cs with OIDC, token acquisition, and **BlazorAuthenticationChallengeHandler**
- [ ] Step 2.4: Create UserInfo.razor component (LOGIN BUTTON)
- [ ] Step 2.5: Update MainLayout.razor to include UserInfo
- [ ] Step 2.6: Update Routes.razor with AuthorizeRouteView
- [ ] Step 2.7: Store client secret in user-secrets
- [ ] Step 2.8: Add try/catch with ChallengeHandler on **every page calling APIs**

---

## Step-by-Step Implementation

### Prerequisites

1. .NET Aspire solution with API and Web (Blazor) projects
2. Azure AD tenant

> **Two-phase workflow:**
> - **Phase 1**: Add authentication code with placeholder values → App will **build** but **not run**
> - **Phase 2**: Run `entra-id-aspire-provisioning` skill to create app registrations → App will **run**

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

// Add Blazor authentication challenge handler for incremental consent and Conditional Access
builder.Services.AddScoped<BlazorAuthenticationChallengeHandler>();

// HttpClient with automatic token attachment
builder.Services.AddHttpClient<WeatherApiClient>(client =>
{
    client.BaseAddress = new("https+http://apiservice"); // Aspire service discovery
})
.AddMicrosoftIdentityMessageHandler(builder.Configuration.GetSection("WeatherApi").Bind);

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
app.MapGroup("/authentication").MapLoginAndLogout();

app.Run();
```

> 💡 **Important:** The Blazor helpers (`BlazorAuthenticationChallengeHandler` and `LoginLogoutEndpointRouteBuilderExtensions`) ship in the `Microsoft.Identity.Web` NuGet package (v3.3.0+). Simply add `using Microsoft.Identity.Web;` — no file copying required.
>
> **For older package versions**: If you're using Microsoft.Identity.Web < v3.3.0, you can copy the helper files from this skill folder as a workaround. See the files `BlazorAuthenticationChallengeHandler.cs` and `LoginLogoutEndpointRouteBuilderExtensions.cs` in this directory.

**2.4 Create UserInfo Component (`Components/UserInfo.razor`) — THE LOGIN BUTTON:**

> **CRITICAL: This step is frequently forgotten. Without this, users have no way to log in!**

```razor
@using Microsoft.AspNetCore.Components.Authorization

<AuthorizeView>
    <Authorized>
        <span class="nav-item">Hello, @context.User.Identity?.Name</span>
        <form action="/authentication/logout" method="post" class="nav-item">
            <AntiforgeryToken />
            <input type="hidden" name="returnUrl" value="/" />
            <button type="submit" class="btn btn-link nav-link">Logout</button>
        </form>
    </Authorized>
    <NotAuthorized>
        <a href="/authentication/login?returnUrl=/" class="nav-link">Login</a>
    </NotAuthorized>
</AuthorizeView>
```

**2.5 Update MainLayout.razor to include UserInfo:**

Find the `<main>` or navigation section in `Components/Layout/MainLayout.razor` and add the UserInfo component:

```razor
@inherits LayoutComponentBase

<div class="page">
    <div class="sidebar">
        <NavMenu />
    </div>

    <main>
        <div class="top-row px-4">
            <UserInfo />  @* <-- ADD THIS LINE *@
        </div>

        <article class="content px-4">
            @Body
        </article>
    </main>
</div>
```

**2.6 Update Routes.razor for AuthorizeRouteView:**

Replace `RouteView` with `AuthorizeRouteView` in `Components/Routes.razor`:

```razor
@using Microsoft.AspNetCore.Components.Authorization

<Router AppAssembly="typeof(Program).Assembly">
    <Found Context="routeData">
        <AuthorizeRouteView RouteData="routeData" DefaultLayout="typeof(Layout.MainLayout)">
            <NotAuthorized>
                <p>You are not authorized to view this page.</p>
                <a href="/authentication/login">Login</a>
            </NotAuthorized>
        </AuthorizeRouteView>
        <FocusOnNavigate RouteData="routeData" Selector="h1" />
    </Found>
</Router>
```

**2.7 Store Client Secret in User Secrets:**

> **Never commit secrets to source control!**

```powershell
cd MyService.Web
dotnet user-secrets init
dotnet user-secrets set "AzureAd:ClientCredentials:0:ClientSecret" "<your-client-secret>"
```

Then update `appsettings.json` to reference user secrets (remove the hardcoded secret):
```jsonc
{
  "AzureAd": {
    "ClientCredentials": [
      {
        // For more options see https://aka.ms/ms-id-web/credentials
        "SourceType": "ClientSecret"
      }
    ]
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

### Handle Conditional Access / MFA / Incremental Consent

> **This is NOT optional** — Blazor Server requires explicit exception handling for Conditional Access and consent.

When calling APIs, Conditional Access policies or consent requirements can trigger `MicrosoftIdentityWebChallengeUserException`. You MUST handle this on **every page that calls a downstream API**.

**Step 2.3 registers the handler** — `AddScoped<BlazorAuthenticationChallengeHandler>()` makes the service available.

**Each page calling APIs needs this pattern:**

```razor
@page "/weather"
@attribute [Authorize]

@using Microsoft.AspNetCore.Authorization
@using Microsoft.Identity.Web

@inject WeatherApiClient WeatherApi
@inject BlazorAuthenticationChallengeHandler ChallengeHandler

<PageTitle>Weather</PageTitle>

<h1>Weather</h1>

@if (!string.IsNullOrEmpty(errorMessage))
{
    <div class="alert alert-warning">@errorMessage</div>
}
else if (forecasts == null)
{
    <p><em>Loading...</em></p>
}
else
{
    @* Display your data *@
}

@code {
    private WeatherForecast[]? forecasts;
    private string? errorMessage;

    protected override async Task OnInitializedAsync()
    {
        if (!await ChallengeHandler.IsAuthenticatedAsync())
        {
            // Not authenticated - redirect to login with required scopes
            await ChallengeHandler.ChallengeUserWithConfiguredScopesAsync("WeatherApi:Scopes");
            return;
        }

        try
        {
            forecasts = await WeatherApi.GetWeatherAsync();
        }
        catch (Exception ex)
        {
            // Handle incremental consent / Conditional Access
            if (!await ChallengeHandler.HandleExceptionAsync(ex))
            {
                errorMessage = $"Error loading data: {ex.Message}";
            }
        }
    }
}
```

> **Why this pattern?**
> 1. `IsAuthenticatedAsync()` checks if user is signed in before making API calls
> 2. `HandleExceptionAsync()` catches `MicrosoftIdentityWebChallengeUserException` (or as InnerException)
> 3. If it is a challenge exception → redirects user to re-authenticate with required claims/scopes
> 4. If it is NOT a challenge exception → returns false so you can handle the error

> **Why is this not automatic?** Blazor Server's circuit-based architecture requires explicit handling. The handler re-challenges the user by navigating to the login endpoint with the required claims/scopes.

---

## Troubleshooting

| Issue | Solution |
|-------|----------|
| 401 on API calls | Verify scopes match the API's App ID URI |
| OIDC redirect fails | Add `/signin-oidc` to Azure AD redirect URIs |
| Token not attached | Ensure `AddMicrosoftIdentityMessageHandler` is configured |
| AADSTS65001 | Admin consent required - grant in Azure Portal |
| 404 on `/MicrosoftIdentity/Account/Challenge` | Use `BlazorAuthenticationChallengeHandler` instead of `MicrosoftIdentityConsentHandler` |

---

## Key Files to Modify

| Project | File | Purpose |
|---------|------|---------|
| ApiService | `Program.cs` | JWT auth + `RequireAuthorization()` |
| ApiService | `appsettings.json` | AzureAd config (ClientId, TenantId) |
| Web | `Program.cs` | OIDC + token acquisition + challenge handler registration |
| Web | `appsettings.json` | AzureAd config + downstream API scopes |
| Web | `Components/UserInfo.razor` | **Login/logout button UI** |
| Web | `Components/Layout/MainLayout.razor` | Include UserInfo in layout |
| Web | `Components/Routes.razor` | AuthorizeRouteView for protected pages |

> **Note:** The Blazor helpers (`BlazorAuthenticationChallengeHandler` and `LoginLogoutEndpointRouteBuilderExtensions`) are now included in Microsoft.Identity.Web (v3.3.0+). No manual file copying is required.

---

## Post-Implementation Verification

**AGENT: After completing all steps, verify:**

1. **Build succeeds:**
   ```powershell
   dotnet build
   ```

2. **Check all files were created/modified:**
   - [ ] API `Program.cs` has `AddMicrosoftIdentityWebApi`
   - [ ] API `appsettings.json` has `AzureAd` section
   - [ ] Web `Program.cs` has `AddMicrosoftIdentityWebApp` and `AddMicrosoftIdentityMessageHandler`
   - [ ] Web `Program.cs` has `AddScoped<BlazorAuthenticationChallengeHandler>()`
   - [ ] Web `appsettings.json` has `AzureAd` and scope configuration
   - [ ] Web has `Components/UserInfo.razor` (**LOGIN BUTTON**)
   - [ ] Web `MainLayout.razor` includes `<UserInfo />`
   - [ ] Web `Routes.razor` uses `AuthorizeRouteView`
   - [ ] **Every page calling protected APIs** has try/catch with `ChallengeHandler.HandleExceptionAsync(ex)`

3. **AGENT: Inform user of next step:**
   > "✅ **Phase 1 complete!** Authentication code is in place. The app will **build** but **won't run** until app registrations are configured.
   > 
   > **Next:** Run the `entra-id-aspire-provisioning` skill to:
   > - Create Entra ID app registrations
   > - Update `appsettings.json` with real ClientIds
   > - Store client secret securely
   > 
   > Ready to proceed with provisioning?"

---

## Resources

- 📖 **[Full Aspire Integration Guide](https://github.com/AzureAD/microsoft-identity-web/blob/master/docs/frameworks/aspire.md)** - Comprehensive documentation with diagrams, detailed explanations, and advanced scenarios
- [Microsoft.Identity.Web Documentation](https://github.com/AzureAD/microsoft-identity-web/tree/master/docs)
- [MicrosoftIdentityMessageHandler Guide](https://github.com/AzureAD/microsoft-identity-web/blob/master/docs/calling-downstream-apis/custom-apis.md#microsoftidentitymessagehandler---for-httpclient-integration)
- [.NET Aspire Service Discovery](https://learn.microsoft.com/dotnet/aspire/service-discovery/overview)
- [Credentials Guide](https://github.com/AzureAD/microsoft-identity-web/blob/master/docs/authentication/credentials/credentials-README.md)
