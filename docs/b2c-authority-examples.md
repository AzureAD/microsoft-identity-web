# Azure AD B2C Authority Configuration Examples

This guide provides detailed examples and best practices for configuring authentication authorities in Azure AD B2C scenarios using Microsoft.Identity.Web.

## Overview

Azure AD B2C (Business to Consumer) is a cloud identity service that enables custom sign-up, sign-in, and profile management for consumer-facing applications. B2C authority configuration has unique characteristics compared to standard Azure AD due to its policy-based architecture.

## Key B2C Concepts

### User Flows (Policies)

B2C uses user flows (policies) to define authentication experiences:
- **Sign-up and Sign-in (SUSI)**: Combined registration and authentication flow
- **Password Reset**: Self-service password recovery
- **Profile Editing**: User profile modification

Each policy has a unique identifier like `B2C_1_susi`, `B2C_1_reset`, or `B2C_1_edit_profile`.

### B2C Authority Structure

A complete B2C authority URL includes:
1. **B2C Login Instance**: `https://{tenant-name}.b2clogin.com/`
2. **Tenant Domain**: `{tenant-name}.onmicrosoft.com`
3. **Policy Path**: The user flow identifier

Example: `https://contoso.b2clogin.com/contoso.onmicrosoft.com/B2C_1_susi`

## Recommended Configuration Pattern

### Primary Configuration (appsettings.json)

Always use the complete `Authority` URL including the policy path. Do NOT split into `Instance` and `TenantId` for B2C.

```json
{
  "AzureAdB2C": {
    "Authority": "https://contoso.b2clogin.com/contoso.onmicrosoft.com/B2C_1_susi",
    "ClientId": "11111111-1111-1111-1111-111111111111",
    "Domain": "contoso.onmicrosoft.com",
    "SignUpSignInPolicyId": "B2C_1_susi",
    "ResetPasswordPolicyId": "B2C_1_reset",
    "EditProfilePolicyId": "B2C_1_edit_profile"
  }
}
```

### Multiple Policies

When your application supports multiple user flows, configure all policy IDs:

```json
{
  "AzureAdB2C": {
    "Authority": "https://fabrikam.b2clogin.com/fabrikam.onmicrosoft.com/B2C_1_signupsignin1",
    "ClientId": "22222222-2222-2222-2222-222222222222",
    "Domain": "fabrikam.onmicrosoft.com",
    "SignUpSignInPolicyId": "B2C_1_signupsignin1",
    "ResetPasswordPolicyId": "B2C_1_passwordreset1",
    "EditProfilePolicyId": "B2C_1_profileediting1",
    "CallbackPath": "/signin-oidc"
  }
}
```

### Code Configuration (Program.cs or Startup.cs)

```csharp
builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(options =>
    {
        builder.Configuration.Bind("AzureAdB2C", options);
    });
```

## Legacy /tfp/ Path Normalization

### Historical Context

Older B2C implementations used the `/tfp/` (Trust Framework Policy) path segment:

```
https://contoso.b2clogin.com/tfp/contoso.onmicrosoft.com/B2C_1_susi
```

### Automatic Normalization

Microsoft.Identity.Web **automatically removes** the `/tfp/` segment during authority preparation. Both formats work identically:

**Legacy format** (auto-normalized):
```json
{
  "AzureAdB2C": {
    "Authority": "https://contoso.b2clogin.com/tfp/contoso.onmicrosoft.com/B2C_1_susi",
    "ClientId": "11111111-1111-1111-1111-111111111111",
    "Domain": "contoso.onmicrosoft.com"
  }
}
```

**Modern format** (recommended):
```json
{
  "AzureAdB2C": {
    "Authority": "https://contoso.b2clogin.com/contoso.onmicrosoft.com/B2C_1_susi",
    "ClientId": "11111111-1111-1111-1111-111111111111",
    "Domain": "contoso.onmicrosoft.com"
  }
}
```

Both configurations result in the same prepared instance: `https://contoso.b2clogin.com/contoso.onmicrosoft.com/`

### Migration from /tfp/

If you're migrating from legacy `/tfp/` URLs:

1. **No action required**: Your existing configuration continues to work
2. **Optional cleanup**: Remove `/tfp/` from your configuration for clarity
3. **No breaking changes**: The normalization is transparent to your application

## Custom Domains in B2C

### Standard b2clogin.com Domain

Most B2C tenants use the standard `{tenant}.b2clogin.com` domain:

```json
{
  "AzureAdB2C": {
    "Authority": "https://contoso.b2clogin.com/contoso.onmicrosoft.com/B2C_1_susi",
    "ClientId": "11111111-1111-1111-1111-111111111111",
    "Domain": "contoso.onmicrosoft.com"
  }
}
```

### Custom Domain Configuration

For B2C custom domains (e.g., `login.contoso.com`):

```json
{
  "AzureAdB2C": {
    "Authority": "https://login.contoso.com/contoso.onmicrosoft.com/B2C_1_susi",
    "ClientId": "11111111-1111-1111-1111-111111111111",
    "Domain": "contoso.onmicrosoft.com"
  }
}
```

