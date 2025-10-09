# Agent Identities

Agent identities let callers request tokens or downstream API calls under a *named agent blueprint application*, optionally with a single associated user identity.

## Parameters

| Parameter | Required Together? | Description |
|-----------|--------------------|-------------|
| `AgentIdentity` | Base parameter | Identifies the agent blueprint (app context) to use. |
| `AgentUsername` | Requires `AgentIdentity` (mutually exclusive with `AgentUserId`) | UPN of the single user identity associated with this agent for delegated OBO. |
| `AgentUserId` | Requires `AgentIdentity` (mutually exclusive with `AgentUsername`) | Object ID (OID) of the single user identity associated with this agent for delegated OBO. |

> You cannot supply `AgentUsername` or `AgentUserId` without `AgentIdentity`.
> You cannot specify both `AgentUsername` and `AgentUserId` in the same request.

## Selection Semantics

| Case | Resulting Context |
|------|-------------------|
| No `AgentIdentity` parameters | Use inbound caller identity directly. |
| `AgentIdentity` only | Autonomous agent (app/client credentials for that blueprint). |
| `AgentIdentity` + `AgentUsername` | Agent performing delegated operation for that user (UPN). |
| `AgentIdentity` + `AgentUserId` | Agent performing delegated operation for that user (OID, preferred stable form). |

## Examples

Autonomous agent (no user):
```
GET /AuthorizationHeader/Graph?AgentIdentity=BatchOrchestrator
```

Agent acting for a user (UPN):
```
GET /AuthorizationHeader/Graph?AgentIdentity=BatchOrchestrator&AgentUsername=jane.doe@contoso.com
```

Agent acting for a user (OID + long-running OBO session):
```
GET /AuthorizationHeader/Graph?AgentIdentity=BatchOrchestrator&AgentUserId=<user-oid>&optionsOverride.AcquireTokenOptions.LongRunningWebApiSessionKey=workflow-42
```

Downstream proxy with autonomous agent:
```
POST /DownstreamApi/InternalApi?AgentIdentity=Scheduler
```

Downstream proxy with agent + user delegation:
```
POST /DownstreamApi/InternalApi?AgentIdentity=Scheduler&AgentUserId=<user-oid>
```

Force refresh (rare):
```
GET /AuthorizationHeader/Graph?AgentIdentity=BatchOrchestrator&AgentUserId=<user-oid>&optionsOverride.AcquireTokenOptions.ForceRefresh=true
```

## Validation Rules

| Rule | Failure Response |
|------|------------------|
| `AgentUsername` without `AgentIdentity` | 400 (problem details) |
| `AgentUserId` without `AgentIdentity` | 400 |
| Both `AgentUsername` and `AgentUserId` | 400 |
| Invalid / unknown `AgentIdentity` (if enforced) | 400 |

## Claims Challenges

If Conditional Access / CAE requires extra claims:
```
GET /AuthorizationHeader/Graph?AgentIdentity=BatchOrchestrator&AgentUserId=<oid>&optionsOverride.AcquireTokenOptions.Claims=<url-encoded-json>
```

## Guidance

| Scenario | Recommended Form |
|----------|------------------|
| Background automation only | `AgentIdentity` |
| User-specific workflow (stable) | `AgentIdentity` + `AgentUserId` |
| Developer / test readability | `AgentIdentity` + `AgentUsername` |

Prefer OID for stability (user renames do not break flows).

## Security Considerations

- Upstream authorization should restrict which callers may invoke specific `AgentIdentity` values.
- Avoid granting unnecessary delegated permissions to agent blueprint apps if only autonomous tasks are needed.
