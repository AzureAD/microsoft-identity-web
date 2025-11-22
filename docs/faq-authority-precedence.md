# Authority Precedence FAQ

This FAQ addresses common questions about authority configuration, precedence rules, and troubleshooting configuration warnings in Microsoft.Identity.Web.

## General Questions

### Q: What is the difference between Authority, Instance, and TenantId?

**A**: These are different ways to specify your authentication endpoint:

- **Authority**: A complete URL including both the authentication service endpoint and tenant identifier
  - Example: `https://login.microsoftonline.com/contoso.onmicrosoft.com`
  
- **Instance**: The base URL of the authentication service (without tenant information)
  - Example: `https://login.microsoftonline.com/`
  
- **TenantId**: The tenant identifier, which can be a GUID, domain name, or special value (`common`, `organizations`, `consumers`)
  - Example: `contoso.onmicrosoft.com`

When you provide Instance and TenantId, they are combined to form an authority: `{Instance}{TenantId}`

### Q: Which configuration approach should I use: Authority or Instance/TenantId?

**A**: Both approaches are valid, but recommendations vary by scenario:

**Use Instance + TenantId when**:
- Configuring Azure AD single-tenant applications (most common)
- You want clear separation between instance and tenant
- You need to easily swap between environments with different tenants
- Following official Microsoft documentation examples

**Use Authority when**:
- Configuring Azure AD B2C (must include policy path)
- Configuring CIAM with custom domains (with `PreserveAuthority: true`)
- You prefer a single, complete URL
- Migrating from legacy configurations

**Never mix both** – choose one approach to avoid configuration conflicts.

### Q: What happens if I configure both Authority and Instance/TenantId?

**A**: **Instance and TenantId take precedence**, and Authority is completely ignored. You'll see a warning:

```
[Warning] [MsIdWeb] Authority 'https://login.microsoftonline.com/common' is being ignored 
because Instance 'https://login.microsoftonline.com/' and/or TenantId 'organizations' 
are already configured.
```

**EventId**: 408 (AuthorityConflict)

To fix: Remove either Authority OR both Instance and TenantId from your configuration.

## Warning Messages

### Q: Why do I see a warning "Both Authority and Instance/TenantId are set"?

**A**: You have configured conflicting authority properties. The warning indicates that your `Authority` setting is being ignored because you also specified `Instance` and/or `TenantId`, which take precedence.

**To resolve**:

**Option 1** - Remove Authority:
```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "organizations",
    "ClientId": "your-client-id"
  }
}
```

**Option 2** - Remove Instance and TenantId:
```json
{
  "AzureAd": {
    "Authority": "https://login.microsoftonline.com/organizations",
    "ClientId": "your-client-id"
  }
}
```

### Q: Is the warning critical? Will my application still work?

**A**: Your application will continue to work, but the warning indicates a configuration inconsistency:

- ✅ **Authentication works**: Instance and TenantId are used
- ⚠️ **Potential confusion**: Authority is ignored, which might not be what you intended
- 📝 **Best practice**: Clean up the configuration to remove the warning and improve clarity

The warning helps you catch potential misconfigurations before they cause issues.

### Q: How do I suppress the warning without changing my configuration?

**A**: **You shouldn't suppress the warning** – it indicates a real configuration issue. Instead, fix the configuration by choosing one approach (Authority OR Instance/TenantId).

If you have a specific reason to keep both (e.g., gradual migration), the warning will remain until the configuration is corrected.

## B2C-Specific Questions

### Q: Should I use Authority or Instance/TenantId for B2C?

**A**: **Always use Authority** for B2C applications. The Authority must include the policy path, which cannot be represented by Instance/TenantId alone.

**Correct B2C configuration**:
```json
{
  "AzureAdB2C": {
    "Authority": "https://contoso.b2clogin.com/contoso.onmicrosoft.com/B2C_1_susi",
    "ClientId": "your-client-id",
    "Domain": "contoso.onmicrosoft.com",
    "SignUpSignInPolicyId": "B2C_1_susi"
  }
}
```

**Why**: B2C policies are part of the authority path. Using Instance/TenantId would lose the policy information.

### Q: My B2C configuration has `/tfp/` in the URL. Do I need to remove it?

**A**: No, you don't need to remove it, but you can for clarity.

Microsoft.Identity.Web **automatically normalizes** the `/tfp/` segment:
- `https://contoso.b2clogin.com/tfp/contoso.onmicrosoft.com/B2C_1_susi`
- Becomes: `https://contoso.b2clogin.com/contoso.onmicrosoft.com/B2C_1_susi`

Both formats work identically. The modern format without `/tfp/` is recommended for new configurations.

### Q: Can I use Instance and TenantId with B2C if I set the policy ID separately?

**A**: **No, this is not recommended** and won't work correctly. The policy must be part of the Authority URL:

