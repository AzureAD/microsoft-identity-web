# Troubleshooting (Updated Agent Rules)

| Symptom | HTTP | Likely Cause | Action |
|---------|------|-------------|--------|
| 401 (AuthorizationHeader) | 401 | Missing or invalid inbound token | Supply valid user/agent token |
| 400 “apiName not found” | 400 | No config entry | Add `DownstreamApis` entry |
| 400 invalid override | 400 | Malformed `optionsOverride.*` | Correct/remove parameter |
| Downstream 403 inside wrapper | 200 | Missing permission / consent | Adjust scopes / admin consent |
| Slow responses after forcing refresh | 200 | Overuse `ForceRefresh` | Remove unless necessary |
| Managed identity error | 400 | Binding absent | Fix pod/workload identity setup |
| Claims challenge loop | 401 | Not echoing claims | Add `Claims` JSON override |
| SHR rejected | 200 wrapper / 401 downstream | Downstream not SHR-enabled or key mismatch | Align key & validation |
| 400 with user param only | 400 | `AgentUserId` / `AgentUsername` without `AgentIdentity` | Add `AgentIdentity` or remove user param |
| 400 both user params | 400 | Provided both UPN + OID | Keep only one |
| 400 unknown agent identity | 400 | Nonexistent agent blueprint label | Use valid label or omit |

## Debug Steps

1. Add `CorrelationId`.
2. Use `/Validate` (with safe token) in dev.
3. Temporarily enable `ShowPII` in isolated environment only.

## Cache Guidance

Normalize scope sets; avoid proliferating many unique scope combinations unnecessarily.
