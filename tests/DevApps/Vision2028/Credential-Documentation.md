# Credential Class Documentation

## Overview

The `Credential` class represents an authenticated identity in the Microsoft Identity ecosystem. It serves as a key abstraction for managing authentication credentials across various scenarios, including agent identities, federated identity credentials (FIC), cross-cloud authentication, cross-tenant authentication, etc ...

## Purpose

`Credential` encapsulates the authentication state and identity information needed to acquire tokens and make authenticated API calls. It acts as a portable, reusable representation of an authenticated entity that can be:

- Created from application registrations with client credentials
- Exchanged for federated identity credentials
- Used to obtain access tokens for downstream APIs
- Passed between different authentication contexts

---

## Examples

### Cross cloud FIC

```csharp
// Describe the credentials for the Fairfax app
Credential appInFairFaxCredentials = mise.NewCredential(new()
{
    Instance = "https://login.microsoftonline.us/",
    ClientId = "FairFax AppID",
    TenantId = "TenantId in public FairFax",

    // Not using client credentials. They are "guessed" from the platform.
    // For instance managed certificate, or System Assigned Identity, or Fmi.
});


// Token exchange to get FIC token for FairFax App (token exchange URL determined automatically based on clouds instance)
Credential FicForAppInFairFax = await mise.ExchangeCredentialAsync(agentIdentityCredentials);

// Derived-credential for public cloud app
Credential CredentialInPublicCloud = mise.NewCredential(appInFairFaxCredentials, new()
{
    Instance = "https://login.microsoftonline.us/",
    ClientId = "Public cloud AppId",
    TenantId = "TenantId in public cloud"
});

// Get a token or an authorization header
var authorizationHeaderGccM = await mise.GetAuthorizationHeaderAsync(CredentialInPublicCloud, new()
{
    Scopes = ["https://graph.microsoft.com/.default"]
});
```

### Implement an agent identity calling an API

The recommended ways to call a downstream API are documented in [Agent identities](https://github.com/AzureAD/microsoft-identity-web/blob/master/src/Microsoft.Identity.Web.AgentIdentities/README.AgentIdentities.md). You should just use `.WithAgentIdentity(agentIdentity)`. 

If you wanted to decompose yourself what needs to be done, the code would look like:

```csharp

string agentBluePrintAppId = "c4b2d4d9-9257-4c1a-a5c0-0a4907c83411";
string agentIdentity = "44250d7d-2362-4fba-9ba0-49c19ae270e0";
string tenantId = "31a58c3b-ae9c-4448-9e8f-e9e143e800df";

Mise mise = new();

// Agent blueprint credentials with SN/I Cert
Credential agentBlueprintCredentials = mise.NewCredential(new()
{
    ClientId = agentBluePrintAppId,
    TenantId = tenantId,
    ManagedIdentityClientId = "UAMI=GUID"
});

// Token exchange to get FIC token for agent blueprint with Fmi path for agent identity (azp = AB)
Credential agentBlueprintFic = await mise.ExchangeCredentialAsync(agentBlueprintCredentials, new()
{
    FmiPath = agentIdentity
});

// Get the Agent identity FIC credentials
Credential agentIdentityCredentials = mise.NewCredential(agentBlueprintFic, new()
{
    ClientId = agentIdentity,
    TenantId = tenantId
});

// Token for Agent identity to call Graph
string tokenForAgentIdentityToCallGraph = await mise.GetToken(agentIdentityCredentials, new()
{
    Scopes = ["https://graph.microsoft.com/.default"]
});

// Or get authorization header for Agent identity to call Graph
var authorizationHeader = await mise.GetAuthorizationHeaderAsync(agentIdentityCredentials, new DownstreamApiOptions()
{
    Scopes = ["https://graph.microsoft.com/.default"]
});
```


## Key Usage Patterns

### 1. Creating a Credential with Client Credentials

Create a `Credential` for an application (for instance here using an SN/I certificate). For more options, see [client credentials](https://aka.ms/mise/client-credentials)

```csharp
Credential agentBlueprintCredentials = mise.NewCredential(new()
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
- When you have client credentials (certificate, managed certificate, or FIC+managed identity or Fmi)
- As the starting point for credential chains

---

### 2. Exchanging Credentials for Federated Identity

Exchange an existing credential for a federated identity credential (FIC). Here we specify an FMI path for the FIC.

```csharp
Credential agentBlueprintFic = await mise.ExchangeCredentialAsync(
    agentBlueprintCredentials, 
    new()
    {
        FmiPath = "44250d7d-2362-4fba-9ba0-49c19ae270e0"
    });
```

The token exchange URL is determined automatically based on the cloud instance in the credentials.

---

### 3. Deriving a New Credential from an Existing One

Create a new (chained) credential based on a federated identity credential:

```csharp
Credential agentIdentityCredentials = mise.NewCredential(
    agentBlueprintFic, 
    new()
    {
        ClientId = "44250d7d-2362-4fba-9ba0-49c19ae270e0",
        TenantId = "31a58c3b-ae9c-4448-9e8f-e9e143e800df"
    });
```

**When to use:**
- After exchanging for a FIC token
- To create a credential with specific clientID, cloud instance, tenant, Azure regions, ... properties
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

Or get an authorization header (header + binding certificate for mTLS Pop)

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

## Configuration Options

### Cloud Instances

Different cloud environments use different authentication endpoints:

| Cloud | Instance URL |
|-------|-------------|
| Azure Public | `https://login.microsoftonline.com/` (default) |
| Azure Government | `https://login.microsoftonline.us/` |
| Azure China | `https://login.chinacloudapi.cn/` |
| Azure Germany | `https://login.microsoftonline.de/` |

---

## Related APIs

### Core Methods

| Method | Purpose |
|--------|---------|
| `Mise.NewCredential()` | Creates or derives credentials |
| `Mise.ExchangeCredentialAsync()` | Exchanges credentials for FIC tokens |
| `Mise.GetToken()` | Acquires access tokens using credentials |
| `Mise.GetAuthorizationHeaderAsync()` | Gets formatted authorization headers |

### Supporting Classes (for the prototype)


- `CertificateDescription` - Describes certificate-based credentials
- `DownstreamApiOptions` - Options for calling downstream APIs
- `Mise` - Main entry point for credential and token operations

---

## See Also

- [Client Credentials Documentation](https://aka.ms/mise/client-credentials)
- [Agent Identity Patterns](../../../src/Microsoft.Identity.Web.AgentIdentities/README.AgentIdentities.md)
- [Federated Identity Credentials (FIC)](https://learn.microsoft.com/azure/active-directory/workload-identities/workload-identity-federation)
- [Cross-Cloud Authentication](https://learn.microsoft.com/azure/azure-government/documentation-government-get-started-connect-with-cli)
- [Microsoft Identity Web Documentation](../../../README.md)
