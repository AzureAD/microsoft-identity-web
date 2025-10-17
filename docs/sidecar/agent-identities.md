# Agent Identities

Agent identities enable sophisticated authentication scenarios where an agent application acts autonomously or on behalf of users for interactive agents. This document explains the semantics and usage patterns for agent identities in the sidecar. It focuses on the SDK aspects. More documentation exists for agents identity in general in <a href="https://learn.microsoft.com/entra/agentic-identity-platform">Agentic identity platform documentation</a>

## Overview

Agent identities support two primary patterns:

1. **Autonomous Agent**: The agent application operates in its own application context
2. **Delegated Agent**: An interactive agent that operates on behalf the user that triggered it.

The sidecar accepts three optional query parameters that control agent identity behavior:
- `AgentIdentity` - GUID of the agent identity
- `AgentUsername` - The user principal name (UPN) for agent user identities.
- `AgentUserId` - The user object ID (OID) for agent user identities as an alternative to UPN

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

**`AgentIdentity` + (`AgentUsername` OR `AgentUserId`) = user agent (context of the agent user identity)**

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

### Pattern 2: Autonomous user agent with username

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

### Pattern 3: Autonomous user Agent with user ID

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

### Pattern 4: Interactive agent (acting on behalf of the user calling it)

An agent web API that receives a user token, validates it, and calls downstream APIs on behalf of that user.

**Scenario**: A web API acting as an interactive agent that validates incoming user tokens and makes delegated calls to downstream services.

**Flow**:
1. The agent web API receives a user token from the calling application
2. It validates the token by calling the sidecar's `/Validate` endpoint
3. It acquires tokens for downstream APIs by calling `/AuthorizationHeader` with only the `AgentIdentity` and the incoming Authorization header

```bash
# Step 1: Validate incoming user token
GET /Validate
Authorization: Bearer <user-token>

# Step 2: Get authorization header for downstream API on behalf of the user
GET /AuthorizationHeader/Graph?AgentIdentity=<agent-client-id>
Authorization: Bearer <user-token>
```

**Token Characteristics**:
- Token type: User token (OBO flow)
- Subject (`sub`): Original user's object ID
- Agent acts as intermediary for the user
- Permissions: Delegated permissions scoped to the user

**Use Cases**:
- Web APIs that act as agents
- Interactive agent services
- Agent-based middleware that delegates to downstream APIs
- Services that validate and forward user context

### Pattern 5: Regular Request (No Agent)

When no agent parameters are provided, the sidecar uses the incoming token's identity.

**Scenario**: Standard on-behalf-of (OBO) flow without agent identities.

```bash
GET /AuthorizationHeader/Graph
Authorization: Bearer <user-token>
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
   - Set up client credentials (FIC+MSI or certificate or secret)

2. **Agent Identity Configuration**:
   - Create agent identities using the agent blueprint
   - Assign necessary permissions to agent identities

3. **Application Permissions**:
   - Grant application permissions for autonomous scenarios
   - Grant delegated permissions for user delegation scenarios
   - Ensure admin consent is provided where required

For detailed step-by-step instructions on configuring agent identities in Microsoft Entra ID, see the <a href="https://learn.microsoft.com/entra/agentic-identity-platform">Agentic identity platform documentation</a>.

## Token Claims

For detailed information about token claims for agent identities, including autonomous and delegated agent patterns, see the <a href="https://learn.microsoft.com/entra/agentic-identity-platform">Agentic identity platform documentation</a>.

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

The sidecar is called by a web API that receives an authorization header. All these examples show this authorization header being transmitted to the sidecar.

### TypeScript/JavaScript

```typescript
// Autonomous agent
const autonomousResponse = await fetch(
  `http://localhost:5000/AuthorizationHeader/Graph?AgentIdentity=${agentClientId}`,
  {
    headers: {
      'Authorization': incomingAuthorizationHeader
    }
  }
);

// Delegated agent with username
const delegatedResponse = await fetch(
  `http://localhost:5000/AuthorizationHeader/Graph?` +
  `AgentIdentity=${agentClientId}&AgentUsername=${encodeURIComponent(userPrincipalName)}`,
  {
    headers: {
      'Authorization': incomingAuthorizationHeader
    }
  }
);

// Delegated agent with user ID
const delegatedByIdResponse = await fetch(
  `http://localhost:5000/AuthorizationHeader/Graph?` +
  `AgentIdentity=${agentClientId}&AgentUserId=${userObjectId}`,
  {
    headers: {
      'Authorization': incomingAuthorizationHeader
    }
  }
);
```

### Python

```python
import requests

# Autonomous agent
response = requests.get(
    "http://localhost:5000/AuthorizationHeader/Graph",
    params={"AgentIdentity": agent_client_id},
    headers={"Authorization": incoming_authorization_header}
)

# Delegated agent with username
response = requests.get(
    "http://localhost:5000/AuthorizationHeader/Graph",
    params={
        "AgentIdentity": agent_client_id,
        "AgentUsername": user_principal_name
    },
    headers={"Authorization": incoming_authorization_header}
)

# Delegated agent with user ID
response = requests.get(
    "http://localhost:5000/AuthorizationHeader/Graph",
    params={
        "AgentIdentity": agent_client_id,
        "AgentUserId": user_object_id
    },
    headers={"Authorization": incoming_authorization_header}
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
