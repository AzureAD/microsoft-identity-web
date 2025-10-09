# Comparison & Migration

## Keep In-Process When

| Condition | Rationale |
|-----------|-----------|
| Single .NET service only | Minimal complexity |
| Very low token variability | Library simpler |
| Tight latency requirements | Avoid extra hop |

## Use Sidecar When

| Condition | Benefit |
|-----------|---------|
| Polyglot ecosystem | Language-neutral token service |
| Central control of outbound auth | Unified config & logging |
| Agent identity scenarios | Clean per-request identity selection |
| Reduced secret sprawl | One place to manage credentials / managed identity |

## Migration Steps

1. Inventory existing token acquisition code.
2. Add sidecar container; replicate scopes in `DownstreamApis`.
3. Replace MSAL token calls with HTTP to `AuthorizationHeader`.
4. Gradually shift selected APIs to proxy via `DownstreamApi`.
5. Remove legacy caching & auth code once validated.
6. Add monitoring (correlation IDs, metrics).

## Partial Migration

You can mix approaches: keep high-performance internal calls in-process; external / shared APIs via sidecar.
