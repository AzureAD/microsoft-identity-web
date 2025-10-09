# Scenario: Long-Running OBO (Agent + User)

Use a stable session key for continuity.

```
GET /AuthorizationHeader/Graph?AgentIdentity=BatchOrchestrator&AgentUserId=<user-oid>&optionsOverride.AcquireTokenOptions.LongRunningWebApiSessionKey=workflow-42
```

Force refresh only if required:
```
GET /AuthorizationHeader/Graph?AgentIdentity=BatchOrchestrator&AgentUserId=<user-oid>&optionsOverride.AcquireTokenOptions.LongRunningWebApiSessionKey=workflow-42&optionsOverride.AcquireTokenOptions.ForceRefresh=true
```

Key rules:
- Must include `AgentIdentity` when specifying user.
- Use OID rather than UPN for stability in long-running contexts.
