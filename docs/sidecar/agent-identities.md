# Agent Identities

Agent identities enable sophisticated authentication scenarios where an agent application acts autonomously or on behalf of specific users. This document explains the semantics and usage patterns for agent identities in the sidecar.

## Overview

Agent identities support two primary patterns:

1. **Autonomous Agent**: The agent application operates in its own application context
2. **Delegated Agent**: The agent application operates on behalf of a specific user identity

The sidecar accepts three optional query parameters that control agent identity behavior:
- `AgentIdentity` - The client/application ID of the agent identity
- `AgentUsername` - The user principal name (UPN) for delegated scenarios
- `AgentUserId` - The user object ID (OID) for delegated scenarios

## Semantic Rules

### Rule 1: AgentIdentity Requirement

**`AgentUsername` or `AgentUserId` MUST be paired with `AgentIdentity`**

If you specify `AgentUsername` or `AgentUserId` without `AgentIdentity`, the request will fail with a validation error.

```bash
# ❌ INVALID - AgentUsername without AgentIdentity
GET /AuthorizationHeader/Graph?AgentUsername=user@contoso.com

# ✅ VALID - AgentUsername with AgentIdentity
GET /AuthorizationHeader/Graph?AgentIdentity=agent-client-id&AgentUsername=user@contoso.com
```

### Rule 2: Mutual Exclusivity

**`AgentUsername` and `AgentUserId` are mutually exclusive**

You cannot specify both `AgentUsername` and `AgentUserId` in the same request. If both are provided, the request will fail with a validation error.

```bash
# ❌ INVALID - Both AgentUsername and AgentUserId specified
GET /AuthorizationHeader/Graph?AgentIdentity=agent-id&AgentUsername=user@contoso.com&AgentUserId=user-oid

# ✅ VALID - Only AgentUsername
GET /AuthorizationHeader/Graph?AgentIdentity=agent-id&AgentUsername=user@contoso.com

# ✅ VALID - Only AgentUserId
GET /AuthorizationHeader/Graph?AgentIdentity=agent-id&AgentUserId=user-object-id
```

### Rule 3: Autonomous vs Delegated

**`AgentIdentity` alone = autonomous agent (application context)**

When only `AgentIdentity` is provided without `AgentUsername` or `AgentUserId`, the sidecar acquires an application token for the agent identity.

**`AgentIdentity` + (`AgentUsername` OR `AgentUserId`) = delegated agent (user context)**

When `AgentIdentity` is combined with either `AgentUsername` or `AgentUserId`, the sidecar acquires a user token for that specific user in the context of the agent identity.

## Usage Patterns

### Pattern 1: Autonomous Agent

The agent application operates independently in its own application context, acquiring application tokens.

**Scenario**: A batch processing service that processes files autonomously.

```bash
GET /AuthorizationHeader/Graph?AgentIdentity=12345678-1234-1234-1234-123456789012
```

**Token Characteristics**:
- Token type: Application token
- Subject (`sub`): Agent application's object ID
- Token issued for the agent's identity
- Permissions: Application permissions assigned to the agent identity

**Use Cases**:
- Automated batch processing
- Background tasks
- System-to-system operations
- Scheduled jobs without user context

### Pattern 2: Delegated Agent with Username

The agent operates on behalf of a specific user identified by their UPN.

**Scenario**: An AI assistant acting on behalf of a user in a chat application.

```bash
GET /AuthorizationHeader/Graph?AgentIdentity=12345678-1234-1234-1234-123456789012&AgentUsername=alice@contoso.com
```

**Token Characteristics**:
- Token type: User token
- Subject (`sub`): User's object ID
- Agent identity facet included in token claims
- Permissions: Delegated permissions scoped to the user

**Use Cases**:
- Interactive agent applications
- AI assistants with user delegation
- User-scoped automation
- Personalized workflows

### Pattern 3: Delegated Agent with User ID

The agent operates on behalf of a specific user identified by their object ID (OID).

**Scenario**: A workflow engine processing user-specific tasks using stored user IDs.

```bash
GET /AuthorizationHeader/Graph?AgentIdentity=12345678-1234-1234-1234-123456789012&AgentUserId=87654321-4321-4321-4321-210987654321
```

**Token Characteristics**:
- Token type: User token
- Subject (`sub`): User's object ID
- Agent identity facet included in token claims
- Permissions: Delegated permissions scoped to the user

