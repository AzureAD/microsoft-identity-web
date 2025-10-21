# Calling Azure SDKs with MicrosoftIdentityTokenCredential

This guide shows how to authenticate Azure SDK clients using `MicrosoftIdentityTokenCredential`. KeyVault credential examples removed (per review) to keep focus on unified credential + DI guidance.

## Overview
Implements `TokenCredential`, reuses Microsoft.Identity.Web token acquisition & caching.

### Benefits
- Unified auth (web apps / APIs / daemons)
- Delegated & app-only
- Agent identities
- Managed identity support

## Installation
```bash
dotnet add package Microsoft.Identity.Web.Azure
```

## Recommended ASP.NET Core Setup
```csharp
builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"))
    .EnableTokenAcquisitionToCallDownstreamApi()
    .AddInMemoryTokenCaches();

builder.Services.AddTokenAcquisition();
builder.Services.AddMicrosoftIdentityAzureTokenCredential();

builder.Services.AddAzureClients(clientBuilder =>
{
    clientBuilder.UseCredential(
        sp => sp.GetRequiredService<MicrosoftIdentityTokenCredential>());

    clientBuilder.AddBlobServiceClient(new Uri("https://myaccount.blob.core.windows.net"));
    clientBuilder.AddServiceBusClient("myservicebus.servicebus.windows.net");
});
```
(Per Azure SDK DI guidance.)

## Delegated vs App
```csharp
var credential = sp.GetRequiredService<MicrosoftIdentityTokenCredential>();
credential.Options.RequestAppToken = true; // app-only (.default)
credential.Options.Scopes = new[] {"https://storage.azure.com/user_impersonation"}; // delegated example
```

## Agent Identities
Secret examples removed (agents don’t have secrets).
```csharp
credential.Options.WithAgentIdentity("my-agent");
credential.Options.RequestAppToken = true;
```

## FIC+Managed Identity Integration
```json
{
  "AzureAd": {
    "ClientCredentials": [
      { "SourceType": "SignedAssertionFromManagedIdentity" }
    ]
  }
}
```
User-assigned add `ManagedIdentityClientId`.

## Resilience
Use standard resilience handler instead of manual retry:
```csharp
builder.Services.AddHttpClient("AzureHttp")
    .AddStandardResilienceHandler(o => o.Retry.MaxRetries = 3);
```

## Troubleshooting
- ManagedIdentityCredential failed: enable MI, assign roles.
- Scope errors: ensure resource-specific scope or `.default`.
- Local dev: fall back to client secret until deployed.

## Related
- Azure SDK DI guidance
- Managed Identity docs
- Credentials configuration
- Overview README

---
**Next Steps**: Register clients with `AddAzureClients`, set correct scopes, adopt resilience handler.