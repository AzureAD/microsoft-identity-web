# Wave 1 API Diff Process Documentation

## Overview
This document describes the process for generating an API diff between Microsoft.Identity.Web v3.14.1 (last 3.x release) and v4.0.0.

## API Diff Generation Approach

### Tools
The recommended tool for generating .NET API diffs is the `Microsoft.DotNet.ApiCompat` tool from the .NET SDK or the `AsmDiff` tool.

### Process Steps

#### 1. Prepare v3.14.1 Assemblies
```bash
# Clone the repository at v3.14.1 tag
git clone https://github.com/AzureAD/microsoft-identity-web.git v3.14.1-baseline
cd v3.14.1-baseline
git checkout 3.14.1  # or the appropriate tag

# Build the v3.14.1 assemblies
dotnet build Microsoft.Identity.Web.sln -c Release

# Copy assemblies to a baseline directory
mkdir -p /tmp/api-diff/baseline
cp -r src/*/bin/Release/net8.0/*.dll /tmp/api-diff/baseline/
```

#### 2. Prepare v4.0.0 Assemblies
```bash
# From the v4 branch
dotnet build Microsoft.Identity.Web.sln -c Release

# Copy assemblies to a comparison directory
mkdir -p /tmp/api-diff/v4
cp -r src/*/bin/Release/net8.0/*.dll /tmp/api-diff/v4/
```

#### 3. Generate API Diff
Using Microsoft.DotNet.ApiCompat:
```bash
dotnet tool install -g Microsoft.DotNet.ApiCompat.Tool

# Generate diff for each assembly
for dll in /tmp/api-diff/baseline/*.dll; do
    name=$(basename "$dll")
    echo "Diffing $name..."
    dotnet api-compat \
        --baseline "/tmp/api-diff/baseline/$name" \
        --current "/tmp/api-diff/v4/$name" \
        --output "/tmp/api-diff/reports/$name.diff.txt"
done
```

### Expected Breaking Changes (Based on Migration Guide)

#### Removed APIs (Breaking)
1. **TokenAcquisitionTokenCredential** - Removed, replaced by MicrosoftIdentityTokenCredential
2. **TokenAcquisitionAppTokenCredential** - Removed, replaced by MicrosoftIdentityTokenCredential
3. **IDownstreamWebApi** - Removed (marked obsolete with error=false in v4)
4. **DownstreamWebApi** - Removed (marked obsolete with error=false in v4)
5. **AddDownstreamWebApi** extension methods - Removed (marked obsolete with error=false in v4)
6. **Generic helper methods** like `PostForUserAsync<T>`, `PutForUserAsync<T>` - Removed (marked obsolete)
7. **WithClientCredentials (sync)** - Removed, replaced by async version
8. **IMsalTokenCacheProvider.InitializeAsync** - Removed, replaced by Initialize
9. **Protected field _certificatesObserver** - Marked obsolete, replaced by _certificatesObservers

#### Deprecated APIs (Non-Breaking - Warning Only)
1. **TokenAcquirerTokenCredential** - Obsolete (error=false)
2. **TokenAcquirerAppTokenCredential** - Obsolete (error=false)

#### Framework Target Changes (Breaking)
- Removed support for net6.0 and net7.0
- Maintained support for net8.0, net9.0, net462, net472, netstandard2.0

## Verification Completed

### ✅ Obsolete Attribute Verification
All key deprecated APIs have proper `[Obsolete]` attributes with:
- Migration guidance messages
- Reference to migration guide URL (https://aka.ms/ms-id-web/v3-to-v4)
- Appropriate error flag (true for removed APIs, false for deprecated but still available)

### ✅ PublicAPI Files Updated
- Ran `tools/mark-shipped.ps1` successfully
- Moved all unshipped APIs to shipped files
- Updated 76 PublicAPI files across all target frameworks
- Key additions:
  - `_certificatesObservers` field (internal API)
  - `CerticateObserverAction.SuccessfullyUsed` enum value
  - Various internal constants and utilities

### ✅ Build Validation
- Core libraries build successfully:
  - Microsoft.Identity.Web
  - Microsoft.Identity.Web.TokenAcquisition
  - Microsoft.Identity.Web.Azure
  - Microsoft.Identity.Web.TokenCache
  - Microsoft.Identity.Web.Certificate

## Recommendations

1. **Generate Full API Diff**: Use the process above to generate a comprehensive API diff report
2. **Attach to Release**: Include the API diff artifact with the v4.0.0 release notes
3. **Update CHANGELOG.md**: Document all breaking changes with clear migration paths
4. **Sample Code Updates**: Ensure all samples and templates use the new v4 APIs

## Status Summary

| Task | Status | Notes |
|------|--------|-------|
| Obsolete attribute sweep | ✅ Complete | All APIs properly marked with migration guidance |
| PublicAPI.Shipped.txt update | ✅ Complete | 76 files updated, all unshipped APIs moved |
| _certificatesObservers validation | ✅ Complete | Properly implemented with obsolete attribute on singular form |
| API diff generation | ⏳ Pending | Process documented, needs execution with v3.14.1 baseline |
| Build validation | ✅ Complete | Core libraries build successfully |

## Next Steps for Wave 2

Wave 2 will focus on:
- Analyzer project scaffolding
- Implementation of diagnostic rules (IDW4001-IDW4007)
- Code fixers and Roslyn tests
- CI integration

---
*Generated as part of Wave 1 v4 Migration Finalization*
