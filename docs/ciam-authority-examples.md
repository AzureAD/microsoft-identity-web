# CIAM (Customer Identity Access Management) Authority Configuration Examples

This guide provides detailed examples and best practices for configuring authentication authorities in Microsoft Entra External ID (formerly CIAM - Customer Identity and Access Management) scenarios using Microsoft.Identity.Web.

## Overview

Microsoft Entra External ID (CIAM) is a customer identity and access management solution that enables organizations to create secure, customized sign-in experiences for customer-facing applications. CIAM authority configuration has specific requirements, particularly when using custom domains.

## Key CIAM Concepts

### CIAM vs Traditional Azure AD

| Feature | Traditional Azure AD | CIAM |
|---------|---------------------|------|
| **Primary Use Case** | Employee/organizational identity | Customer/consumer identity |
| **Default Domain** | `login.microsoftonline.com` | `{tenant}.ciamlogin.com` |
| **Custom Domains** | Optional | Commonly used for branding |
| **Authority Handling** | Parsed into Instance + TenantId | Use complete Authority URL |

## Recommended Configuration Patterns

### Standard CIAM Domain (ciamlogin.com)

For CIAM tenants using the default `.ciamlogin.com` domain:

```json
{
  "AzureAd": {
    "Authority": "https://contoso.ciamlogin.com/contoso.onmicrosoft.com",
    "ClientId": "11111111-1111-1111-1111-111111111111",
    "CallbackPath": "/signin-oidc"
  }
}
```

**Key Points**:
- Include the full tenant domain in the Authority
- The library automatically handles CIAM authorities correctly
- Do not mix Authority with Instance/TenantId properties

### CIAM with Custom Domain

### CIAM with Custom Domain

Custom domains are frequently used in CIAM for seamless brand experience:

```json
{
  "AzureAd": {
    "Authority": "https://login.contoso.com/contoso.onmicrosoft.com",
    "ClientId": "11111111-1111-1111-1111-111111111111",
    "CallbackPath": "/signin-oidc"
  }
}
```

**Important**: Ensure your custom domain is properly configured in your CIAM tenant. The library handles custom CIAM domains automatically.

### CIAM with Tenant ID (GUID)

Using the tenant GUID instead of domain name:

```json
{
  "AzureAd": {
    "Authority": "https://contoso.ciamlogin.com/12345678-1234-1234-1234-123456789012",
    "ClientId": "11111111-1111-1111-1111-111111111111"
  }
}
```

### CIAM Multi-Tenant Scenario

For CIAM applications supporting multiple customer tenants:

```json
{
  "AzureAd": {
    "Authority": "https://contoso.ciamlogin.com/common",
    "ClientId": "11111111-1111-1111-1111-111111111111"
,
    "ValidateIssuer": false
  }
}
```

**Warning**: Multi-tenant CIAM scenarios require careful issuer validation configuration. Ensure you implement proper tenant isolation in your application logic.

## Code Configuration

### ASP.NET Core Startup Configuration

**Program.cs (.NET 6+)**:
```csharp
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Identity.Web;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(options =>
    {
        builder.Configuration.Bind("AzureAd", options);
    });

builder.Services.AddAuthorization();
builder.Services.AddRazorPages();

var app = builder.Build();

// Configure the HTTP request pipeline
app.UseAuthentication();
app.UseAuthorization();
app.MapRazorPages();

app.Run();
```

### Advanced Configuration with Events

```csharp
builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(options =>
    {
        builder.Configuration.Bind("AzureAd", options);
        
        options.Events = new OpenIdConnectEvents
        {
            OnRedirectToIdentityProvider = context =>
            {
                // Add custom parameters if needed
                context.ProtocolMessage.SetParameter("ui_locales", "en-US");
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                // Custom validation logic
                var tenantId = context.Principal?.FindFirst("tid")?.Value;
                // Implement tenant-specific logic
                return Task.CompletedTask;
            }
        };
    });
```

## Custom Domain Configuration in Azure

To use custom domains with CIAM, configure your tenant:

### Step 1: Configure Custom Domain in Azure Portal

1. Navigate to **Microsoft Entra admin center**
2. Select your CIAM tenant
3. Go to **Custom domains**
4. Add and verify your custom domain (e.g., `login.contoso.com`)
5. Configure DNS CNAME records as instructed

### Step 2: Update Application Configuration

```json
{
  "AzureAd": {
    "Authority": "https://login.contoso.com/{tenant-id-or-domain}",
    "ClientId": "your-client-id",
    "CallbackPath": "/signin-oidc"
  }
}
```

### Step 3: Update Redirect URIs