❌ **Wrong**:
```json
{
  "AzureAdB2C": {
    "Instance": "https://contoso.b2clogin.com/",
    "TenantId": "contoso.onmicrosoft.com",
    "SignUpSignInPolicyId": "B2C_1_susi",
    "ClientId": "your-client-id"
  }
}
```

✅ **Correct**:
```json
{
  "AzureAdB2C": {
    "Authority": "https://contoso.b2clogin.com/contoso.onmicrosoft.com/B2C_1_susi",
    "SignUpSignInPolicyId": "B2C_1_susi",
    "ClientId": "your-client-id",
    "Domain": "contoso.onmicrosoft.com"
  }
}
```

## CIAM-Specific Questions

### Q: What is PreserveAuthority and when should I use it?

**A**: `PreserveAuthority` is a boolean setting (default: `false`) that controls how the library processes the Authority URL:

- **When `false` (default)**: Authority is parsed into Instance and TenantId components
- **When `true`**: The full Authority URL is used as-is without parsing

**Use `PreserveAuthority: true` for**:
- CIAM with custom domains
- Any scenario where you want to prevent authority URL parsing
- Complex authority structures that shouldn't be split

**Example**:
```json
{
  "AzureAd": {
    "Authority": "https://login.contoso.com/contoso.onmicrosoft.com",
    "ClientId": "your-client-id",
    "PreserveAuthority": true
  }
}
```

### Q: Do I need PreserveAuthority for standard CIAM domains (ciamlogin.com)?

**A**: It's **recommended but not always required** for standard CIAM domains:

**Without PreserveAuthority** (works but may have edge cases):
```json
{
  "AzureAd": {
    "Authority": "https://contoso.ciamlogin.com/contoso.onmicrosoft.com",
    "ClientId": "your-client-id"
  }
}
```

**With PreserveAuthority** (recommended best practice):
```json
{
  "AzureAd": {
    "Authority": "https://contoso.ciamlogin.com/contoso.onmicrosoft.com",
    "ClientId": "your-client-id",
    "PreserveAuthority": true
  }
}
```

For **custom domains**, `PreserveAuthority: true` is **essential**.

### Q: What happens if I don't set PreserveAuthority with a CIAM custom domain?

**A**: The library may incorrectly parse your custom domain URL, leading to authentication failures:

❌ **Without PreserveAuthority** (may fail):
```json
{
  "AzureAd": {
    "Authority": "https://login.contoso.com/contoso.onmicrosoft.com",
    "ClientId": "your-client-id"
  }
}
```

**Potential Issue**: The library might parse `login.contoso.com` as the instance and `contoso.onmicrosoft.com` as the tenant, which could cause issues with custom domain routing.

✅ **With PreserveAuthority** (correct):
```json
{
  "AzureAd": {
    "Authority": "https://login.contoso.com/contoso.onmicrosoft.com",
    "ClientId": "your-client-id",
    "PreserveAuthority": true
  }
}
```

## Multi-Tenant Questions

### Q: How do I configure a multi-tenant Azure AD application?

**A**: Use `organizations` or `common` as the tenant identifier:

**Using Instance/TenantId**:
```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "organizations",
    "ClientId": "your-client-id"
  }
}
```

**Using Authority**:
```json
{
  "AzureAd": {
    "Authority": "https://login.microsoftonline.com/organizations",
    "ClientId": "your-client-id"
  }
}
```

**Tenant values**:
- `organizations`: Work/school accounts from any Azure AD tenant
- `common`: Both work/school accounts and Microsoft personal accounts
- `consumers`: Microsoft personal accounts only

### Q: What's the difference between 'common' and 'organizations'?

**A**:

| Tenant Value | Work/School Accounts | Personal Microsoft Accounts | Use Case |
|--------------|---------------------|---------------------------|----------|
| `organizations` | ✅ Yes | ❌ No | Business applications (most common) |
| `common` | ✅ Yes | ✅ Yes | Consumer-facing apps accepting both account types |
| `consumers` | ❌ No | ✅ Yes | Personal account-only applications |

**Recommendation**: Use `organizations` for most multi-tenant business applications.

## Configuration Edge Cases

### Q: Does the trailing slash in Instance matter?

**A**: No, trailing slashes are automatically normalized. These are equivalent:

```json
"Instance": "https://login.microsoftonline.com/"
"Instance": "https://login.microsoftonline.com"
```

Both work identically. The library ensures a trailing slash when needed.

### Q: Should I include `/v2.0` in my Authority?

**A**: **No**, Microsoft.Identity.Web uses the v2.0 endpoint by default. Don't append `/v2.0`:

❌ **Avoid**:
```json
{
  "AzureAd": {
    "Authority": "https://login.microsoftonline.com/common/v2.0"
  }
}
```

✅ **Correct**:
```json
{
  "AzureAd": {
    "Authority": "https://login.microsoftonline.com/common"
  }
}
```

### Q: Can I include query parameters in the Authority URL?

