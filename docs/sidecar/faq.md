# FAQ (Updated Excerpts)

**Do I need the proxy endpoints?**  
No. You can just fetch headers. Proxy endpoints consolidate outbound HTTP concerns.

**Are agent identities configured separately?**  
No—selection is purely via request query parameters (`AgentIdentity`, `AgentUsername`, `AgentUserId`).

**Can I pass just `AgentUserId` without `AgentIdentity`?**  
No. A user-associated agent identity must specify the owning `AgentIdentity`.

**Can I supply both `AgentUsername` and `AgentUserId`?**  
No. Choose one; OID preferred for stability.

**When use `AgentUserId` vs `AgentUsername`?**  
Prefer `AgentUserId` (stable OID); use UPN for convenience or interactive scenarios.

**How to answer Conditional Access challenges?**  
Repeat with `optionsOverride.AcquireTokenOptions.Claims=<claims-json>`.

**How to minimize secrets?**  
Use managed/workload identity or certificate (Key Vault or store) instead of client secrets.

**Difference between SHR and normal bearer?**  
SHR binds token to a public key; helps mitigate replay if downstream enforces it.

**Can I override tenant per request?**  
Yes via `optionsOverride.AcquireTokenOptions.Tenant`, but ensure registration supports multi-tenant or appropriate issuance.

**What about multi-service sharing of a sidecar?**  
Possible, but balance simplicity vs isolation and blast radius.

Add more questions via issue or PR (label `sidecar-docs`).