**Note**: Custom domains require additional B2C tenant configuration. See [Azure AD B2C custom domains documentation](https://learn.microsoft.com/azure/active-directory-b2c/custom-domain).

## Policy Switching at Runtime

### Handling Password Reset Flow

B2C password reset is typically invoked from the sign-in page. Configure error handling:

```csharp
builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(options =>
    {
        builder.Configuration.Bind("AzureAdB2C", options);
        
        options.Events = new OpenIdConnectEvents
        {
            OnRemoteFailure = async context =>
            {
                // Handle password reset error
                if (context.Failure?.Message?.Contains("AADB2C90118") == true)
                {
                    context.Response.Redirect($"/AzureAdB2C/Account/ResetPassword");
                    context.HandleResponse();
                }
            }
        };
    });
```

### Profile Edit Flow

```csharp
[Authorize]
public IActionResult EditProfile()
{
    var editProfileUrl = $"{Configuration["AzureAdB2C:Instance"]}" +
                        $"{Configuration["AzureAdB2C:Domain"]}/" +
                        $"{Configuration["AzureAdB2C:EditProfilePolicyId"]}" +
                        "/oauth2/v2.0/authorize" +
                        $"?client_id={Configuration["AzureAdB2C:ClientId"]}" +
                        $"&redirect_uri={Request.Scheme}://{Request.Host}/signin-oidc" +
                        "&response_type=id_token" +
                        "&scope=openid profile" +
                        "&response_mode=form_post" +
                        $"&nonce={Guid.NewGuid()}";
    
    return Redirect(editProfileUrl);
}
```

## Common B2C Configuration Mistakes

### ❌ Mistake 1: Mixing Authority with Instance/TenantId

**Wrong**:
```json
{
  "AzureAdB2C": {
    "Authority": "https://contoso.b2clogin.com/contoso.onmicrosoft.com/B2C_1_susi",
    "Instance": "https://contoso.b2clogin.com/",
    "TenantId": "contoso.onmicrosoft.com",
    "ClientId": "11111111-1111-1111-1111-111111111111"
  }
}
```

**Correct**: Choose ONE approach:
```json
{
  "AzureAdB2C": {
    "Authority": "https://contoso.b2clogin.com/contoso.onmicrosoft.com/B2C_1_susi",
    "ClientId": "11111111-1111-1111-1111-111111111111",
    "Domain": "contoso.onmicrosoft.com"
  }
}
```

### ❌ Mistake 2: Omitting Policy from Authority

**Wrong**:
```json
{
  "AzureAdB2C": {
    "Authority": "https://contoso.b2clogin.com/contoso.onmicrosoft.com",
    "ClientId": "11111111-1111-1111-1111-111111111111"
  }
}
```

**Correct**: Include the policy in the Authority:
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

### ❌ Mistake 3: Using login.microsoftonline.com for B2C

**Wrong**:
```json
{
  "AzureAdB2C": {
    "Authority": "https://login.microsoftonline.com/contoso.onmicrosoft.com/B2C_1_susi",
    "ClientId": "11111111-1111-1111-1111-111111111111"
  }
}
```

**Correct**: Use the B2C-specific domain:
```json
{
  "AzureAdB2C": {
    "Authority": "https://contoso.b2clogin.com/contoso.onmicrosoft.com/B2C_1_susi",
    "ClientId": "11111111-1111-1111-1111-111111111111",
    "Domain": "contoso.onmicrosoft.com"
  }
}
```

## Multi-Region B2C

For applications deployed across regions with B2C instances in multiple geographies:

### Configuration per Region

**US Region**:
```json
{
  "AzureAdB2C": {
    "Authority": "https://contoso.b2clogin.com/contoso.onmicrosoft.com/B2C_1_susi",
    "ClientId": "11111111-1111-1111-1111-111111111111",
    "Domain": "contoso.onmicrosoft.com"
  }
}
```

**EU Region** (using custom domain for GDPR compliance):
```json
{
  "AzureAdB2C": {
    "Authority": "https://login.contoso.eu/contoso.onmicrosoft.com/B2C_1_susi",
    "ClientId": "11111111-1111-1111-1111-111111111111",
    "Domain": "contoso.onmicrosoft.com"
  }
}
```

Use environment-specific configuration files (`appsettings.Production.json`, `appsettings.Development.json`) to manage regional variations.

## Testing and Validation

### Verify B2C Configuration

1. **Check Authority Format**: Ensure it includes instance, tenant domain, and policy
2. **Validate Policy IDs**: Confirm they match your B2C tenant configuration
3. **Test All Flows**: Sign-up, sign-in, password reset, and profile edit
4. **Monitor Logs**: Watch for EventId 408 warnings indicating configuration conflicts

### Common Validation Errors

**Error**: "AADB2C90008: The provided grant has not been issued for this endpoint"
- **Cause**: Mismatch between configured authority and actual policy
- **Fix**: Verify the policy ID in your Authority matches the B2C tenant

**Error**: "AADB2C90091: The user has cancelled entering self-asserted information"
- **Cause**: User canceled the flow (not a configuration error)
- **Fix**: Handle gracefully in your application

## Migration from Microsoft.Identity.Web v1.x

If upgrading from earlier versions:

### Before (v1.x)
```json
{
  "AzureAdB2C": {
    "Instance": "https://contoso.b2clogin.com/tfp/",
    "ClientId": "11111111-1111-1111-1111-111111111111",
    "Domain": "contoso.onmicrosoft.com",
    "SignUpSignInPolicyId": "B2C_1_susi"
  }
}
```

### After (v2.x and later)
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

**Note**: The v1.x pattern still works due to backward compatibility, but the Authority-based approach is recommended for clarity.

## Additional Resources

- [Authority Configuration & Precedence Guide](authority-configuration.md)
- [CIAM Authority Examples](ciam-authority-examples.md)
- [Migration Guide](migration-authority-vs-instance.md)
- [Azure AD B2C documentation](https://learn.microsoft.com/azure/active-directory-b2c/)
- [B2C custom policies](https://learn.microsoft.com/azure/active-directory-b2c/custom-policy-overview)
