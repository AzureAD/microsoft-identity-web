# Aspire Blazor Calls Web API

This sample demonstrates how to use Microsoft.Identity.Web with Blazor Server and .NET Aspire to:
- Authenticate users with Microsoft Entra ID
- Call a protected Web API with token acquisition
- Handle incremental consent and Conditional Access scenarios
- Use Aspire for service orchestration and discovery

## Projects

### AspireBlazorCallsWebApi.AppHost
The Aspire host application that orchestrates the services using service discovery and manages the application lifecycle.

### AspireBlazorCallsWebApi.ServiceDefaults
Shared configuration for OpenTelemetry, health checks, service discovery, and resilience patterns.

### AspireBlazorCallsWebApi.ApiService
A protected Web API that:
- Requires authentication using Microsoft.Identity.Web
- Exposes a `/weatherforecast` endpoint
- Returns weather forecast data

### AspireBlazorCallsWebApi.Web
A Blazor Server application that:
- Authenticates users with Microsoft Entra ID
- Uses `BlazorAuthenticationChallengeHandler` for incremental consent
- Calls the protected API with automatic token acquisition
- Handles authentication challenges (incremental consent, Conditional Access)
- Uses `MapLoginAndLogout()` for enhanced OIDC endpoints

## Features Demonstrated

### 1. Blazor Authentication Components
- **BlazorAuthenticationChallengeHandler**: Handles authentication challenges in Blazor components
- **MapLoginAndLogout()**: Extension method for mapping login/logout endpoints with support for:
  - Incremental consent (scope parameter)
  - Login hints (pre-fill username)
  - Domain hints (skip home realm discovery)
  - Conditional Access claims

### 2. Incremental Consent
The Weather page demonstrates incremental consent by:
1. Attempting to call the API
2. Catching `MicrosoftIdentityWebChallengeUserException`
3. Using `ChallengeHandler.HandleExceptionAsync()` to redirect for additional consent

### 3. Service Discovery
Uses Aspire's service discovery with the `https+http://` scheme to allow the web app to discover and call the API service.

### 4. OpenTelemetry Integration
Both services are instrumented with OpenTelemetry for:
- Distributed tracing
- Metrics collection
- Logging

## Prerequisites

- .NET 9.0 SDK or later
- An Entra ID (Azure AD) tenant
- App registrations configured (see Configuration section)

## Configuration

The sample uses pre-configured app registrations from the Microsoft Identity Web test lab:

### API Service (AspireBlazorCallsWebApi.ApiService)
- **ClientId**: `a021aff4-57ad-453a-bae8-e4192e5860f3`
- **TenantId**: `10c419d4-4a50-45b2-aa4e-919fb84df24f`
- **Scope**: `access_as_user`

### Web App (AspireBlazorCallsWebApi.Web)
- **ClientId**: `a599ce88-0a5f-4a6e-beca-e67d3fc427f4`
- **TenantId**: `10c419d4-4a50-45b2-aa4e-919fb84df24f`
- **API Scope**: `api://a021aff4-57ad-453a-bae8-e4192e5860f3/access_as_user`

> **Note**: These are test credentials. For your own app, you'll need to:
> 1. Register an API app in Entra ID and expose an API scope
> 2. Register a web app in Entra ID and grant access to the API scope
> 3. Update the `appsettings.json` files with your app registrations

## Running the Sample

### Using Visual Studio
1. Open `AspireBlazorCallsWebApi.sln`
2. Set `AspireBlazorCallsWebApi.AppHost` as the startup project
3. Press F5 to run

### Using Command Line
```bash
cd AspireBlazorCallsWebApi.AppHost
dotnet run
```

The Aspire dashboard will open automatically, showing:
- The web frontend (Blazor Server app)
- The API service
- Traces, logs, and metrics

### Using the Application
1. Click on the web frontend endpoint in the Aspire dashboard
2. Click "Log in" to authenticate
3. Navigate to the "Weather" page
4. The app will call the protected API and display weather data
5. If additional scopes are needed, you'll be redirected to consent

## Code Highlights

### Weather.razor - Incremental Consent Pattern
```csharp
try
{
    forecasts = await WeatherApiClient.GetWeatherAsync();
}
catch (Exception ex)
{
    // Handle authentication challenges (incremental consent, Conditional Access)
    if (await ChallengeHandler.HandleExceptionAsync(ex))
    {
        // User will be redirected to re-authenticate with additional scopes
        return;
    }
    // Handle other errors
    error = $"Failed to load weather data: {ex.Message}";
}
```

### Program.cs - Authentication Setup
```csharp
// Add Microsoft Identity Web authentication
builder.Services.AddMicrosoftIdentityWebAppAuthentication(builder.Configuration, "AzureAd")
    .EnableTokenAcquisitionToCallDownstreamApi()
    .AddInMemoryTokenCaches();

// Register the Blazor authentication challenge handler
builder.Services.AddScoped<BlazorAuthenticationChallengeHandler>();

// Map authentication endpoints with incremental consent support
var authGroup = app.MapGroup("/authentication");
authGroup.MapLoginAndLogout();
```

### WeatherApiClient - Service Discovery
```csharp
builder.Services.AddHttpClient<WeatherApiClient>(client =>
{
    // Uses Aspire service discovery
    client.BaseAddress = new Uri("https+http://apiservice");
})
.AddMicrosoftIdentityAppAuthenticationHandler("AzureAd", options =>
{
    var scopes = builder.Configuration.GetSection("WeatherApi:Scopes").Get<string[]>();
    options.Scopes = string.Join(" ", scopes);
});
```

## Architecture

```
┌─────────────────────┐
│  Aspire AppHost     │
│  (Orchestration)    │
└──────────┬──────────┘
           │
           ├─────────────────────────┐
           │                         │
┌──────────▼──────────┐   ┌─────────▼──────────┐
│  Web (Blazor)       │   │  API Service       │
│  - User Auth        │───│  - Weather API     │
│  - Token Acquisition│   │  - [Authorize]     │
│  - Incremental      │   │                    │
│    Consent          │   │                    │
└─────────────────────┘   └────────────────────┘
         │                         │
         └─────────┬───────────────┘
                   │
         ┌─────────▼──────────┐
         │  Service Defaults  │
         │  - OpenTelemetry   │
         │  - Service Discovery│
         │  - Health Checks   │
         └────────────────────┘
```

## Learn More

- [Microsoft.Identity.Web Documentation](https://aka.ms/ms-id-web)
- [.NET Aspire Documentation](https://learn.microsoft.com/dotnet/aspire)
- [Blazor Server Authentication](https://learn.microsoft.com/aspnet/core/blazor/security/server)
- [Incremental Consent and Conditional Access](https://aka.ms/ms-id-web/incremental-consent)
