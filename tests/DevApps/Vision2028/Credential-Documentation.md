# Credential Class Documentation

## Overview

The `Credential` class represents an authenticated identity in the Microsoft Identity ecosystem. It serves as a key abstraction for managing authentication credentials across various scenarios, including agent identities, federated identity credentials (FIC), and cross-cloud authentication.

## Purpose

`Credential` encapsulates the authentication state and identity information needed to acquire tokens and make authenticated API calls. It acts as a portable, reusable representation of an authenticated entity that can be:

- Created from application registrations with client credentials
- Exchanged for federated identity credentials
- Used to obtain access tokens for downstream APIs
- Passed between different authentication contexts

---

## Key Usage Patterns

### 1. Creating a Credential with Client Credentials

Create a `Credential` for an application using certificate-based authentication:

```csharp
Credential agentBlueprintCredentials = mise.GetCredential(new()
{
    ClientId = "c4b2d4d9-9257-4c1a-a5c0-0a4907c83411",
    TenantId = "31a58c3b-ae9c-4448-9e8f-e9e143e800df",
    ClientCredentials = [
         CertificateDescription.FromStoreWithDistinguishedName(
             "CN=LabAuth.MSIDLab.com", 
             StoreLocation.LocalMachine, 
             StoreName.My)
    ]
});
```

**When to use:**
- Initial authentication for an application or service
- When you have client credentials (certificate, client secret, or managed identity)
- As the starting point for credential chains

---

### 2. Exchanging Credentials for Federated Identity

Exchange an existing credential for a federated identity credential (FIC):

```csharp
Credential agentBlueprintFic = await mise.ExchangeCredentialAsync(
    agentBlueprintCredentials, 
    new()
    {
        FmiPath = "44250d7d-2362-4fba-9ba0-49c19ae270e0"
    });
```

**When to use:**
- Implementing agent identity scenarios
- When you need to act on behalf of another identity
- For token exchange flows with FMI (Federated Managed Identity) paths

---

### 3. Deriving a New Credential from an Existing One

Create a new credential based on a federated identity credential:

```csharp
Credential agentIdentityCredentials = mise.GetCredential(
    agentBlueprintFic, 
    new()
    {
        ClientId = "44250d7d-2362-4fba-9ba0-49c19ae270e0",
        TenantId = "31a58c3b-ae9c-4448-9e8f-e9e143e800df"
    });
```

**When to use:**
- After exchanging for a FIC token
- To create a credential with specific client and tenant properties
- To prepare credentials for downstream token acquisition

---

### 4. Using Credentials to Acquire Tokens

Once you have a `Credential`, use it to obtain access tokens:

#### Option A: Get Token String

```csharp
string token = await mise.GetToken(
    agentIdentityCredentials, 
    new()
    {
        Scopes = ["https://graph.microsoft.com/.default"]
    });
```

#### Option B: Get Authorization Header

```csharp
var authorizationHeader = await mise.GetAuthorizationHeaderAsync(
    agentIdentityCredentials, 
    new DownstreamApiOptions()
    {
        Scopes = ["https://graph.microsoft.com/.default"]
    });
```

**When to use:**
- Making authenticated calls to downstream APIs
- After establishing a credential chain
- For any scenario requiring an access token

---

## Advanced Scenarios

### Cross-Cloud Authentication

Use `Credential` for cross-cloud scenarios (e.g., Azure Government to Azure Public Cloud):

