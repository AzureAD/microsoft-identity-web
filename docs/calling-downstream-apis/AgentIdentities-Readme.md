# Agent identities with Microsoft.Identity.Web

The `Microsoft.Identity.Web.AgentIdentities` package enables .NET applications to acquire tokens for Microsoft Entra agent identities and agent user identities. It supports:

- **Autonomous agents** that acquire app-only tokens as an agent identity.
- **Interactive agents** that acquire delegated tokens as an agent identity on behalf of a signed-in user.
- **Agent user identities** that acquire delegated tokens for a user identity assigned to an agent.

For an introduction to Microsoft Entra Agent ID and provisioning agent identities, see the [Microsoft Entra Agent ID documentation](https://learn.microsoft.com/entra/agent-id/).

## Concepts

### Agent identity blueprint

An agent identity blueprint is the confidential client application that hosts your agent. You configure its client ID, tenant, and credentials in Microsoft.Identity.Web. The blueprint can create and manage agent identities and acquire tokens through them.

### Agent identity

An agent identity is a service principal created from an agent identity blueprint. It has no credential of its own. The blueprint uses a federated identity credential (FIC) trust chain to acquire tokens as the agent identity.

An agent identity can:

- Acquire an app-only token for an autonomous agent.
- Represent an interactive agent while the application acts on behalf of a signed-in user.

### Agent user identity

An agent user identity represents a user identity assigned to an agent. For example, it can have a mailbox or appear as a user in the directory. The blueprint identifies the agent user by either user principal name (UPN) or object ID (OID).

## Install the package

```bash
dotnet add package Microsoft.Identity.Web.AgentIdentities
```

Install integration packages as needed:

```bash
dotnet add package Microsoft.Identity.Web.DownstreamApi
dotnet add package Microsoft.Identity.Web.GraphServiceClient
dotnet add package Microsoft.Identity.Web.Azure
```

## Configure services

Configure the agent identity blueprint as a confidential client:

```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "your-tenant-id",
    "ClientId": "your-agent-blueprint-client-id",
    "SendX5C": true,
    "ClientCredentials": [
      {
        "SourceType": "StoreWithDistinguishedName",
        "CertificateStorePath": "LocalMachine/My",
        "CertificateDistinguishedName": "CN=YourCertificateName"
      }
    ]
  }
}
```

For a certificate-based blueprint, `SendX5C` must be `true`. This enables Subject Name and Issuer (SN+I) authentication by sending the certificate chain, which the FMI request requires.

Register token acquisition, a token cache, and agent identity support:

```csharp
services.Configure<MicrosoftIdentityApplicationOptions>(
    configuration.GetSection("AzureAd"));

services.AddTokenAcquisition(isTokenAcquisitionSingleton: true);
services.AddInMemoryTokenCaches();
services.AddHttpClient();
services.AddAgentIdentities();
```

`AddAgentIdentities()` registers the FIC support used by agent identity flows. If the application calls Microsoft Graph or other downstream APIs, also register the corresponding integration:

```csharp
services.AddMicrosoftGraph();
services.AddDownstreamApis(
    configuration.GetSection("DownstreamApis"));
```

Microsoft.Identity.Web creates and reuses the per-agent confidential clients required by the flow. They inherit the blueprint's selected authentication scheme, authority, Azure region, client capabilities, logging, and HTTP configuration. Do not create or separately configure an MSAL confidential client for each agent identity.

See the [credentials guide](../authentication/credentials/credentials-README.md) for certificate, managed identity, and other credential options.

## How Microsoft.Identity.Web implements the flow

Microsoft.Identity.Web hides the MSAL confidential-client and token-exchange plumbing:

1. The configured **blueprint client** uses its credential to acquire an FMI token (T1) for the requested agent identity.
2. Microsoft.Identity.Web creates or reuses an internal **agent client**, keyed by the agent identity's client ID. That client uses T1 as its assertion.
3. For an autonomous agent, the agent client acquires the downstream app-only token directly.
4. For an agent user identity, the agent client first acquires an instance token (T2), then uses MSAL's User FIC API with T2 and the user's UPN or OID to acquire the delegated token.

The FIC token-exchange audience used for T1 and T2 is resolved from the configured authority host, so applications should not hardcode `api://AzureADTokenExchange/.default`.

MSAL's User FIC API always performs a network request. Microsoft.Identity.Web first attempts `AcquireTokenSilent` using the account identifier it retained from an earlier successful request. It invokes the User FIC API only when no usable cached user token exists or when refresh is forced.

For the equivalent lower-level MSAL pattern, see [How to use FIC and FMI in agentic scenarios](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/wiki/How-to-Use-FIC-and-FMI-in-Agentic-Scenarios).

## Acquire tokens

Choose the API that matches the token type:

| Scenario | Options | Token acquisition API |
|---|---|---|
| Autonomous agent | `WithAgentIdentity(...)` | `CreateAuthorizationHeaderForAppAsync(...)` |
| Interactive agent acting for a signed-in user | `WithAgentIdentity(...)` | `CreateAuthorizationHeaderForUserAsync(...)` |
| Agent user identity | `WithAgentUserIdentity(...)` | `CreateAuthorizationHeaderForUserAsync(...)` |

### Autonomous agent

Use `WithAgentIdentity` with the app-token API:

```csharp
IAuthorizationHeaderProvider authorizationHeaderProvider =
    serviceProvider.GetRequiredService<IAuthorizationHeaderProvider>();

string agentIdentityId = "agent-identity-client-id";
var options = new AuthorizationHeaderProviderOptions()
    .WithAgentIdentity(agentIdentityId);

string authorizationHeader = await authorizationHeaderProvider
    .CreateAuthorizationHeaderForAppAsync(
        "https://resource.example/.default",
        options);
```

The returned value includes the authorization scheme, for example `Bearer <access-token>`.

### Interactive agent

When a protected web API receives a user token and the agent must call a downstream API on behalf of that user, use `WithAgentIdentity` with the user-token API:

```csharp
string agentIdentityId = "agent-identity-client-id";
var options = new AuthorizationHeaderProviderOptions()
    .WithAgentIdentity(agentIdentityId);

string authorizationHeader = await authorizationHeaderProvider
    .CreateAuthorizationHeaderForUserAsync(
        scopes: ["api://downstream-api/access_as_user"],
        authorizationHeaderProviderOptions: options,
        claimsPrincipal: HttpContext.User);
```

This is an on-behalf-of flow. Pass the authenticated caller's `ClaimsPrincipal` as you would for a non-agent web API.

### Agent user identity by UPN

Use `WithAgentUserIdentity` and identify the agent user by UPN:

```csharp
string agentIdentityId = "agent-identity-client-id";
string userUpn = "agent-user@contoso.com";

var options = new AuthorizationHeaderProviderOptions()
    .WithAgentUserIdentity(agentIdentityId, userUpn);

string authorizationHeader = await authorizationHeaderProvider
    .CreateAuthorizationHeaderForUserAsync(
        scopes: ["https://graph.microsoft.com/.default"],
        authorizationHeaderProviderOptions: options);
```

### Agent user identity by object ID

You can instead identify the agent user by OID:

```csharp
string agentIdentityId = "agent-identity-client-id";
Guid userObjectId = Guid.Parse("e1f76997-1b35-4aa8-8a58-a5d8f1ac4636");

var options = new AuthorizationHeaderProviderOptions()
    .WithAgentUserIdentity(agentIdentityId, userObjectId);

string authorizationHeader = await authorizationHeaderProvider
    .CreateAuthorizationHeaderForUserAsync(
        scopes: ["https://graph.microsoft.com/.default"],
        authorizationHeaderProviderOptions: options);
```

If both identifiers are placed in the options manually, the UPN takes precedence. The public overloads set only one identifier.

## Agent user token caching

Agent user identity flows use MSAL's native User FIC API and cache tokens by agent identity, user identifier, and tenant. You do not need to create or reuse a synthetic `ClaimsPrincipal` to enable caching:

```csharp
var options = new AuthorizationHeaderProviderOptions()
    .WithAgentUserIdentity(agentIdentityId, userUpn);

string firstHeader = await authorizationHeaderProvider
    .CreateAuthorizationHeaderForUserAsync(
        ["https://graph.microsoft.com/.default"],
        options);

// The same agent, user, tenant, and scopes can be served from the token cache.
string secondHeader = await authorizationHeaderProvider
    .CreateAuthorizationHeaderForUserAsync(
        ["https://graph.microsoft.com/.default"],
        options);
```

Microsoft.Identity.Web tracks the MSAL account needed for silent acquisition internally. Set `AuthorizationHeaderProviderOptions.AcquireTokenOptions.ForceRefresh` when you intentionally need to bypass the cached access token.

> [!NOTE]
> Agent-user account lookup state and the shared MSAL cache are maintained by the running application. Do not depend on a caller-created `ClaimsPrincipal` to transfer this state between application instances.

## Tenant overrides

For multi-tenant applications, set the tenant on the acquisition options. Microsoft.Identity.Web applies the tenant to each leg of the agent token flow:

```csharp
var options = new AuthorizationHeaderProviderOptions()
    .WithAgentUserIdentity(agentIdentityId, userUpn);

options.AcquireTokenOptions.Tenant = "customer-tenant-id";

string authorizationHeader = await authorizationHeaderProvider
    .CreateAuthorizationHeaderForUserAsync(
        ["https://graph.microsoft.com/.default"],
        options);
```

## Microsoft Graph

### Agent identity

For app-only Graph calls, set both the agent identity and `RequestAppToken`:

```csharp
GraphServiceClient graphClient =
    serviceProvider.GetRequiredService<GraphServiceClient>();

var applications = await graphClient.Applications.GetAsync(request =>
{
    request.Options.WithAuthenticationOptions(options =>
    {
        options.WithAgentIdentity(agentIdentityId);
        options.RequestAppToken = true;
    });
});
```

### Agent user identity

For an agent user, configure either the UPN or OID:

```csharp
var profile = await graphClient.Me.GetAsync(request =>
{
    request.Options.WithAuthenticationOptions(options =>
        options.WithAgentUserIdentity(agentIdentityId, userUpn));
});
```

```csharp
var profile = await graphClient.Me.GetAsync(request =>
{
    request.Options.WithAuthenticationOptions(options =>
        options.WithAgentUserIdentity(agentIdentityId, userObjectId));
});
```

## IDownstreamApi

Configure a downstream API:

```json
{
  "DownstreamApis": {
    "MyApi": {
      "BaseUrl": "https://api.example.com/",
      "Scopes": [ "api://my-api/.default" ]
    }
  }
}
```

Call it as an agent identity or agent user identity:

```csharp
IDownstreamApi downstreamApi =
    serviceProvider.GetRequiredService<IDownstreamApi>();

var appResponse = await downstreamApi.GetForAppAsync<string>(
    "MyApi",
    options => options.WithAgentIdentity(agentIdentityId));

var userResponse = await downstreamApi.GetForUserAsync<string>(
    "MyApi",
    options => options.WithAgentUserIdentity(agentIdentityId, userUpn));
```

## HttpClient

`MicrosoftIdentityMessageHandler` supports per-request agent authentication:

```csharp
services.AddHttpClient("AgentApi", client =>
{
    client.BaseAddress = new Uri("https://api.example.com/");
})
.AddMicrosoftIdentityMessageHandler(options =>
{
    options.Scopes.Add("api://my-api/.default");
});
```

For an autonomous agent:

```csharp
var request = new HttpRequestMessage(HttpMethod.Get, "data")
    .WithAuthenticationOptions(options =>
    {
        options.WithAgentIdentity(agentIdentityId);
        options.RequestAppToken = true;
    });

HttpResponseMessage response = await httpClient.SendAsync(request);
```

For an agent user identity:

```csharp
var request = new HttpRequestMessage(HttpMethod.Get, "user-data")
    .WithAuthenticationOptions(options =>
        options.WithAgentUserIdentity(agentIdentityId, userUpn));

HttpResponseMessage response = await httpClient.SendAsync(request);
```

## Azure SDKs

Register `MicrosoftIdentityTokenCredential`:

```csharp
services.AddMicrosoftIdentityAzureTokenCredential();
```

Apply agent options to the credential before using it with an Azure SDK client:

```csharp
MicrosoftIdentityTokenCredential credential =
    serviceProvider.GetRequiredService<MicrosoftIdentityTokenCredential>();

credential.Options.WithAgentIdentity(agentIdentityId);
credential.Options.RequestAppToken = true;
```

For agent user identities, use `WithAgentUserIdentity` instead. See [Azure SDK integration](./azure-sdks.md) for complete examples.

## Sovereign and private clouds

Microsoft.Identity.Web resolves the FIC token-exchange audience from the configured authority host. Known clouds, such as Azure public, US Government, and China, work automatically.

For a private or otherwise unknown cloud, register its FIC audience from configuration:

```json
{
  "CloudMetadata": {
    "login.my-cloud.example": {
      "federated_credential_audience": "api://AzureADTokenExchangeMyCloud"
    }
  }
}
```

```csharp
services.AddCloudMetadata(
    configuration.GetSection("CloudMetadata"));
```

You can alternatively register an `ICloudMetadataProvider` before Microsoft.Identity.Web services. An explicitly registered provider takes precedence over the configuration-based provider.

If the authority host is non-empty but no FIC audience is known for it, token acquisition fails with an `InvalidOperationException` instead of silently using the public-cloud audience.

## Validate tokens issued to agent identities

Protect the API as you would for any other bearer token. The package also provides helpers for inspecting agent claims:

```csharp
string? parentBlueprintClientId =
    HttpContext.User.GetParentAgentBlueprint();

bool isAgentUserIdentity =
    HttpContext.User.IsAgentUserIdentity();
```

Both methods are available for `ClaimsPrincipal` and `ClaimsIdentity`. `GetParentAgentBlueprint()` returns the `xms_par_app_azp` claim when present. `IsAgentUserIdentity()` validates the `xms_sub_fct` claim and returns `true` when it contains the agent-user facet.

## Troubleshooting

### The wrong token acquisition API is used

- For an autonomous agent, use `CreateAuthorizationHeaderForAppAsync` with `WithAgentIdentity`.
- For an interactive agent acting on behalf of an authenticated caller, use `CreateAuthorizationHeaderForUserAsync` with `WithAgentIdentity` and pass the caller's `ClaimsPrincipal`.
- For an agent user identity, use `CreateAuthorizationHeaderForUserAsync` with `WithAgentUserIdentity`. A synthetic `ClaimsPrincipal` is not required.

### Repeated network token requests

- Reuse the registered Microsoft.Identity.Web services instead of building a new service provider for each request.
- Ensure the agent identity ID, user identifier, tenant, and requested scopes remain consistent.
- Check whether `ForceRefresh` is enabled.
- Enable Microsoft.Identity.Web and MSAL logging to distinguish cache misses from refreshes.

### An unknown-cloud exception is thrown

Verify that `AzureAd:Instance` contains the correct authority. For a cloud not included in MSAL's built-in metadata, call `AddCloudMetadata(...)` or register an `ICloudMetadataProvider`.

### Permissions or FIC errors

- Verify the blueprint credential and tenant configuration.
- Verify the blueprint is authorized to act for the agent identity.
- Verify the agent identity or agent user has the permissions required by the downstream API.
- Verify the requested token type matches the API permissions: application permissions for app-only tokens and delegated permissions for user tokens.
