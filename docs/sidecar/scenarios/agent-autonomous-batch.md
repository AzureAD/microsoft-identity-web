# Scenario: Autonomous Batch Agent

Run scheduled tasks as an autonomous agent (no user).

```
POST /DownstreamApi/InternalApi?AgentIdentity=BatchOrchestrator
```

Add scope temporarily:
```
POST /DownstreamApi/InternalApi?AgentIdentity=BatchOrchestrator&optionsOverride.Scopes=api://internal/Reports.ReadWrite
```

Correlation:
```
...?AgentIdentity=BatchOrchestrator&optionsOverride.AcquireTokenOptions.CorrelationId=<guid>
```