```csharp
// Step 1: Create credential for Government Cloud app
Credential appInFairFaxCredentials = mise.GetCredential(new()
{
    Instance = "https://login.microsoftonline.us/",
    ClientId = "FairFax AppID",
    TenantId = "TenantId in public FairFax",
    
    // Not using client credentials. They are guessed from the platform.
    // For instance managed certificate of System Assigned Identity.
});

// Step 2: Exchange automatically detects cloud boundaries
Credential FicForAppInFairFax = await mise.ExchangeCredentialAsync(
    agentIdentityCredentials);

// Step 3: Create credential for Public Cloud
Credential CredentialInPublicCloud = mise.GetCredential(
    appInFairFaxCredentials, 
    new()
    {
        Instance = "https://login.microsoftonline.us/",
        ClientId = "Public cloud AppId",
        TenantId = "TenantId in public cloud"
    });

// Step 4: Get authorization header for Public Cloud API
var authorizationHeaderGccM = await mise.GetAuthorizationHeaderAsync(
    CredentialInPublicCloud, 
    new DownstreamApiOptions()
    {
        Scopes = ["https://graph.microsoft.com/.default"]
    });
```

**Use cases:**
- Azure Government to Azure Public Cloud scenarios
- Cross-sovereign cloud authentication
- GCC-M (Government Community Cloud - Moderate) to Commercial Cloud

---

## Credential Lifecycle

### Typical Flow Pattern

```
1. Create Base Credential (with client credentials)
   ?
2. Exchange for FIC (optional, for agent scenarios)
   ?
3. Derive Identity Credential (with specific ClientId/TenantId)
   ?
4. Acquire Access Token (for downstream API)
```

### Example Complete Flow

```csharp
// 1. Start with agent blueprint credentials
Credential blueprint = mise.GetCredential(new()
{
    ClientId = agentBluePrintAppId,
    TenantId = tenantId,
    ClientCredentials = [certificate]
});

// 2. Exchange for FIC
Credential fic = await mise.ExchangeCredentialAsync(blueprint, new()
{
    FmiPath = agentIdentity
});

// 3. Get agent identity credentials
Credential identity = mise.GetCredential(fic, new()
{
    ClientId = agentIdentity,
    TenantId = tenantId
});

// 4. Use to call Graph
string token = await mise.GetToken(identity, new()
{
    Scopes = ["https://graph.microsoft.com/.default"]
});
```

---

## Best Practices

### 1. Reusability
Store and reuse `Credential` instances rather than recreating them for each token request:

```csharp
// ? Good - Create once, use multiple times
var credential = mise.GetCredential(options);
var token1 = await mise.GetToken(credential, graphScopes);
var token2 = await mise.GetToken(credential, azureScopes);

// ? Avoid - Creating multiple times unnecessarily
var token1 = await mise.GetToken(mise.GetCredential(options), graphScopes);
var token2 = await mise.GetToken(mise.GetCredential(options), azureScopes);
```

### 2. Credential Chaining
Leverage credential exchange and derivation for agent identity scenarios:

```csharp
// Chain credentials for complex scenarios
var baseCredential = mise.GetCredential(baseOptions);
var exchangedCredential = await mise.ExchangeCredentialAsync(baseCredential, exchangeOptions);
var finalCredential = mise.GetCredential(exchangedCredential, finalOptions);
```

### 3. Automatic Detection
When possible, omit client credentials to allow the platform to automatically detect managed identities or certificates:

```csharp
// Platform will detect System Assigned Managed Identity
Credential autoCredential = mise.GetCredential(new()
{
    ClientId = "app-id",
    TenantId = "tenant-id"
    // No ClientCredentials specified
});
```

### 4. Security
- Keep `Credential` instances secure as they represent authenticated identities
- Do not log or expose credential objects
- Dispose of credentials when no longer needed
- Use appropriate token caching strategies

---

## Configuration Options

### Client Credential Types

The `ClientCredentials` property supports various authentication methods:

```csharp
ClientCredentials = [
    // Certificate from store
    CertificateDescription.FromStoreWithDistinguishedName(
        "CN=MyCert", StoreLocation.LocalMachine, StoreName.My),
    
    // Certificate from Key Vault
    CertificateDescription.FromKeyVault("keyVaultUrl", "certificateName"),
    
    // Client Secret
    ClientSecret.FromConfiguration("AzureAd:ClientSecret"),
    
    // Managed Identity
    // (auto-detected when ClientCredentials is empty)
]
```

