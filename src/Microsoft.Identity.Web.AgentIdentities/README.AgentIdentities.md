# Microsoft.Identity.Web.AgentIdentities

## Overview

The Microsoft.Identity.Web.AgentIdentities NuGet package provides support for Agent Identities in Microsoft Entra ID. It enables applications to securely authenticate and acquire tokens for agent applications, agent identities, and agent user identities, which is useful for autonomous agents, interactive agents acting on behalf of their user, and agents having their own user identity.

This package is part of the [Microsoft.Identity.Web](https://github.com/AzureAD/microsoft-identity-web) suite of libraries and was introduced in version 3.10.0.

## Key Concepts

### Agent Applications

An agent application has a special application registration in Microsoft Entra ID that has permissions to act on behalf of Agent identities or Agent User identities, through Federated Identity Credentials (FIC). It's represented by its application ID (Agent Application Client ID). The agent application is configured with credentials (typically FIC+MSI or client certificates) and permissions to acquire tokens for itself to call graph. This is the app the you develop. It's a confidential client application, usually a web API. The only permissions it can have are maintain (create / delete) Agent Identities (using the Microsoft Graph)

### Agent Identity

An agent identity is a special service principal in Microsoft Entra ID. It represents an identity that the agent application created and is authorized to impersonate. It doesn't have credentials on its own. The agent application can acquire tokens on behalf of the agent identity provided the user or tenant admin consented for the agent identity to the corresponding scopes. Autonomous agents acquire app tokens on behalf of the agent identity. Interactive agents called with a user token acquire user tokens on behalf of the agent identity.

### Agent User Identity

An agent user identity is an Agent identity that can also act as a user (think of an agent identity that would have its own mailbox, or would report to you in the directory). An agent application can acquire a token on behalf of an agent user identity.

### Federated Identity Credentials (FIC)

FIC is a trust mechanism in Microsoft Entra ID that enables applications to trust each other using OpenID Connect (OIDC) tokens. In the context of agent identities, FICs are used to establish trust between the agent application and agent identities, and agent identities and agent user identities

## Installation

```bash
dotnet add package Microsoft.Identity.Web.AgentIdentities
```

## Usage

### 1. Configure Services

First, register the required services in your application:

```csharp
// Add the core Identity Web services
services.AddTokenAcquisition();
services.AddInMemoryTokenCaches();
services.AddHttpClient();

// Add Microsoft Graph integration if needed. 
// Requires the Microsoft.Identity.Web.GraphServiceClient package
services.AddMicrosoftGraph();

// Add Agent Identities support
services.AddAgentIdentities();
```

### 2. Configure the Agent Application

Configure your application with the necessary credentials using appsettings.json:

```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "your-tenant-id",
    "ClientId": "agent-application-client-id",
    
    "ClientCredentials": [
      {
        "SourceType": "StoreWithDistinguishedName",
        "CertificateStorePath": "LocalMachine/My",
        "CertificateDistinguishedName": "CN=YourCertificateName"
      }
    
      // Or for Federation Identity Credential with Managed Identity:
      // {
      //   "SourceType": "SignedAssertionFromManagedIdentity",
      //   "ManagedIdentityClientId": "managed-identity-client-id"  // Omit for system-assigned
      // }
    ]
  }
}
```

Or, if you prefer, configure programmatically:

```csharp
// Configure the information about the agent application
services.Configure<MicrosoftIdentityApplicationOptions>(
    options =>
    {
        options.Instance = "https://login.microsoftonline.com/";
        options.TenantId = "your-tenant-id"; 
        options.ClientId = "agent-application-client-id"; 
        options.ClientCredentials = [
            CertificateDescription.FromStoreWithDistinguishedName(
                "CN=YourCertificateName", StoreLocation.LocalMachine, StoreName.My)
        ];
    });
```

See https://aka.ms/ms-id-web/credential-description for all the ways to express credentials.

On ASP.NET Core, use the override of services.Configure taking an authentication shceme.

### 3. Use Agent Identities

#### Agent Identity

##### Autonomous agent

For your autonomous agent application to acquire **app** tokens for an agent identity:

```csharp
// Get the required services from the DI container
IAuthorizationHeaderProvider authorizationHeaderProvider = 
    serviceProvider.GetRequiredService<IAuthorizationHeaderProvider>();

// Configure options for the agent identity
string agentIdentity = "agent-identity-guid";
var options = new AuthorizationHeaderProviderOptions()
    .WithAgentIdentity(agentIdentity);

// Acquire an access token for the agent identity
string authHeader = await authorizationHeaderProvider
    .CreateAuthorizationHeaderForAppAsync("https://resource/.default", options);

// The authHeader contains "Bearer " + the access token (or another protocol
// depending on the options)
```

##### Interactive agent

For your interactive agent application to acquire **user** tokens for an agent identity on behalf of the user calling the web API:

```csharp
// Get the required services from the DI container
IAuthorizationHeaderProvider authorizationHeaderProvider = 
    serviceProvider.GetRequiredService<IAuthorizationHeaderProvider>();

// Configure options for the agent identity
string agentIdentity = "agent-identity-guid";
var options = new AuthorizationHeaderProviderOptions()
    .WithAgentIdentity(agentIdentity);

// Acquire an access token for the agent identity
string authHeader = await authorizationHeaderProvider
    .CreateAuthorizationHeaderForAppAsync(["https://resource/.default"], options);

// The authHeader contains "Bearer " + the access token (or another protocol
// depending on the options)


#### Agent User Identity

For your agent application to acquire tokens on behalf of a agent user identity:

```csharp
// Get the required services
IAuthorizationHeaderProvider authorizationHeaderProvider = 
    serviceProvider.GetRequiredService<IAuthorizationHeaderProvider>();

// Configure options for the agent user identity
string agentIdentity = "agent-identity-client-id";
string userUpn = "user@contoso.com";
var options = new AuthorizationHeaderProviderOptions()
    .WithAgentUserIdentity(agentIdentity, userUpn);

// Create a ClaimsPrincipal to enable token caching
ClaimsPrincipal user = new ClaimsPrincipal();

// Acquire a user token
string authHeader = await authorizationHeaderProvider
    .CreateAuthorizationHeaderForUserAsync(
        scopes: ["https://graph.microsoft.com/.default"],
        options: options,
        user: user);

// The user object now has claims including uid and utid. If you use it
// in another call it will use the cached token.
```

### 4. Microsoft Graph Integration

#### Using Agent Identity with Microsoft Graph:

```csharp
// Get the GraphServiceClient
GraphServiceClient graphServiceClient = serviceProvider.GetRequiredService<GraphServiceClient>();

// Call Microsoft Graph APIs with the agent identity
var applications = await graphServiceClient.Applications
    .GetAsync(r => r.Options.WithAuthenticationOptions(options =>
    {
        options.WithAgentIdentity(agentIdentity);
        options.RequestAppToken = true;
    }));
```

#### Using Agent User Identity with Microsoft Graph:

```csharp
// Get the GraphServiceClient
GraphServiceClient graphServiceClient = serviceProvider.GetRequiredService<GraphServiceClient>();

// Call Microsoft Graph APIs with the agent user identity
var me = await graphServiceClient.Me
    .GetAsync(r => r.Options.WithAuthenticationOptions(options => 
        options.WithAgentUserIdentity(agentIdentity, userUpn)));
```

### 5. Downstream API Integration

To call other APIs using the IDownstreamApi abstraction:

```csharp
// Get the IDownstreamApi service
IDownstreamApi downstreamApi = serviceProvider.GetRequiredService<IDownstreamApi>();

// Call API with agent identity
var response = await downstreamApi.GetForAppAsync<string>(
    "MyApi", 
    options => options.WithAgentIdentity(agentIdentity));

// Call API with agent user identity
var userResponse = await downstreamApi.GetForUserAsync<string>(
    "MyApi", 
    options => options.WithAgentUserIdentity(agentIdentity, userUpn));
```

## Prerequisites

### Microsoft Entra ID Configuration

1. **Agent Application Configuration**:
   - Register an agent application with the graph SDK
   - Add client credentials for the agent application
   - Grant appropriate API permissions, such as Application.ReadWrite.All to create agent identities
   - Example configuration in JSON:
     ```json
     {
       "AzureAd": {
         "Instance": "https://login.microsoftonline.com/",
         "TenantId": "your-tenant-id",
         "ClientId": "agent-application-id",
         "ClientCredentials": [
           {
             "SourceType": "StoreWithDistinguishedName",
             "CertificateStorePath": "LocalMachine/My",
             "CertificateDistinguishedName": "CN=YourCertName"
           }
         ]
       }
     }
     ```

2. **Agent Identity Configuration**:
   - Have the agent create an agent identity
   - Grant appropriate API permissions based on what your agent identity needs to do

3. **User Permission**:
   - For agent user identity scenarios, ensure appropriate user permissions are configured.

## How It Works

Under the hood, the Microsoft.Identity.Web.AgentIdentities package:

1. Uses Federated Identity Credentials (FIC) to establish trust between the agent application and agent identity and between the agent identity and the agent user identity.
2. Acquires FIC tokens using the `GetFicTokenAsync` method
3. Uses the FIC tokens to authenticate as the agent identity
4. For agent user identities, it leverages MSAL extensions to perform user token acquisition

## Troubleshooting

### Common Issues

1. **Missing FIC Configuration**: Ensure Federated Identity Credentials are properly configured in Microsoft Entra ID between the agent application and agent identity.

2. **Permission Issues**: Verify the agent application has sufficient permissions to manage agent identities and that the agent identities have enough permissions to call the downstream APIs.

3. **Certificate Problems**: If you use a client certificate, make sure the certificate is registered in the app registration, and properly installed and accessible by the code of the agent application.

4. **Token Acquisition Failures**: Enable logging to diagnose token acquisition failures:
   ```csharp
   services.AddLogging(builder => {
       builder.AddConsole();
       builder.SetMinimumLevel(LogLevel.Debug);
   });
   ```

## Resources

- [Microsoft Entra ID documentation](https://docs.microsoft.com/en-us/azure/active-directory/)
- [Microsoft Identity Web documentation](https://github.com/AzureAD/microsoft-identity-web/wiki)
- [Workload Identity Federation](https://docs.microsoft.com/en-us/azure/active-directory/develop/workload-identity-federation)
- [Microsoft Graph SDK documentation](https://docs.microsoft.com/en-us/graph/sdks/sdks-overview)
