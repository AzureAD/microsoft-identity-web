# Microsoft.Identity.Web 4.0.0 Migration Guide (Draft)

Authoritative online version: https://aka.ms/ms-id-web/v3-to-v4

Status: DRAFT (pending final API diff)  
Audience: Application developers, library integrators, enterprise maintainers

---

## Overview

Microsoft.Identity.Web 4.0.0 is a major release that modernizes the library by consolidating authentication patterns, removing legacy APIs, and dropping support for end-of-life .NET versions. This guide helps you migrate from 3.x to 4.0.0 efficiently and safely.

---

## 1. Why a Major Release?

Version 4.0.0 includes several important changes that necessitate a major version bump:

- Drop net6.0 / net7.0 support (both are EOL). Targets now .NET 8.0, .NET 9.0, and .NET Framework 4.6.2+.
- Consolidate onto Microsoft.Identity.Abstractions for consistency and extensibility.
- Remove legacy downstream web API surface (`IDownstreamWebApi`, `DownstreamWebApi`, their extension methods).
- Unify Azure credential usage around `MicrosoftIdentityTokenCredential`.
- Deprecate transitional `TokenAcquirer*` credentials (now obsolete).
- Promote async-first patterns (remove sync shims like `WithClientCredentials`).
- Simplify token cache initialization (`InitializeAsync` removed).
- Internal hardening: remove obsolete protected members (e.g., `_certificatesObserver`).
- Provide a single, discoverable migration link: https://aka.ms/ms-id-web/v3-to-v4

---

## 2. Breaking Changes (Removed)

| Category | Removed Symbol(s) | Impact | Migration Path |
|----------|-------------------|--------|----------------|
| Framework Targets | net6.0, net7.0 | Older TFMs fail to build | Upgrade to net8.0/net9.0 or stay on net462+ dual-target |
| Downstream API Surface | `IDownstreamWebApi`, `DownstreamWebApi`, `AddDownstreamWebApi(...)` | Compile errors | Use `IDownstreamApi`, `DownstreamApi`, `AddDownstreamApi(...)` |
| Generic Helper Extensions | `PostForUserAsync<T>`, `PutForUserAsync<T>`, etc. | Compile errors | Use new strongly typed `IDownstreamApi` methods |
| Legacy Credentials | `TokenAcquisitionTokenCredential`, `TokenAcquisitionAppTokenCredential` | Compile errors | Use `MicrosoftIdentityTokenCredential` |
| Sync Credential Builder | `WithClientCredentials(...)` (sync) | Compile errors | Use `await WithClientCredentialsAsync(...)` |
| Token Cache Init | `IMsalTokenCacheProvider.InitializeAsync(...)` | Compile errors | Use `Initialize(...)` |
| Wrapper Interface | `IAuthenticationSchemeInformationProvider` (Web wrapper) | Compile errors | Use `Microsoft.Identity.Abstractions.IAuthenticationSchemeInformationProvider` |
| Protected Field | `_certificatesObserver` | Inheritance break | Use `_certificatesObservers` enumeration |
| Miscellaneous Obsoletes | (Per final API diff) | Compile/runtime changes | Follow symbol mapping |

---

## 3. Deprecations (Non‑Breaking in v4)

These types are now `[Obsolete]` (warnings) but still functional:

| Type | Status | Replacement | Notes |
|------|--------|-------------|-------|
| `TokenAcquirerTokenCredential` | Obsolete | `MicrosoftIdentityTokenCredential` | Non-error obsolete |
| `TokenAcquirerAppTokenCredential` | Obsolete | `MicrosoftIdentityTokenCredential` + `Options.RequestAppToken = true` | Consolidate |

Rationale: Provide one flexible credential that toggles user/app/agent scenarios via `Options`.

---

## 4. Azure Credential Modernization

### Credential Evolution

| Historical | Role | v4 Status | Action |
|------------|------|----------|--------|
| `TokenAcquisitionTokenCredential` | Early wrapper | Removed | Replace |
| `TokenAcquisitionAppTokenCredential` | Early app wrapper | Removed | Replace |
| `TokenAcquirerTokenCredential` | Transitional user credential | Obsolete | Replace |
| `TokenAcquirerAppTokenCredential` | Transitional app credential | Obsolete | Replace |
| `MicrosoftIdentityTokenCredential` | Unified | Recommended | Adopt |

