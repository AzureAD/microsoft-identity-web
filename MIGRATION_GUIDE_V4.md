# Microsoft.Identity.Web 4.0.0 Migration Guide (Draft)

Refer to the authoritative online version: https://aka.ms/ms-id-web/v3-to-v4

## Overview

Microsoft.Identity.Web 4.0.0 is a major release that modernizes the library by consolidating authentication patterns, removing legacy APIs, and dropping support for end-of-life .NET versions. This guide will help you migrate from version 3.x to 4.0.0.

## Why a Major Release?

Version 4.0.0 includes several important changes that necessitate a major version bump:

- **Drop net6.0 / net7.0 support**: Both .NET 6 and .NET 7 have reached end-of-support. This release targets .NET 8.0, .NET 9.0, and .NET Framework 4.7.2+.
- **Consolidate onto Microsoft.Identity.Abstractions**: Aligning the entire library with the unified abstractions layer for better consistency and maintainability.
- **Remove legacy downstream web API surface**: Obsolete types and patterns have been removed to simplify the API surface.
- **Unify Azure credential usage on MicrosoftIdentityTokenCredential**: Consolidating Azure SDK authentication to a single, unified credential implementation.
- **Deprecate TokenAcquirer* credentials**: Marking transitional credential types as obsolete in favor of the unified approach.

## Breaking Changes

| Change | Impact | Migration Path |
|--------|--------|----------------|
| Dropped .NET 6.0 / 7.0 support | Projects targeting net6.0 or net7.0 will not compile | Upgrade to .NET 8.0 or .NET 9.0 |
| Removed legacy downstream web API types | Code using obsolete APIs will not compile | Migrate to Microsoft.Identity.Abstractions patterns |
| Removed obsolete token acquisition methods | Code using old methods will not compile | Use ITokenAcquirer or IDownstreamApi patterns |

## Deprecations (Non-Breaking)

The following types are marked as obsolete with warnings but remain functional in v4.0.0:

| Type | Status | Replacement |
|------|--------|-------------|
| `TokenAcquirerTokenCredential` | Obsolete (warning) | `MicrosoftIdentityTokenCredential` |
| `TokenAcquirerAppTokenCredential` | Obsolete (warning) | `MicrosoftIdentityTokenCredential` with `Options.RequestAppToken = true` |

## Azure Credential Modernization

### Why the Change?

Microsoft.Identity.Web introduced multiple credential implementations during its evolution:
- `TokenAcquisitionTokenCredential` (early, already obsolete)
- `TokenAcquirerTokenCredential` (transitional)
- `TokenAcquirerAppTokenCredential` (transitional, app-specific)
- `MicrosoftIdentityTokenCredential` (unified, current)

Version 4.0 consolidates on **MicrosoftIdentityTokenCredential** as the single, recommended credential for all scenarios.

### Migration from TokenAcquirerTokenCredential

**Before (v3.x):**
```csharp
services.AddScoped<TokenAcquirerTokenCredential>();

// In your service/controller
public MyService(TokenAcquirerTokenCredential credential)
{
    var blobClient = new BlobServiceClient(new Uri("..."), credential);
}
```

**After (v4.0):**
```csharp
services.AddMicrosoftIdentityAzureTokenCredential();

// In your service/controller
public MyService(MicrosoftIdentityTokenCredential credential)
{
    var blobClient = new BlobServiceClient(new Uri("..."), credential);
}
```

### Migration from TokenAcquirerAppTokenCredential

**Before (v3.x):**
```csharp
services.AddScoped<TokenAcquirerAppTokenCredential>();

// In your service/controller
public MyService(TokenAcquirerAppTokenCredential credential)
{
    var keyVaultClient = new SecretClient(new Uri("..."), credential);
}
```

**After (v4.0):**
```csharp
services.AddMicrosoftIdentityAzureTokenCredential();

// In your service/controller
public MyService(MicrosoftIdentityTokenCredential credential)
{
    // Configure for app tokens
    credential.Options.RequestAppToken = true;
    
    var keyVaultClient = new SecretClient(new Uri("..."), credential);
}
```

### Unified Credential Benefits

`MicrosoftIdentityTokenCredential` provides a single, flexible credential that supports:

- **User delegated tokens** (default)
- **App tokens** (`Options.RequestAppToken = true`)
- **Agent identity tokens** (`Options.WithAgentIdentity`)
- **Custom token acquisition options** via `Options.AcquireTokenOptions`

## Downstream API Migration

### From IDownstreamWebApi to IDownstreamApi

If you're still using the legacy `IDownstreamWebApi` interface (marked obsolete in previous versions), migrate to `IDownstreamApi`:

**Before:**
```csharp
#pragma warning disable CS0618
var result = await _downstreamWebApi.CallWebApiForUserAsync(
    serviceName: "MyApi",
    options: options);
#pragma warning restore CS0618
```

