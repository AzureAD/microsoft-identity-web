# Authority Precedence FAQ

This FAQ addresses common questions about authority configuration, conflict detection, and troubleshooting configuration errors in Microsoft.Identity.Web.

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

**A**: Recommendations depend on the scenario:

**Use Instance + TenantId when** (strongly preferred for AAD):
- Configuring Azure AD single-tenant or multi-tenant applications
- You want clear separation between instance and tenant
- You need to easily swap between environments with different tenants
- Following official Microsoft documentation examples

Using `Instance` + `TenantId` routes through MSAL's `WithAuthority()`, which enables AAD-specific security and resilience features (instance discovery, authority validation, regional fallback).

**Use Authority when**:
- Configuring Azure AD B2C (must include policy path)
- Configuring CIAM (standard or custom domains)

> **Warning**: Using `Authority` alone for AAD endpoints routes through MSAL's generic OIDC path (`WithOidcAuthority`), which skips AAD-specific security and resilience features. Avoid this for standard AAD scenarios.

**Never mix both** -- setting both Authority and Instance/TenantId throws an `InvalidOperationException` at startup.

### Q: What happens if I configure both Authority and Instance/TenantId?

**A**: Your application **throws an `InvalidOperationException` at startup** with a clear error message:

```
System.InvalidOperationException: [MsIdWeb] Both 'Authority' ('https://login.microsoftonline.com/common')
and 'Instance'/'TenantId' ('https://login.microsoftonline.com/', 'organizations') are configured.
These settings conflict. Remove either 'Authority' or 'Instance'/'TenantId' from the configuration.
```

To fix: Remove either Authority OR both Instance and TenantId from your configuration.

## Configuration Errors

### Q: My application fails to start with "Both 'Authority' and 'Instance'/'TenantId' are configured". What do I do?

**A**: You have configured conflicting authority properties. Choose one approach:

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

### Q: I upgraded Microsoft.Identity.Web and my app no longer starts. What happened?

**A**: Starting with the version that includes PR #3873, configuring both `Authority` and `Instance`/`TenantId` throws an `InvalidOperationException` instead of silently ignoring `Authority`. This is a breaking change that surfaces previously hidden configuration conflicts.

**To fix**: Choose one approach -- either `Authority` alone or `Instance` + `TenantId` alone. See the [Migration Guide](migration-authority-vs-instance.md) for detailed upgrade paths.

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

### Q: How do I configure CIAM applications?

**A**: For CIAM applications, use the complete Authority URL. The library automatically handles CIAM authorities correctly:

**Standard CIAM domain**:
```json
{
  "AzureAd": {
    "Authority": "https://contoso.ciamlogin.com/contoso.onmicrosoft.com",
    "ClientId": "your-client-id"
  }
}
```

**Custom CIAM domain**:
```json
{
  "AzureAd": {
    "Authority": "https://login.contoso.com/contoso.onmicrosoft.com",
    "ClientId": "your-client-id"
  }
}
```

**Important**: Do not mix Authority with Instance/TenantId for CIAM scenarios -- doing so throws `InvalidOperationException`.

### Q: Do I need special configuration for CIAM custom domains?

**A**: No special configuration is needed beyond ensuring your custom domain is properly configured in your CIAM tenant. Use the complete Authority URL with your custom domain, and the library will handle it automatically:

```json
{
  "AzureAd": {
    "Authority": "https://login.contoso.com/contoso.onmicrosoft.com",
    "ClientId": "your-client-id"
  }
}
```

Make sure your custom domain DNS records are correctly configured in the Azure portal before using it in your application.

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

**Using Authority** (not recommended for AAD -- loses AAD-specific security features):
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

Or with Authority (not recommended -- loses AAD-specific security features):
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

1. **Check for conflicts**: Look for `InvalidOperationException` about Authority/Instance/TenantId conflict
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

### Q: My application was working, but now it throws an InvalidOperationException about Authority conflicts. What changed?

**A**: Possible reasons:

1. **Library upgrade**: Newer versions of Microsoft.Identity.Web throw on this conflict instead of silently ignoring Authority
2. **Configuration file updated**: Someone added conflicting Instance/TenantId or Authority
3. **Environment-specific override**: Different settings in environment-specific config files
4. **Code configuration**: Authority properties set programmatically in addition to configuration files

**Solution**: Choose one approach -- either `Authority` alone or `Instance`/`TenantId` alone. Review all configuration sources (including environment-specific files) and remove the conflicting properties.

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

**A**: Depending on your version, behavior differs:

- **Older versions**: Conflicting configurations were silently resolved (Authority was ignored)
- **Current version**: Conflicting configurations throw `InvalidOperationException` at startup
- **Recommended**: Review and clean up your configuration to use only one approach

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
