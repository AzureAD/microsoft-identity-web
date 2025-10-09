# Scenario: Signed HTTP Request (SHR)

Autonomous agent SHR:
```
GET /AuthorizationHeader/Graph?AgentIdentity=BatchOrchestrator&optionsOverride.AcquireTokenOptions.PopPublicKey=<url-encoded-jwk>
```

Agent + user SHR:
```
GET /AuthorizationHeader/Graph?AgentIdentity=BatchOrchestrator&AgentUserId=<user-oid>&optionsOverride.AcquireTokenOptions.PopPublicKey=<url-encoded-jwk>
```