In your app registration, add redirect URIs using the custom domain:
- `https://yourapp.com/signin-oidc`
- `https://yourapp.com/signout-callback-oidc`

## Common CIAM Configuration Mistakes

### ❌ Mistake 1: Mixing Authority with Instance/TenantId in CIAM

**Wrong**:
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

**Correct**: Use only Authority:
```json
{
  "AzureAd": {
    "Authority": "https://contoso.ciamlogin.com/contoso.onmicrosoft.com",
    "ClientId": "11111111-1111-1111-1111-111111111111"
  }
}
```

### ❌ Mistake 2: Using Standard AAD Login URL for CIAM

**Wrong**:
```json
{
  "AzureAd": {
    "Authority": "https://login.microsoftonline.com/contoso.onmicrosoft.com",
    "ClientId": "11111111-1111-1111-1111-111111111111"
  }
}
```

**Correct**: Use the CIAM-specific domain:
```json
{
  "AzureAd": {
    "Authority": "https://contoso.ciamlogin.com/contoso.onmicrosoft.com",
    "ClientId": "11111111-1111-1111-1111-111111111111"
  }
}
```

## Migration from B2C to CIAM

Organizations migrating from Azure AD B2C to CIAM should note key differences:

### B2C Configuration
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

### CIAM Configuration
```json
{
  "AzureAd": {
    "Authority": "https://contoso.ciamlogin.com/contoso.onmicrosoft.com",
    "ClientId": "22222222-2222-2222-2222-222222222222"
  }
}
```

**Key Differences**:
- CIAM doesn't use policy IDs in the Authority
- CIAM uses `AzureAd` configuration section (not `AzureAdB2C`)
- The library automatically handles CIAM authorities
- No need for `Domain` property unless specific scenarios require it

## Environment-Specific Configuration

### Development Environment

```json
{
  "AzureAd": {
    "Authority": "https://contoso-dev.ciamlogin.com/contoso-dev.onmicrosoft.com",
    "ClientId": "dev-client-id",
    "CallbackPath": "/signin-oidc"
  }
}
```

### Production Environment

```json
{
  "AzureAd": {
    "Authority": "https://login.contoso.com/contoso.onmicrosoft.com",
    "ClientId": "prod-client-id",
    "CallbackPath": "/signin-oidc"
  }
}
```

Use separate configuration files:
- `appsettings.Development.json`
- `appsettings.Staging.json`
- `appsettings.Production.json`

## Testing and Validation

### Verify CIAM Configuration

1. **Check Authority Format**: Ensure it uses CIAM domain (`.ciamlogin.com` or custom domain)
2. **Avoid Instance/TenantId**: Use Authority only, not mixed with Instance/TenantId
3. **Test Sign-in Flow**: Verify authentication redirects work correctly
4. **Monitor Logs**: Check for EventId 408 warnings indicating configuration conflicts

### Debugging Tips

**Issue**: "The reply URL specified does not match"
- **Cause**: Redirect URI mismatch between app configuration and Azure registration
- **Fix**: Ensure CallbackPath matches Azure app registration redirect URIs

**Issue**: "AADSTS50011: The reply URL does not match"
- **Cause**: Custom domain not properly configured
- **Fix**: Verify custom domain DNS settings and Azure CIAM configuration

**Issue**: Authority configuration errors
- **Cause**: Mixing Authority with Instance/TenantId
- **Fix**: Use Authority only for CIAM configurations

## Advanced Scenarios

### CIAM with API Protection

Protecting a Web API with CIAM:

```csharp
// Program.cs for Web API
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(options =>
    {
        builder.Configuration.Bind("AzureAd", options);
    },
    options =>
    {
        builder.Configuration.Bind("AzureAd", options);
    });
```

### CIAM with Downstream API Calls

```csharp
builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(options =>
    {
        builder.Configuration.Bind("AzureAd", options);
    })
    .EnableTokenAcquisitionToCallDownstreamApi(
        builder.Configuration.GetSection("DownstreamApi:Scopes").Get<string[]>())
    .AddInMemoryTokenCaches();

builder.Services.AddDownstreamApi("DownstreamApi", 
    builder.Configuration.GetSection("DownstreamApi"));
```

## Additional Resources

- [Authority Configuration & Precedence Guide](authority-configuration.md)
- [Azure AD B2C Authority Examples](b2c-authority-examples.md)
- [Migration Guide](migration-authority-vs-instance.md)
- [Microsoft Entra External ID documentation](https://learn.microsoft.com/entra/external-id/)
- [CIAM custom domains](https://learn.microsoft.com/entra/external-id/customers/how-to-custom-domain)
