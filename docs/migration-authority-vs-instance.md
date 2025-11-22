# Migration Guide: Authority vs Instance/TenantId Configuration

This guide helps you upgrade existing Microsoft.Identity.Web applications to use recommended authority configuration patterns, especially if you're encountering the EventId 408 warning about conflicting Authority and Instance/TenantId settings.

## Understanding the Warning

If you see a log message like this:

```
[Warning] [MsIdWeb] Authority 'https://login.microsoftonline.com/common' is being ignored 
because Instance 'https://login.microsoftonline.com/' and/or TenantId 'organizations' 
are already configured. To use Authority, remove Instance and TenantId from the configuration.
```

This indicates your configuration has **conflicting authority settings**. The library uses Instance and TenantId, completely ignoring the Authority value.

**Event ID**: 408 (AuthorityConflict)

## Quick Fix Options

### Option 1: Remove Authority (Recommended for most scenarios)

**Before**:
```json
{
  "AzureAd": {
    "Authority": "https://login.microsoftonline.com/common",
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "organizations",
    "ClientId": "11111111-1111-1111-1111-111111111111"
  }
}
```

**After**:
```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "organizations",
    "ClientId": "11111111-1111-1111-1111-111111111111"
  }
}
```

### Option 2: Remove Instance and TenantId (Simpler for some scenarios)

**Before**:
```json
{
  "AzureAd": {
    "Authority": "https://login.microsoftonline.com/common",
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "common",
    "ClientId": "11111111-1111-1111-1111-111111111111"
  }
}
```

**After**:
```json
{
  "AzureAd": {
    "Authority": "https://login.microsoftonline.com/common",
    "ClientId": "11111111-1111-1111-1111-111111111111"
  }
}
```

## Scenario-Specific Migration Patterns

### Azure AD Single-Tenant Applications

#### Pattern 1: From Authority to Instance/TenantId

**Before (using Authority)**:
```json
{
  "AzureAd": {
    "Authority": "https://login.microsoftonline.com/12345678-1234-1234-1234-123456789012",
    "ClientId": "11111111-1111-1111-1111-111111111111"
  }
}
```

**After (split into Instance/TenantId - Recommended)**:
```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "12345678-1234-1234-1234-123456789012",
    "ClientId": "11111111-1111-1111-1111-111111111111"
  }
}
```

**Benefits**:
- Clear separation of instance and tenant
- Easier to update for different environments
- Consistent with Microsoft documentation

#### Pattern 2: Keep Authority (Also Valid)

If you prefer the Authority format, you can keep it:

```json
{
  "AzureAd": {
    "Authority": "https://login.microsoftonline.com/12345678-1234-1234-1234-123456789012",
    "ClientId": "11111111-1111-1111-1111-111111111111"
  }
}
```

**Note**: The library will automatically parse this into Instance and TenantId internally.

### Azure AD Multi-Tenant Applications

#### From Mixed Configuration

**Before (conflicting configuration)**:
```json
{
  "AzureAd": {
    "Authority": "https://login.microsoftonline.com/organizations",
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "common",
    "ClientId": "11111111-1111-1111-1111-111111111111"
  }
}
```

**After (using Instance/TenantId)**:
```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "organizations",
    "ClientId": "11111111-1111-1111-1111-111111111111"
  }
}
```

**Alternative (using Authority)**:
```json
{
  "AzureAd": {
    "Authority": "https://login.microsoftonline.com/organizations",
    "ClientId": "11111111-1111-1111-1111-111111111111"
  }
}
```

### Azure AD B2C Applications

For B2C, **always use Authority** including the policy path. Do NOT use Instance/TenantId separately.

#### Consolidate to Authority-Only

**Before (incorrect mixed configuration)**:
```json
{
  "AzureAdB2C": {
    "Authority": "https://contoso.b2clogin.com/contoso.onmicrosoft.com/B2C_1_susi",
    "Instance": "https://contoso.b2clogin.com/",
    "TenantId": "contoso.onmicrosoft.com",
    "ClientId": "11111111-1111-1111-1111-111111111111",
    "Domain": "contoso.onmicrosoft.com"
  }
}
```