See [Client Credentials Documentation](https://aka.ms/mise/client-credentials) for all possibilities.

### Cloud Instances

Different cloud environments use different authentication endpoints:

| Cloud | Instance URL |
|-------|-------------|
| Azure Public | `https://login.microsoftonline.com/` (default) |
| Azure Government | `https://login.microsoftonline.us/` |
| Azure China | `https://login.chinacloudapi.cn/` |
| Azure Germany | `https://login.microsoftonline.de/` |

---

## Common Patterns

### Pattern 1: Simple Application Authentication

```csharp
Credential credential = mise.GetCredential(new()
{
    ClientId = "your-client-id",
    TenantId = "your-tenant-id",
    ClientCredentials = [certificate]
});

string token = await mise.GetToken(credential, new()
{
    Scopes = ["https://graph.microsoft.com/.default"]
});
```

### Pattern 2: Agent Identity with Blueprint

```csharp
// Agent blueprint authenticates
var blueprint = mise.GetCredential(blueprintOptions);

// Exchange for agent identity
var agentFic = await mise.ExchangeCredentialAsync(blueprint, new()
{
    FmiPath = agentIdentityId
});

// Get agent credentials
var agentCredential = mise.GetCredential(agentFic, agentOptions);

// Agent makes API calls
var token = await mise.GetToken(agentCredential, apiScopes);
```

### Pattern 3: Managed Identity (No Credentials)

```csharp
// Works on Azure resources with managed identity enabled
Credential credential = mise.GetCredential(new()
{
    ClientId = "your-client-id",
    TenantId = "your-tenant-id"
    // No ClientCredentials - uses system-assigned managed identity
});

var header = await mise.GetAuthorizationHeaderAsync(credential, apiOptions);
```

---

## Related APIs

### Core Methods

| Method | Purpose |
|--------|---------|
| `Mise.GetCredential()` | Creates or derives credentials |
| `Mise.ExchangeCredentialAsync()` | Exchanges credentials for FIC tokens |
| `Mise.GetToken()` | Acquires access tokens using credentials |
| `Mise.GetAuthorizationHeaderAsync()` | Gets formatted authorization headers |

### Supporting Classes

- `CertificateDescription` - Describes certificate-based credentials
- `DownstreamApiOptions` - Options for calling downstream APIs
- `ClientSecret` - Represents client secret credentials
- `Mise` - Main entry point for credential and token operations

---

## Troubleshooting

### Common Issues

**Issue: Credential creation fails**
- Verify ClientId and TenantId are correct
- Ensure certificates are accessible and valid
- Check that managed identity is enabled (if using MSI)

**Issue: Token exchange fails**
- Verify FmiPath is correct
- Ensure the agent identity relationship is configured in Azure AD
- Check that the blueprint has permission to exchange for the agent identity

**Issue: Cross-cloud exchange fails**
- Verify both cloud instances are correctly specified
- Ensure federated identity credentials are configured for cross-cloud
- Check tenant configuration allows cross-cloud scenarios

---

## See Also

- [Client Credentials Documentation](https://aka.ms/mise/client-credentials)
- [Agent Identity Patterns](../../../src/Microsoft.Identity.Web.AgentIdentities/README.AgentIdentities.md)
- [Federated Identity Credentials (FIC)](https://learn.microsoft.com/azure/active-directory/workload-identities/workload-identity-federation)
- [Cross-Cloud Authentication](https://learn.microsoft.com/azure/azure-government/documentation-government-get-started-connect-with-cli)
- [Microsoft Identity Web Documentation](../../../README.md)

---

## Version Information

- **Target Framework**: .NET 9
- **C# Version**: 13.0
- **Project**: Vision2028
- **Part of**: Microsoft Identity Web

---

*Last Updated: 2024*

*The `Credential` class is part of the Vision2028 project, which provides a simplified authentication experience for Microsoft Identity scenarios.*
