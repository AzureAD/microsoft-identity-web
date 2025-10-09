# Scenario: Proxy a Downstream API Call

## Basic (Inbound Caller Identity)

```
POST /DownstreamApi/Graph
Authorization: Bearer <user-token>
```

## Autonomous Agent

```
POST /DownstreamApi/Graph?AgentIdentity=Scheduler
```

## Agent + User Delegation (OID)

```
POST /DownstreamApi/Graph?AgentIdentity=Scheduler&AgentUserId=<user-oid>&optionsOverride.HttpMethod=GET&optionsOverride.RelativePath=/v1.0/users/<user-oid>/messages
```

## Additional Scope

```
POST /DownstreamApi/Graph?optionsOverride.Scopes=Mail.Read
```

## Invalid (Will 400): Missing AgentIdentity

```
POST /DownstreamApi/Graph?AgentUserId=<user-oid>
```