**After:**
```csharp
var result = await _downstreamApi.CallApiForUserAsync(
    serviceName: "MyApi",
    options: options);
```

## Target Framework Migration

### Update Your Project Files

**Before (v3.x):**
```xml
<TargetFrameworks>net6.0;net7.0;net8.0</TargetFrameworks>
```

**After (v4.0):**
```xml
<TargetFrameworks>net8.0;net9.0</TargetFrameworks>
```

Or for .NET Framework projects:
```xml
<TargetFrameworks>net472;net8.0</TargetFrameworks>
```

## Migration Checklist

- [ ] Upgrade project to .NET 8.0 or .NET 9.0 (or stay on .NET Framework 4.7.2+)
- [ ] Update Microsoft.Identity.Web package references to 4.0.0
- [ ] Replace `TokenAcquirerTokenCredential` with `MicrosoftIdentityTokenCredential`
- [ ] Replace `TokenAcquirerAppTokenCredential` with `MicrosoftIdentityTokenCredential` (set `Options.RequestAppToken = true`)
- [ ] Remove `#pragma warning disable` for obsolete API warnings
- [ ] Replace legacy `IDownstreamWebApi` usage with `IDownstreamApi` (if applicable)
- [ ] Test authentication and token acquisition flows
- [ ] Test Azure SDK integrations with the new credential
- [ ] Update any custom middleware or authentication handlers
- [ ] Review and update documentation

## Frequently Asked Questions

### Q: Will my v3.x application continue to work?
A: Yes, v3.x will continue to receive critical security updates according to the support policy. However, we recommend migrating to v4.0 for the latest features and improvements.

### Q: Can I use both TokenAcquirer* credentials and MicrosoftIdentityTokenCredential in the same application?
A: Yes, during migration you can use both. The old credentials are marked obsolete but remain functional in v4.0. However, we recommend completing the migration as soon as practical.

### Q: What if I can't upgrade from .NET 6.0 / 7.0 yet?
A: Continue using Microsoft.Identity.Web 3.x until you're ready to upgrade. Note that .NET 6 and .NET 7 have reached end-of-support, so upgrading is strongly recommended for security and support reasons.

### Q: Will MicrosoftIdentityTokenCredential work with all Azure SDK clients?
A: Yes, `MicrosoftIdentityTokenCredential` implements `Azure.Core.TokenCredential` and works with any Azure SDK client that accepts a `TokenCredential` parameter.

### Q: How do I configure different authentication schemes with the new credential?
A: Use `credential.Options.AcquireTokenOptions.AuthenticationOptionsName` to specify a particular scheme:
```csharp
credential.Options.AcquireTokenOptions.AuthenticationOptionsName = "MyScheme";
```

### Q: What about the TokenAcquisitionTokenCredential?
A: `TokenAcquisitionTokenCredential` was marked obsolete in earlier versions and has been removed in v4.0. If you're still using it, migrate to `MicrosoftIdentityTokenCredential`.

## Symbol and Type Mapping

### Credentials
- `TokenAcquisitionTokenCredential` → **Removed** → Use `MicrosoftIdentityTokenCredential`
- `TokenAcquirerTokenCredential` → **Obsolete** → Use `MicrosoftIdentityTokenCredential`
- `TokenAcquirerAppTokenCredential` → **Obsolete** → Use `MicrosoftIdentityTokenCredential` with `Options.RequestAppToken = true`

### Downstream APIs
- `IDownstreamWebApi` → **Removed** → Use `IDownstreamApi`
- `DownstreamWebApi` → **Removed** → Use `DownstreamApi`

### Target Frameworks
- `net6.0` → **Removed** → Use `net8.0` or `net9.0`
- `net7.0` → **Removed** → Use `net8.0` or `net9.0`

## Additional Resources

- [Microsoft.Identity.Web Documentation](https://aka.ms/ms-identity-web)
- [Azure SDK for .NET Documentation](https://docs.microsoft.com/azure/developer/azure-sdk/)
- [Migration Guide (Online)](https://aka.ms/ms-id-web/v3-to-v4)
- [Microsoft.Identity.Abstractions](https://www.nuget.org/packages/Microsoft.Identity.Abstractions)
- [.NET Support Policy](https://dotnet.microsoft.com/platform/support/policy/dotnet-core)

## Getting Help

If you encounter issues during migration:

1. Review this guide and the online documentation at https://aka.ms/ms-id-web/v3-to-v4
2. Check existing [GitHub Issues](https://github.com/AzureAD/microsoft-identity-web/issues)
3. Open a new issue with the `migration` label
4. For security concerns, follow our [Security Policy](SECURITY.md)

## Acknowledgments

Thank you for using Microsoft.Identity.Web! We appreciate your patience during this migration and believe these changes will provide a more consistent, maintainable, and powerful authentication experience for your applications.