### Migration from TokenAcquirerTokenCredential

**Before (v3.x)**:
```csharp
services.AddScoped<TokenAcquirerTokenCredential>();

public class MyService
{
    public MyService(TokenAcquirerTokenCredential credential)
    {
        var blobClient = new BlobServiceClient(new Uri("..."), credential);
    }
}
```

**After (v4.0)**:
```csharp
services.AddMicrosoftIdentityAzureTokenCredential();

public class MyService
{
    public MyService(MicrosoftIdentityTokenCredential credential)
    {
        var blobClient = new BlobServiceClient(new Uri("..."), credential);
    }
}
```

### Migration from TokenAcquirerAppTokenCredential

**Before (v3.x)**:
```csharp
services.AddScoped<TokenAcquirerAppTokenCredential>();

public class MyService
{
    public MyService(TokenAcquirerAppTokenCredential credential)
    {
        var secretClient = new SecretClient(new Uri("..."), credential);
    }
}
```

**After (v4.0)**:
```csharp
services.AddMicrosoftIdentityAzureTokenCredential();

public class MyService
{
    public MyService(MicrosoftIdentityTokenCredential credential)
    {
        credential.Options.RequestAppToken = true;
        var secretClient = new SecretClient(new Uri("..."), credential);
    }
}
```

### Unified Credential Benefits

`MicrosoftIdentityTokenCredential` supports:
- User delegated tokens (default)
- App tokens (`Options.RequestAppToken = true`)
- Agent identity tokens (`Options.WithAgentIdentity(...)`)
- Agent user identity tokens (`Options.WithAgentUserIdentity(...)`)
- Custom acquisition logic (correlation id, tenant override, scheme override)

---

## 5. Downstream API Migration

### From `IDownstreamWebApi` to `IDownstreamApi`

**Before** (legacy):
```csharp
#pragma warning disable CS0618
var response = await _downstreamWebApi.CallWebApiForUserAsync(
    "MyApi",
    options: opts => { /* ... */ });
#pragma warning restore CS0618
```

**After**:
```csharp
var response = await _downstreamApi.CallForUserAsync(
    "MyApi",
    options => { /* ... */ });
```

### Scopes Configuration Change

Old (`DownstreamWebApiOptions`):
```json
"DownstreamWebApi": {
  "Scopes": "User.Read offline_access email"
}
```

New (`DownstreamApiOptions`):
```json
"DownstreamApis": {
  "MyApi": {
    "Scopes": [ "User.Read", "offline_access", "email" ],
    "BaseUrl": "https://graph.microsoft.com/v1.0"
  }
}
```

---

## 6. Target Framework Migration

**Before (v3.x)**:
```xml
<TargetFrameworks>net6.0;net7.0;net8.0</TargetFrameworks>
```

**After (v4.0)**:
```xml
<TargetFrameworks>net8.0;net9.0</TargetFrameworks>
```

Or dual with .NET Framework:
```xml
<TargetFrameworks>net472;net8.0</TargetFrameworks>
```

---

## 7. Codebase Audit (Search Patterns & Tooling)

Use these patterns and scripts to detect legacy usage prior to (or immediately after) upgrading.

### 7.1 Quick ripgrep Patterns

```bash
rg "TokenAcquisitionTokenCredential"
rg "TokenAcquisitionAppTokenCredential"
rg "TokenAcquirerTokenCredential"
rg "TokenAcquirerAppTokenCredential"
rg "IDownstreamWebApi"
rg "DownstreamWebApi[^A-Za-z]"
rg "AddDownstreamWebApi"
rg "PostForUserAsync<"
rg "PutForUserAsync<"
rg "WithClientCredentials("
rg "InitializeAsync\\(ITokenCache"
rg "_certificatesObserver"
```

### 7.2 PowerShell Variant
```powershell
$patterns = @(
  "TokenAcquisitionTokenCredential",
  "TokenAcquisitionAppTokenCredential",
  "TokenAcquirerTokenCredential",
  "TokenAcquirerAppTokenCredential",
  "IDownstreamWebApi",
  "DownstreamWebApi",
  "AddDownstreamWebApi",
  "PostForUserAsync<",
  "PutForUserAsync<",
  "WithClientCredentials(",
  "InitializeAsync(ITokenCache",
  "_certificatesObserver"
)

$errors = 0
foreach ($p in $patterns) {
  $matches = Select-String -Path . -Pattern $p -ErrorAction SilentlyContinue `
    -Exclude *.dll,*.exe,*.png,*.jpg,*.gif,*.lock
  if ($matches) {
    Write-Host "`n=== Pattern: $p ===" -ForegroundColor Cyan
    $matches | ForEach-Object { "$($_.Path):$($_.LineNumber): $($_.Line.Trim())" }
    $errors++
  }
}

