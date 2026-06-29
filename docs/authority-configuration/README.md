# Authority Configuration & Precedence in Microsoft.Identity.Web

## Overview

Microsoft.Identity.Web provides flexible options for configuring authentication authority URLs. Understanding how these configuration options interact is crucial for proper application setup, especially when working with Azure Active Directory (AAD), Azure AD B2C, and Customer Identity and Access Management (CIAM).

This guide explains:
- How authority-related configuration properties work together
- The precedence rules when multiple properties are set
- Best practices for different authentication scenarios
- How to interpret and resolve configuration errors

## Terminology

### Core Configuration Properties

- **Authority**: A complete URL to the authentication endpoint, including the instance and tenant identifier. Examples:
  - `https://login.microsoftonline.com/common`
  - `https://login.microsoftonline.com/contoso.onmicrosoft.com`
  - `https://contoso.b2clogin.com/contoso.onmicrosoft.com/B2C_1_susi`

- **Instance**: The base URL of the authentication service without tenant information. Examples:
  - `https://login.microsoftonline.com/` (Azure Commercial)
  - `https://login.microsoftonline.us/` (Azure Government)
  - `https://login.chinacloudapi.cn/` (Azure China)
  - `https://contoso.b2clogin.com/` (B2C)
  
  See [Azure AD authentication endpoints](https://learn.microsoft.com/azure/active-directory/develop/authentication-national-cloud) for sovereign cloud instances.

- **TenantId**: The tenant identifier, which can be:
  - A GUID (e.g., `12345678-1234-1234-1234-123456789012`)
  - A tenant domain (e.g., `contoso.onmicrosoft.com`)
  - Special values (`common`, `organizations`, `consumers`)

- **Domain**: The primary domain of your tenant (e.g., `contoso.onmicrosoft.com`). Used primarily with B2C configurations.

- **Policy IDs**: B2C-specific identifiers for user flows:
  - `SignUpSignInPolicyId` (e.g., `B2C_1_susi`)
  - `ResetPasswordPolicyId` (e.g., `B2C_1_reset`)
  - `EditProfilePolicyId` (e.g., `B2C_1_edit_profile`)

## Authority Resolution Decision Tree

The following flowchart shows how Microsoft.Identity.Web resolves the authority and selects the MSAL builder method:

```mermaid
flowchart TD
    A[Configuration Provided] --> B{Both Authority AND<br/>Instance/TenantId<br/>explicitly set?}
    B -- Yes --> THROW[InvalidOperationException<br/>at startup]
    B -- No --> C{Instance and/or<br/>TenantId set?}
    C -- Yes --> D{Is B2C?}
    D -- Yes --> B2CMSAL["MSAL: WithB2CAuthority()"]
    D -- No --> AADMSAL["MSAL: WithAuthority()<br/>AAD security + resilience"]
    C -- No --> G{Authority set?}
    G -- No --> INVALID[Missing configuration error]
    G -- Yes --> CIAM{"Host is *.ciamlogin.com<br/>with no path?"}
    CIAM -- Yes --> REWRITE["Rewrite: append tenant domain<br/>Parse into Instance + TenantId"]
    REWRITE --> AADMSAL
    CIAM -- No --> PRESERVE["PreserveAuthority = true<br/>Authority URL kept as-is"]
    PRESERVE --> OIDCMSAL["MSAL: WithOidcAuthority()<br/>Generic OIDC path"]

    style THROW fill:#f66,color:#fff
    style INVALID fill:#f66,color:#fff
    style AADMSAL fill:#4a4,color:#fff
    style B2CMSAL fill:#48a,color:#fff
    style OIDCMSAL fill:#a84,color:#fff
```

**Key implications**:
- `Instance` + `TenantId` (AAD) routes through `WithAuthority()` -- full AAD-specific security, instance discovery, and resilience.
- `Instance` + `TenantId` (B2C with policies) routes through `WithB2CAuthority()` -- B2C-optimized path.
- `Authority` alone for **any** scenario (AAD, B2C, CIAM with path) routes through `WithOidcAuthority()` -- generic OIDC, no AAD-specific protections.
- The only exception is `*.ciamlogin.com` with **no** path segment (e.g., `https://contoso.ciamlogin.com`), which gets rewritten and parsed into `Instance` + `TenantId` automatically.

## Configuration Rules

You must choose **one** approach -- `Authority` or `Instance`/`TenantId`. Setting both throws an `InvalidOperationException` at startup:

| Authority Set | Instance Set | TenantId Set | Result |
|---------------|--------------|--------------|--------|
| ✅ | ❌ | ❌ | Authority is parsed into Instance + TenantId |
| ❌ | ✅ | ✅ | Instance + TenantId used directly |
| ❌ | ✅ | ❌ | Instance used, tenant resolved at runtime* |
| ✅ | ✅ | ❌ | **Throws `InvalidOperationException`** |
| ✅ | ❌ | ✅ | **Throws `InvalidOperationException`** |
| ✅ | ✅ | ✅ | **Throws `InvalidOperationException`** |
| ❌ | ❌ | ✅ | Invalid configuration |

\* For single-tenant apps, always specify TenantId when using Instance.

## Recommended Configuration Patterns

### AAD Single-Tenant Application

**Recommended**: Use `Instance` and `TenantId` separately for clarity and flexibility.

```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "12345678-1234-1234-1234-123456789012",
    "ClientId": "11111111-1111-1111-1111-111111111111"
  }
}
```

**Alternative**: Use `Authority` (will be parsed automatically).

> **Note**: For AAD authorities, `Instance` + `TenantId` is strongly preferred. Using `Authority` alone routes through MSAL's generic OIDC path (`WithOidcAuthority`), which skips AAD-specific security and resilience features such as instance discovery and authority validation.

```json
{
  "AzureAd": {
    "Authority": "https://login.microsoftonline.com/12345678-1234-1234-1234-123456789012",
    "ClientId": "11111111-1111-1111-1111-111111111111"
  }
}
```

### AAD Multi-Tenant Application

**Option 1**: Use `Instance` with special tenant value.

```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "organizations",
    "ClientId": "11111111-1111-1111-1111-111111111111"
  }
}
```

**Option 2**: Use complete `Authority`.

> **Note**: For AAD authorities, `Instance` + `TenantId` (Option 1) is strongly preferred. Using `Authority` alone routes through MSAL's generic OIDC path, which skips AAD-specific security and resilience features.

```json
{
  "AzureAd": {
    "Authority": "https://login.microsoftonline.com/organizations",
    "ClientId": "11111111-1111-1111-1111-111111111111"
  }
}
```

### Azure AD B2C

**Recommended**: Use `Authority` including the policy path. Do NOT mix with `Instance`/`TenantId`.

```json
{
  "AzureAdB2C": {
    "Authority": "https://contoso.b2clogin.com/contoso.onmicrosoft.com/B2C_1_susi",
    "ClientId": "11111111-1111-1111-1111-111111111111",
    "Domain": "contoso.onmicrosoft.com"
  }
}
```

**Note**: The legacy `/tfp/` path segment is automatically normalized by Microsoft.Identity.Web:
- `https://contoso.b2clogin.com/tfp/contoso.onmicrosoft.com/B2C_1_susi` 
- Becomes: `https://contoso.b2clogin.com/contoso.onmicrosoft.com/B2C_1_susi`

See [B2C Authority Examples](b2c-authority-examples.md) for more details.

### CIAM (Customer Identity and Access Management)

**Recommended**: Use `Authority` for CIAM configurations. The library automatically handles CIAM authorities correctly.

```json
{
  "AzureAd": {
    "Authority": "https://contoso.ciamlogin.com/contoso.onmicrosoft.com",
    "ClientId": "11111111-1111-1111-1111-111111111111"
  }
}
```

See [CIAM Authority Examples](ciam-authority-examples.md) for more details.

## Conflicting Configuration: Authority + Instance/TenantId

If both `Authority` and (`Instance` and/or `TenantId`) are explicitly configured, Microsoft.Identity.Web throws an `InvalidOperationException` at startup:

```
System.InvalidOperationException: [MsIdWeb] Both 'Authority' ('https://login.microsoftonline.com/common')
and 'Instance'/'TenantId' ('https://login.microsoftonline.com/', 'contoso.onmicrosoft.com') are configured.
These settings conflict. Remove either 'Authority' or 'Instance'/'TenantId' from the configuration.
```

**How to fix**:
1. **Option 1 (Recommended)**: Remove `Authority` from your configuration, keep `Instance` and `TenantId`.
2. **Option 2**: Remove both `Instance` and `TenantId`, keep only `Authority`.

## Edge Cases and Special Scenarios

### Scheme-less Authority

If you provide an authority without the `https://` scheme, you may encounter parsing errors. Always include the full URL:

❌ Wrong: `"Authority": "login.microsoftonline.com/common"`  
✅ Correct: `"Authority": "https://login.microsoftonline.com/common"`

### Trailing Slashes

Trailing slashes are automatically normalized. Both forms work identically:
- `https://login.microsoftonline.com/`
- `https://login.microsoftonline.com`

### Query Parameters in Authority

Query parameters in the Authority URL are preserved during parsing but generally not recommended. Use `ExtraQueryParameters` configuration option instead.

### Missing v2.0 Endpoint Suffix

Microsoft.Identity.Web and MSAL.NET use the v2.0 endpoint by default. You do NOT need to append `/v2.0` to your authority:

❌ Avoid: `https://login.microsoftonline.com/common/v2.0`  
✅ Correct: `https://login.microsoftonline.com/common`

### Custom Domains with CIAM

When using custom domains with CIAM, use the full Authority URL. The library handles custom CIAM domains automatically:

```json
{
  "AzureAd": {
    "Authority": "https://login.contoso.com/contoso.onmicrosoft.com",
    "ClientId": "11111111-1111-1111-1111-111111111111"
  }
}
```

**Note**: Ensure your custom domain is properly configured in your CIAM tenant before using it.

## Migration Guidance

If you're upgrading from older configurations or mixing authority properties, see the [Migration Guide](migration-authority-vs-instance.md) for detailed upgrade paths.

## Frequently Asked Questions

For answers to common configuration questions and troubleshooting tips, see the [Authority Precedence FAQ](faq-authority-precedence.md).

## Additional Resources

- [Azure AD B2C Authority Examples](b2c-authority-examples.md)
- [CIAM Authority Examples](ciam-authority-examples.md)
- [Migration Guide: Authority vs Instance/TenantId](migration-authority-vs-instance.md)
- [Microsoft identity platform documentation](https://learn.microsoft.com/azure/active-directory/develop/)
- [Azure AD B2C documentation](https://learn.microsoft.com/azure/active-directory-b2c/)