**Use Cases**:
- Long-running workflows with stored user identifiers
- Batch operations on behalf of multiple users
- Systems using object IDs for user reference

### Pattern 4: Regular Request (No Agent)

When no agent parameters are provided, the sidecar uses the incoming token's identity.

**Scenario**: Standard on-behalf-of (OBO) flow without agent identities.

```bash
GET /AuthorizationHeader/Graph
```

**Token Characteristics**:
- Token type: Depends on incoming token and configuration
- Uses standard OBO or client credentials flow
- No agent identity facets

## Precedence Rules

When both `AgentUsername` and `AgentUserId` are somehow present (though validation should prevent this), the implementation gives precedence to username:

```
AgentUsername > AgentUserId
```

However, this scenario should be avoided and will result in a validation error in strict validation mode.

## Microsoft Entra ID Configuration

### Prerequisites for Agent Identities

1. **Agent Application Registration**:
   - Register the parent agent application in Microsoft Entra ID
   - Configure API permissions for downstream APIs
   - Set up client credentials (certificate or secret)

2. **Agent Identity Configuration**:
   - Create agent identity registrations
   - Configure Federated Identity Credentials (FIC) between the parent agent and agent identities
   - Assign necessary permissions to agent identities

3. **Application Permissions**:
   - Grant application permissions for autonomous scenarios
   - Grant delegated permissions for user delegation scenarios
   - Ensure admin consent is provided where required

### Example: Configuring an Agent Identity

```bash
# Create agent identity app registration
az ad app create --display-name "MyAgent Identity"

# Get the agent identity client ID
AGENT_IDENTITY_ID=$(az ad app list --display-name "MyAgent Identity" --query [0].appId -o tsv)

# Configure FIC between parent agent and agent identity
az ad app federated-credential create \
  --id $PARENT_AGENT_APP_ID \
  --parameters '{
    "name": "MyAgentIdentityFIC",
    "issuer": "https://login.microsoftonline.com/<tenant-id>/v2.0",
    "subject": "'$AGENT_IDENTITY_ID'",
    "audiences": ["api://AzureADTokenExchange"]
  }'

# Grant permissions to the agent identity
az ad app permission add \
  --id $AGENT_IDENTITY_ID \
  --api 00000003-0000-0000-c000-000000000000 \
  --api-permissions e1fe6dd8-ba31-4d61-89e7-88639da4683d=Scope

# Grant admin consent
az ad app permission admin-consent --id $AGENT_IDENTITY_ID
```

## Token Claims

### Autonomous Agent Token Claims

When using autonomous agent pattern (`AgentIdentity` only):

```json
{
  "aud": "https://graph.microsoft.com",
  "iss": "https://sts.windows.net/<tenant-id>/",
  "appid": "12345678-1234-1234-1234-123456789012",
  "sub": "12345678-1234-1234-1234-123456789012",
  "roles": ["User.Read.All", "Mail.Read.All"],
  "oid": "<agent-object-id>",
  "tid": "<tenant-id>"
}
```

### Delegated Agent Token Claims

When using delegated agent pattern (`AgentIdentity` + `AgentUsername`/`AgentUserId`):

```json
{
  "aud": "https://graph.microsoft.com",
  "iss": "https://sts.windows.net/<tenant-id>/",
  "appid": "12345678-1234-1234-1234-123456789012",
  "sub": "87654321-4321-4321-4321-210987654321",
  "scp": "User.Read Mail.Read",
  "oid": "87654321-4321-4321-4321-210987654321",
  "upn": "alice@contoso.com",
  "xms_sub_fct": "1 2 13 15",
  "xms_par_app_azp": "<parent-agent-client-id>",
  "tid": "<tenant-id>"
}
```

**Key Claims**:
- `xms_sub_fct`: Subject facets (13 = agent identity user facet)
- `xms_par_app_azp`: Parent agent blueprint (client ID of the parent agent)

### Validating Agent Identity Claims

Use the Microsoft.Identity.Web.AgentIdentities package to validate agent identity claims:

```csharp
using Microsoft.Identity.Web;

// Check if token represents an agent user identity
bool isAgentUser = claimsPrincipal.IsAgentUserIdentity();

// Get the parent agent blueprint
string? parentAgent = claimsPrincipal.GetParentAgentBlueprint();
```