**A**: While technically possible, it's **not recommended**. Use the `ExtraQueryParameters` configuration option instead:

❌ **Not Recommended**:
```json
{
  "AzureAd": {
    "Authority": "https://login.microsoftonline.com/common?domain_hint=contoso.com"
  }
}
```

✅ **Recommended**:
```json
{
  "AzureAd": {
    "Authority": "https://login.microsoftonline.com/common",
    "ExtraQueryParameters": {
      "domain_hint": "contoso.com"
    }
  }
}
```

### Q: What if I forget the `https://` scheme in Authority?

**A**: You'll likely encounter parsing errors. Always include the full URL with scheme:

❌ **Wrong**:
```json
{
  "AzureAd": {
    "Authority": "login.microsoftonline.com/common"
  }
}
```

✅ **Correct**:
```json
{
  "AzureAd": {
    "Authority": "https://login.microsoftonline.com/common"
  }
}
```

## Government Cloud Questions

### Q: How do I configure for Azure Government Cloud?

**A**: Use the government cloud instance URL:

```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.us/",
    "TenantId": "your-tenant-id",
    "ClientId": "your-client-id"
  }
}
```

Or with Authority:
```json
{
  "AzureAd": {
    "Authority": "https://login.microsoftonline.us/your-tenant-id",
    "ClientId": "your-client-id"
  }
}
```

### Q: What are the instance URLs for different Azure clouds?

**A**:

| Cloud | Instance URL |
|-------|-------------|
| Azure Public | `https://login.microsoftonline.com/` |
| Azure Government (US) | `https://login.microsoftonline.us/` |
| Azure China | `https://login.chinacloudapi.cn/` |
| Azure Germany (deprecated) | `https://login.microsoftonline.de/` |

## Troubleshooting

### Q: Authentication fails after changing my configuration. What should I check?

**A**: Follow this checklist:

1. **Check for warnings**: Look for EventId 408 in your logs
2. **Verify redirect URIs**: Ensure they match your app registration in Azure
3. **Confirm authority format**: Validate the URL structure is correct
4. **Test sign-in flow**: Try to authenticate and note any error messages
5. **Enable debug logging**: Set `Microsoft.Identity.Web` log level to `Debug`
6. **Check HTTPS**: Ensure all URLs use `https://` (not `http://`)

### Q: How do I enable detailed logging to troubleshoot authority configuration?

**A**: Update your `appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.Identity.Web": "Debug",
      "Microsoft.Identity.Web.TokenAcquisition": "Trace"
    }
  }
}
```

This provides detailed logs about authority resolution and configuration.

### Q: My application was working, but now I see the EventId 408 warning. What changed?

**A**: Possible reasons:

1. **Configuration file updated**: Someone added conflicting Instance/TenantId or Authority
2. **Library upgrade**: Newer versions of Microsoft.Identity.Web added the warning
3. **Environment-specific override**: Different settings in environment-specific config files
4. **Code configuration**: Authority properties set programmatically in addition to configuration files

**Solution**: Review your configuration files (including environment-specific ones) and remove conflicting properties.

### Q: Can I have different authority configurations for development and production?

**A**: Yes, use environment-specific configuration files:

**appsettings.Development.json**:
```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "common"
  }
}
```

**appsettings.Production.json**:
```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "12345678-1234-1234-1234-123456789012"
  }
}
```

## Migration Questions

### Q: I'm upgrading from an older version of Microsoft.Identity.Web. Do I need to change my configuration?

**A**: Not necessarily, but you might see new warnings:

- **Existing configurations continue to work** due to backward compatibility
- **New warning (EventId 408)** may appear if you have conflicting settings
- **Recommended**: Review and clean up your configuration to follow current best practices

See the [Migration Guide](migration-authority-vs-instance.md) for detailed upgrade paths.

### Q: Should I migrate all my applications at once?

**A**: **No**, migrate incrementally:

1. Start with a non-production application
2. Test thoroughly
3. Migrate production applications one at a time
4. Monitor logs and authentication flows after each migration

## Additional Resources

- [Authority Configuration & Precedence Guide](authority-configuration.md)
- [Azure AD B2C Authority Examples](b2c-authority-examples.md)
- [CIAM Authority Examples](ciam-authority-examples.md)
- [Migration Guide: Authority vs Instance/TenantId](migration-authority-vs-instance.md)
- [Microsoft identity platform documentation](https://learn.microsoft.com/azure/active-directory/develop/)

## Still Have Questions?

If your question isn't answered here:

1. Check the [main documentation](authority-configuration.md)
2. Review [GitHub Issues](https://github.com/AzureAD/microsoft-identity-web/issues)
3. Ask on [Stack Overflow](https://stackoverflow.com) with tags `microsoft-identity-web` and `azure-ad`
4. Open a [new GitHub issue](https://github.com/AzureAD/microsoft-identity-web/issues/new) with your specific scenario
