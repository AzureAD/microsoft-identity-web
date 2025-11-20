# Microsoft.Identity.Web.Analyzers

Roslyn analyzer package to help developers migrate from Microsoft.Identity.Web v3.x to v4.0.0.

## Overview

This analyzer package provides diagnostic rules and code fix providers to identify obsolete APIs and breaking changes when migrating to Microsoft.Identity.Web v4.0.0.

## Diagnostic Rules

### IDW4001: TokenAcquirerTokenCredential is obsolete

**Severity**: Warning

**Description**: `TokenAcquirerTokenCredential` has been superseded by `MicrosoftIdentityTokenCredential`. Update your code to use the new credential type.

**Migration**: Replace `TokenAcquirerTokenCredential` with `MicrosoftIdentityTokenCredential` from Azure.Identity.

**Example**:
```csharp
// Before (v3.x)
var credential = new TokenAcquirerTokenCredential(tokenAcquirer);

// After (v4.0)
var credential = new MicrosoftIdentityTokenCredential(tokenAcquisition);
```

### IDW4002: TokenAcquirerAppTokenCredential is obsolete

**Severity**: Warning

**Description**: `TokenAcquirerAppTokenCredential` has been superseded by `MicrosoftIdentityTokenCredential`. Update your code to use the new credential type.

**Migration**: Replace `TokenAcquirerAppTokenCredential` with `MicrosoftIdentityTokenCredential` from Azure.Identity.

**Example**:
```csharp
// Before (v3.x)
var credential = new TokenAcquirerAppTokenCredential(tokenAcquirer);

// After (v4.0)
var credential = new MicrosoftIdentityTokenCredential(tokenAcquisition);
```

### IDW4003: AddDownstreamWebApi is obsolete

**Severity**: Warning

**Description**: `AddDownstreamWebApi` has been replaced by `AddDownstreamApi` from Microsoft.Identity.Abstractions.

**Migration**: Replace `AddDownstreamWebApi` with `AddDownstreamApi`.

**Example**:
```csharp
// Before (v3.x)
services.AddMicrosoftIdentityWebApi(configuration)
    .EnableTokenAcquisitionToCallDownstreamApi()
    .AddDownstreamWebApi("MyApi", configuration.GetSection("MyApi"));

// After (v4.0)
services.AddMicrosoftIdentityWebApi(configuration)
    .EnableTokenAcquisitionToCallDownstreamApi()
    .AddDownstreamApi("MyApi", configuration.GetSection("MyApi"));
```

### IDW4004: IDownstreamWebApi is obsolete

**Severity**: Warning

**Description**: `IDownstreamWebApi` has been replaced by `IDownstreamApi` from Microsoft.Identity.Abstractions.

**Migration**: Replace `IDownstreamWebApi` with `IDownstreamApi`.

**Example**:
```csharp
// Before (v3.x)
private readonly IDownstreamWebApi _downstreamWebApi;

public MyController(IDownstreamWebApi downstreamWebApi)
{
    _downstreamWebApi = downstreamWebApi;
}

// After (v4.0)
private readonly IDownstreamApi _downstreamApi;

public MyController(IDownstreamApi downstreamApi)
{
    _downstreamApi = downstreamApi;
}
```

## Installation

Install the analyzer package as a development dependency:

```bash
dotnet add package Microsoft.Identity.Web.Analyzers
```

Or add it to your project file:

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.Identity.Web.Analyzers" Version="3.14.1" PrivateAssets="all" />
</ItemGroup>
```

## Additional Resources

- [Migration Guide](https://aka.ms/id-web-v4-migration)
- [Downstream API Migration](https://aka.ms/id-web-downstream-api-v2)
- [Microsoft.Identity.Web Documentation](https://aka.ms/ms-id-web)

## Feedback and Contributions

Please report issues or provide feedback on the [GitHub repository](https://github.com/AzureAD/microsoft-identity-web/issues).

## License

This project is licensed under the MIT License - see the [LICENSE](../../LICENSE) file for details.