## Error Scenarios

### Missing AgentIdentity with AgentUsername

**Request**:
```bash
GET /AuthorizationHeader/Graph?AgentUsername=user@contoso.com
```

**Response**:
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Bad Request",
  "status": 400,
  "detail": "AgentUsername requires AgentIdentity to be specified"
}
```

### Both AgentUsername and AgentUserId Specified

**Request**:
```bash
GET /AuthorizationHeader/Graph?AgentIdentity=agent-id&AgentUsername=user@contoso.com&AgentUserId=user-oid
```

**Response**:
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Bad Request",
  "status": 400,
  "detail": "AgentUsername and AgentUserId are mutually exclusive"
}
```

### Invalid AgentUserId Format

**Request**:
```bash
GET /AuthorizationHeader/Graph?AgentIdentity=agent-id&AgentUserId=invalid-guid
```

**Response**:
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Bad Request",
  "status": 400,
  "detail": "AgentUserId must be a valid GUID"
}
```

## Code Examples

### TypeScript/JavaScript

```typescript
// Autonomous agent
const autonomousResponse = await fetch(
  `http://localhost:5000/AuthorizationHeader/Graph?AgentIdentity=${agentClientId}`
);

// Delegated agent with username
const delegatedResponse = await fetch(
  `http://localhost:5000/AuthorizationHeader/Graph?` +
  `AgentIdentity=${agentClientId}&AgentUsername=${encodeURIComponent(userPrincipalName)}`
);

// Delegated agent with user ID
const delegatedByIdResponse = await fetch(
  `http://localhost:5000/AuthorizationHeader/Graph?` +
  `AgentIdentity=${agentClientId}&AgentUserId=${userObjectId}`
);
```

### Python

```python
import requests

# Autonomous agent
response = requests.get(
    "http://localhost:5000/AuthorizationHeader/Graph",
    params={"AgentIdentity": agent_client_id}
)

# Delegated agent with username
response = requests.get(
    "http://localhost:5000/AuthorizationHeader/Graph",
    params={
        "AgentIdentity": agent_client_id,
        "AgentUsername": user_principal_name
    }
)

# Delegated agent with user ID
response = requests.get(
    "http://localhost:5000/AuthorizationHeader/Graph",
    params={
        "AgentIdentity": agent_client_id,
        "AgentUserId": user_object_id
    }
)
```

### C# with HttpClient

```csharp
using System.Net.Http;

var httpClient = new HttpClient();

// Autonomous agent
var autonomousUrl = $"http://localhost:5000/AuthorizationHeader/Graph" +
    $"?AgentIdentity={agentClientId}";
var response = await httpClient.GetAsync(autonomousUrl);

// Delegated agent with username
var delegatedUrl = $"http://localhost:5000/AuthorizationHeader/Graph" +
    $"?AgentIdentity={agentClientId}" +
    $"&AgentUsername={Uri.EscapeDataString(userPrincipalName)}";
response = await httpClient.GetAsync(delegatedUrl);

// Delegated agent with user ID
var delegatedByIdUrl = $"http://localhost:5000/AuthorizationHeader/Graph" +
    $"?AgentIdentity={agentClientId}" +
    $"&AgentUserId={userObjectId}";
response = await httpClient.GetAsync(delegatedByIdUrl);
```

## Best Practices

1. **Validate Input**: Always validate agent identity parameters before making requests
2. **Use Object IDs When Available**: Object IDs are more stable than UPNs for long-running processes
3. **Implement Proper Error Handling**: Handle agent identity validation errors gracefully
4. **Secure Agent Credentials**: Protect agent identity client IDs and credentials
5. **Audit Agent Operations**: Log and monitor agent identity usage for security and compliance
6. **Test Both Patterns**: Validate both autonomous and delegated scenarios in your tests
7. **Document Intent**: Clearly document which agent pattern is appropriate for each use case

## Related Documentation

- [Configuration Reference](configuration.md) - Configure agent identity credentials
- [Endpoints Reference](endpoints.md) - Complete endpoint documentation
- [Scenarios: Agent Autonomous Batch](scenarios/agent-autonomous-batch.md) - Autonomous agent example
- [FAQ](faq.md) - Common questions about agent identities
- [Troubleshooting](troubleshooting.md) - Resolve agent identity issues