**After (correct Authority-based configuration)**:
```json
{
  "AzureAdB2C": {
    "Authority": "https://contoso.b2clogin.com/contoso.onmicrosoft.com/B2C_1_susi",
    "ClientId": "11111111-1111-1111-1111-111111111111",
    "Domain": "contoso.onmicrosoft.com",
    "SignUpSignInPolicyId": "B2C_1_susi"
  }
}
```

**Critical**: B2C requires the policy path in the Authority. Splitting into Instance/TenantId loses the policy information.

### CIAM Applications

For CIAM, use the complete Authority URL. The library handles CIAM authorities automatically.

#### Remove Conflicting Properties

**Before (conflicting configuration)**:
```json
{
  "AzureAd": {
    "Authority": "https://contoso.ciamlogin.com/contoso.onmicrosoft.com",
    "Instance": "https://contoso.ciamlogin.com/",
    "TenantId": "contoso.onmicrosoft.com",
    "ClientId": "11111111-1111-1111-1111-111111111111"
  }
}
```

**After (correct CIAM configuration)**:
```json
{
  "AzureAd": {
    "Authority": "https://contoso.ciamlogin.com/contoso.onmicrosoft.com",
    "ClientId": "11111111-1111-1111-1111-111111111111"
  }
}
```

#### Custom Domain CIAM

**Before**:
```json
{
  "AzureAd": {
    "Authority": "https://login.contoso.com/contoso.onmicrosoft.com",
    "Instance": "https://login.contoso.com/",
    "ClientId": "11111111-1111-1111-1111-111111111111"
  }
}
```

**After**:
```json
{
  "AzureAd": {
    "Authority": "https://login.contoso.com/contoso.onmicrosoft.com",
    "ClientId": "11111111-1111-1111-1111-111111111111"
  }
}
```

**Note**: Ensure your custom domain is properly configured in your CIAM tenant before using it in your application.

## Government Cloud Migrations

### Azure Government (US)

**Before (conflicting)**:
```json
{
  "AzureAd": {
    "Authority": "https://login.microsoftonline.us/12345678-1234-1234-1234-123456789012",
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "12345678-1234-1234-1234-123456789012",
    "ClientId": "11111111-1111-1111-1111-111111111111"
  }
}
```

**After (corrected)**:
```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.us/",
    "TenantId": "12345678-1234-1234-1234-123456789012",
    "ClientId": "11111111-1111-1111-1111-111111111111"
  }
}
```

### Azure China

**After (using Instance/TenantId)**:
```json
{
  "AzureAd": {
    "Instance": "https://login.chinacloudapi.cn/",
    "TenantId": "12345678-1234-1234-1234-123456789012",
    "ClientId": "11111111-1111-1111-1111-111111111111"
  }
}
```

**Alternative (using Authority)**:
```json
{
  "AzureAd": {
    "Authority": "https://login.chinacloudapi.cn/12345678-1234-1234-1234-123456789012",
    "ClientId": "11111111-1111-1111-1111-111111111111"
  }
}
```

## Multi-Environment Configuration Strategy

### Using Environment-Specific Files

Instead of maintaining different configurations in code, use environment-specific settings files:

#### appsettings.json (base configuration)
```json
{
  "AzureAd": {
    "ClientId": "11111111-1111-1111-1111-111111111111",
    "CallbackPath": "/signin-oidc"
  }
}
```

#### appsettings.Development.json
```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "common"
  }
}
```

#### appsettings.Production.json
```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "12345678-1234-1234-1234-123456789012"
  }
}
```

### Using Azure Key Vault for Authority Settings

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// Load configuration from Key Vault
if (builder.Environment.IsProduction())
{
    var keyVaultEndpoint = new Uri(builder.Configuration["KeyVaultEndpoint"]!);
    builder.Configuration.AddAzureKeyVault(
        keyVaultEndpoint,
        new DefaultAzureCredential());
}

builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"));
```

**Key Vault Secrets**:
- `AzureAd--Instance`: `https://login.microsoftonline.com/`
- `AzureAd--TenantId`: `12345678-1234-1234-1234-123456789012`
- `AzureAd--ClientId`: `11111111-1111-1111-1111-111111111111`
- `AzureAd--ClientSecret`: `your-client-secret`

## Code-Based Configuration Migration

### Before: Mixed Configuration in Code

