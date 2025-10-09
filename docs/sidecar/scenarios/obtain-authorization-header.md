# Scenario: Obtain an Authorization Header

## Standard (Inbound Caller Identity)

```
GET /AuthorizationHeader/Graph
Authorization: Bearer <inbound-user-token>
```

## Autonomous Agent

```
GET /AuthorizationHeader/Graph?AgentIdentity=BatchOrchestrator
```

## Agent Acting For a User (UPN)

```
GET /AuthorizationHeader/Graph?AgentIdentity=BatchOrchestrator&AgentUsername=jane.doe@contoso.com
```

## Agent Acting For a User (OID + Long-Running OBO)

```
GET /AuthorizationHeader/Graph?AgentIdentity=BatchOrchestrator&AgentUserId=<user-oid>&optionsOverride.AcquireTokenOptions.LongRunningWebApiSessionKey=session-42
```

## Additional Scope

```
GET /AuthorizationHeader/Graph?optionsOverride.Scopes=Mail.Read
```

## Force App Token

```
GET /AuthorizationHeader/Graph?optionsOverride.RequestAppToken=true
```

(Still uses inbound token for auth; acquisition uses client credentials.)