if ($errors -gt 0) { exit 1 } else { Write-Host "No legacy symbols found." -ForegroundColor Green }
```

### 7.3 CI Bash Script
```bash
#!/usr/bin/env bash
set -euo pipefail
patterns=(
  "TokenAcquisitionTokenCredential"
  "TokenAcquisitionAppTokenCredential"
  "TokenAcquirerTokenCredential"
  "TokenAcquirerAppTokenCredential"
  "IDownstreamWebApi"
  "DownstreamWebApi"
  "AddDownstreamWebApi"
  "PostForUserAsync<"
  "PutForUserAsync<"
  "WithClientCredentials("
  "InitializeAsync(ITokenCache"
  "_certificatesObserver"
)
fail=0
for p in "${patterns[@]}"; do
  if rg --hidden --glob '!.git' "$p" > /dev/null; then
    echo "FOUND: $p"
    rg --hidden --glob '!.git' "$p"
    fail=1
  fi
done
if [ $fail -ne 0 ]; then
  echo "❌ Legacy or obsolete symbols found. See https://aka.ms/ms-id-web/v3-to-v4"
  exit 1
else
  echo "✅ No legacy symbol references detected."
fi
```

### 7.4 Handling Matches

| Pattern | Action |
|---------|--------|
| `TokenAcquisition*Credential` | Replace with `MicrosoftIdentityTokenCredential` |
| `TokenAcquirer*Credential` | Replace; set `Options.RequestAppToken = true` if app token flow |
| `IDownstreamWebApi` / `DownstreamWebApi` / `AddDownstreamWebApi` | Migrate to `IDownstreamApi` / `AddDownstreamApi` |
| `PostForUserAsync<`, `PutForUserAsync<` | Use new typed `IDownstreamApi` methods |
| `WithClientCredentials(` | Use `WithClientCredentialsAsync` |
| `InitializeAsync(ITokenCache` | Replace with `Initialize` |
| `_certificatesObserver` | Use `_certificatesObservers` |

### 7.5 False Positives
- Ignore references inside this migration guide or docs.
- Filter generated code.
- Verify custom wrappers before mass-replace.

### 7.6 Future Analyzer Integration
An analyzer (planned rules IDW4001–IDW4009) will provide automated diagnostics + code fixes; prefer it over manual scanning once released. (aspirational: https://github.com/AzureAD/microsoft-identity-web/issues/3539)

---

## 8. Migration Checklist

| Step | Action | Status |
|------|--------|--------|
| 1 | Upgrade to Microsoft.Identity.Web 4.0.0 |  |
| 2 | Upgrade target frameworks (remove net6/net7) |  |
| 3 | Replace `AddDownstreamWebApi` → `AddDownstreamApi` |  |
| 4 | Replace `IDownstreamWebApi` usages |  |
| 5 | Remove legacy `TokenAcquisition*` credential references |  |
| 6 | Replace `TokenAcquirer*` with `MicrosoftIdentityTokenCredential` |  |
| 7 | Convert scopes from space-separated string → string[] |  |
| 8 | Replace sync `WithClientCredentials` |  |
| 9 | Replace `InitializeAsync` with `Initialize` |  |
|10 | Update auth scheme interface usage |  |
|11 | Remove obsolete generic downstream methods |  |
|12 | Adjust inheritance for `_certificatesObservers` |  |
|13 | Validate user/app/agent Azure SDK scenarios |  |
|14 | Re-run tests (integration + perf) |  |
|15 | Update internal docs & diagrams |  |
|16 | Adopt analyzer (when available) |  |

---

## 9. Release Notes Excerpt Template

```
### Breaking Changes
- Removed legacy downstream web API symbols: IDownstreamWebApi, DownstreamWebApi, AddDownstreamWebApi.
- Removed TokenAcquisitionTokenCredential / TokenAcquisitionAppTokenCredential.
- Removed sync WithClientCredentials extension.
- Removed IMsalTokenCacheProvider.InitializeAsync.
- Removed wrapper IAuthenticationSchemeInformationProvider.
- Removed protected _certificatesObserver field.

### Deprecations
- Obsoleted TokenAcquirerTokenCredential (use MicrosoftIdentityTokenCredential).
- Obsoleted TokenAcquirerAppTokenCredential (use MicrosoftIdentityTokenCredential + Options.RequestAppToken = true).
```

---

## 10. FAQ

| Question | Answer |
|----------|--------|
| Will 3.x still receive updates? | Only critical/security fixes per support policy. |
| Do I need separate credentials per token type? | No—toggle `Options.RequestAppToken` or agent identity helpers. |
| Is behavior of token acquisition changed? | No intentional semantic changes; report regressions. |
| Can I temporarily keep obsolete types? | Yes in v4; plan removal in a future major version. |
| How do I switch auth scheme? | Set `credential.Options.AcquireTokenOptions.AuthenticationOptionsName`. |

---

## 11. Open Validation Items

| Item | Status |
|------|--------|
| Final API diff vs 3.x | Pending |
| All obsolete attributes have aka.ms link | Pending |
| README & README-Azure updated | In progress |
| Analyzer issue created | Done |
| Analyzer package implementation | Pending |

---

## 12. Symbol Mapping

| Old / Removed / Obsolete | Replacement | Notes |
|--------------------------|-------------|-------|
| TokenAcquisitionTokenCredential | MicrosoftIdentityTokenCredential | Removed |
| TokenAcquisitionAppTokenCredential | MicrosoftIdentityTokenCredential | Removed |
| TokenAcquirerTokenCredential (obsolete) | MicrosoftIdentityTokenCredential | Non-error obsolete |
| TokenAcquirerAppTokenCredential (obsolete) | MicrosoftIdentityTokenCredential + `Options.RequestAppToken` | Non-error obsolete |
| IDownstreamWebApi / DownstreamWebApi | IDownstreamApi / DownstreamApi | Consolidation |
| AddDownstreamWebApi | AddDownstreamApi | New API |
| PostForUserAsync / PutForUserAsync (legacy) | Typed `IDownstreamApi` methods | Parity |
| WithClientCredentials (sync) | WithClientCredentialsAsync | Async-first |
| InitializeAsync | Initialize | Removal |
| Wrapper auth scheme provider | Abstractions interface | Direct usage |
| _certificatesObserver | _certificatesObservers | Internal cleanup |

---

## 13. Future Analyzer Rules (Preview)

| Diagnostic ID | Trigger | Suggested Fix |
|---------------|---------|---------------|
| IDW4001 | TokenAcquirerTokenCredential usage | Replace with MicrosoftIdentityTokenCredential |
| IDW4002 | TokenAcquirerAppTokenCredential usage | Replace & set `Options.RequestAppToken` |
| IDW4003 | AddDownstreamWebApi usage | Replace with AddDownstreamApi |
| IDW4004 | IDownstreamWebApi usage | Replace with IDownstreamApi |
| IDW4005 | Legacy generic downstream helper | Use new typed methods |
| IDW4006 | WithClientCredentials (sync) | Use async variant |
| IDW4007 | InitializeAsync usage | Use Initialize |
| IDW4008 | Scopes string pattern | Convert to string[] |
| IDW4009 | _certificatesObserver usage | Iterate _certificatesObservers |

---

## 14. Additional Resources

- Microsoft.Identity.Web Documentation: https://aka.ms/ms-identity-web  
- Migration Guide Online: https://aka.ms/ms-id-web/v3-to-v4  
- Microsoft.Identity.Abstractions NuGet: https://www.nuget.org/packages/Microsoft.Identity.Abstractions  
- .NET Support Policy: https://dotnet.microsoft.com/platform/support/policy/dotnet-core

---

## 15. Getting Help

1. Review this guide & https://aka.ms/ms-id-web/v3-to-v4  
2. Search existing issues: https://github.com/AzureAD/microsoft-identity-web/issues  
3. Open a new issue (label: `migration`) with repro details  
4. For security concerns, follow SECURITY.md  

---

## 16. Acknowledgments

Thank you for using Microsoft.Identity.Web. Your feedback guides these improvements—please file migration experience issues so we can refine documentation and tooling.