```csharp
// Startup.cs or Program.cs (old pattern)
services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(options =>
    {
        options.Authority = "https://login.microsoftonline.com/common";
        options.Instance = "https://login.microsoftonline.com/";
        options.TenantId = "organizations";
        options.ClientId = "11111111-1111-1111-1111-111111111111";
    });
```

### After: Consistent Configuration

**Option 1: Using Instance/TenantId**:
```csharp
services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(options =>
    {
        options.Instance = "https://login.microsoftonline.com/";
        options.TenantId = "organizations";
        options.ClientId = "11111111-1111-1111-1111-111111111111";
    });
```

**Option 2: Using Authority**:
```csharp
services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(options =>
    {
        options.Authority = "https://login.microsoftonline.com/organizations";
        options.ClientId = "11111111-1111-1111-1111-111111111111";
    });
```

**Option 3: Using Configuration Section (Recommended)**:
```csharp
services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(Configuration.GetSection("AzureAd"));
```

## Testing Your Migration

### Step 1: Update Configuration

Choose your preferred pattern and update `appsettings.json` accordingly.

### Step 2: Clear Warning Logs

After updating your configuration, restart your application and verify that the EventId 408 warning no longer appears in your logs.

### Step 3: Verify Authentication Flow

1. Navigate to a protected page in your application
2. Verify you're redirected to the correct sign-in page
3. Sign in and verify successful authentication
4. Check that tokens are acquired correctly

### Step 4: Monitor Logs

Enable detailed logging to verify the configuration is applied correctly:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.Identity.Web": "Debug"
    }
  }
}
```

Look for log entries confirming your authority configuration without warnings.

## Common Migration Issues

### Issue 1: Sign-in Redirect to Wrong Tenant

**Symptom**: Users are redirected to an unexpected tenant for authentication.

**Cause**: Instance or TenantId values don't match the intended authority.

**Solution**: Verify that Instance and TenantId, when combined, equal your intended Authority URL.

### Issue 2: Configuration Not Taking Effect

**Symptom**: Changes to configuration don't seem to apply.

**Cause**: Configuration caching or environment-specific overrides.

**Solution**: 
- Restart the application
- Check for environment-specific settings files that might override your changes
- Verify configuration binding in code

### Issue 3: B2C Policy Not Found

**Symptom**: "AADB2C90008: The provided grant has not been issued for this endpoint"

**Cause**: Policy path missing from Authority after migration.

**Solution**: Ensure the B2C Authority includes the full policy path:
```json
{
  "AzureAdB2C": {
    "Authority": "https://contoso.b2clogin.com/contoso.onmicrosoft.com/B2C_1_susi",
    "ClientId": "..."
  }
}
```

### Issue 4: CIAM Custom Domain Errors

**Symptom**: Authentication fails with custom domain.

**Cause**: Mixing Authority with Instance/TenantId or custom domain not configured properly in Azure.

**Solution**: Use Authority only and verify custom domain configuration:
```json
{
  "AzureAd": {
    "Authority": "https://login.contoso.com/contoso.onmicrosoft.com",
    "ClientId": "..."
  }
}
```

Ensure your custom domain is properly configured in the Azure portal.

## Rollback Plan

If migration causes issues, you can temporarily revert while investigating:

### Quick Rollback

1. Restore your previous `appsettings.json` from version control
2. Restart the application
3. Verify authentication works with the old configuration

### Gradual Migration

If you have multiple applications:

1. Migrate one application first
2. Test thoroughly in non-production environments
3. Monitor for issues before migrating additional applications
4. Use feature flags if available to toggle between configurations

## Additional Resources

- [Authority Configuration & Precedence Guide](authority-configuration.md)
- [Azure AD B2C Authority Examples](b2c-authority-examples.md)
- [CIAM Authority Examples](ciam-authority-examples.md)
- [Authority Precedence FAQ](faq-authority-precedence.md)
- [Microsoft.Identity.Web Wiki](https://github.com/AzureAD/microsoft-identity-web/wiki)

## Getting Help

If you encounter issues during migration:

1. Check the [FAQ](faq-authority-precedence.md) for common questions
2. Enable debug logging to gather diagnostic information
3. Review the [GitHub Issues](https://github.com/AzureAD/microsoft-identity-web/issues) for similar problems
4. Open a new issue with detailed configuration (sanitize sensitive values) and log output
