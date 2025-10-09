# Configuration Reference

This document provides comprehensive configuration options for the Microsoft Entra Identity Sidecar.

## Configuration Overview

The sidecar is configured using environment variables following ASP.NET Core configuration conventions. Configuration values can be provided via:

- Environment variables (recommended for Kubernetes)
- `appsettings.json` file
- Command-line arguments
- Azure App Configuration or Key Vault (for advanced scenarios)

## Configuration Schema

### Core Azure AD Settings

#### AzureAd Section

```yaml
env:
- name: AzureAd__Instance
  value: "https://login.microsoftonline.com/"
- name: AzureAd__TenantId
  value: "<your-tenant-id>"
- name: AzureAd__ClientId
  value: "<your-client-id>"
- name: AzureAd__Audience
  value: "<expected-audience>"  # Optional
- name: AzureAd__Scopes
  value: "access_as_user"  # Optional, for scope validation
```

**Configuration Keys:**

| Key | Description | Required | Default |
|-----|-------------|----------|---------|
| `AzureAd__Instance` | Microsoft Entra authority URL | No | `https://login.microsoftonline.com/` |
| `AzureAd__TenantId` | Your Microsoft Entra tenant ID | Yes | - |
| `AzureAd__ClientId` | Application (client) ID | Yes | - |
| `AzureAd__Audience` | Expected audience in incoming tokens | No | `api://{ClientId}` |
| `AzureAd__Scopes` | Required scopes for incoming tokens (space-separated) | No | - |

### Client Credentials Configuration

The sidecar supports multiple credential types with priority-based selection:

#### Client Secret

```yaml
- name: AzureAd__ClientCredentials__0__SourceType
  value: "ClientSecret"
- name: AzureAd__ClientCredentials__0__ClientSecret
  value: "<your-client-secret>"
```

#### Certificate from Key Vault

```yaml
- name: AzureAd__ClientCredentials__0__SourceType
  value: "KeyVault"
- name: AzureAd__ClientCredentials__0__KeyVaultUrl
  value: "https://<your-keyvault>.vault.azure.net"
- name: AzureAd__ClientCredentials__0__KeyVaultCertificateName
  value: "<certificate-name>"
```

#### Certificate from File

```yaml
- name: AzureAd__ClientCredentials__0__SourceType
  value: "Path"
- name: AzureAd__ClientCredentials__0__CertificateDiskPath
  value: "/path/to/certificate.pfx"
- name: AzureAd__ClientCredentials__0__CertificatePassword
  value: "<certificate-password>"  # Optional
```

#### Certificate from Store

```yaml
- name: AzureAd__ClientCredentials__0__SourceType
  value: "StoreWithThumbprint"
- name: AzureAd__ClientCredentials__0__CertificateStorePath
  value: "CurrentUser/My"
- name: AzureAd__ClientCredentials__0__CertificateThumbprint
  value: "<thumbprint>"
```

#### Signed Assertion

```yaml
- name: AzureAd__ClientCredentials__0__SourceType
  value: "SignedAssertionFromManagedIdentity"
- name: AzureAd__ClientCredentials__0__ManagedIdentityClientId
  value: "<managed-identity-client-id>"
```

#### Workload Identity (Recommended for AKS)

```yaml
# No credentials configuration needed
# Uses Azure AD Workload Identity via service account
```

### Credentials Priority

When multiple credentials are configured (indexed 0, 1, 2, etc.), the sidecar attempts them in order until one succeeds:

```yaml
# First priority - Key Vault certificate
- name: AzureAd__ClientCredentials__0__SourceType
  value: "KeyVault"
- name: AzureAd__ClientCredentials__0__KeyVaultUrl
  value: "https://prod-keyvault.vault.azure.net"
- name: AzureAd__ClientCredentials__0__KeyVaultCertificateName
  value: "prod-cert"

# Second priority - Client secret (fallback)
- name: AzureAd__ClientCredentials__1__SourceType
  value: "ClientSecret"
- name: AzureAd__ClientCredentials__1__ClientSecret
  valueFrom:
    secretKeyRef:
      name: app-secrets
      key: client-secret
```

### Downstream APIs Configuration

Configure APIs that your application will call:

```yaml
- name: DownstreamApis__Graph__BaseUrl
  value: "https://graph.microsoft.com/v1.0"
- name: DownstreamApis__Graph__Scopes
  value: "User.Read Mail.Read"

- name: DownstreamApis__MyApi__BaseUrl
  value: "https://api.contoso.com"
- name: DownstreamApis__MyApi__Scopes
  value: "api://myapi/.default"
```

**Per-API Configuration:**

| Key Pattern | Description | Required |
|-------------|-------------|----------|
| `DownstreamApis__<Name>__BaseUrl` | Base URL of the API | Yes |
| `DownstreamApis__<Name>__Scopes` | Space-separated scopes to request | Yes |
| `DownstreamApis__<Name>__HttpMethod` | Default HTTP method | No (GET) |
| `DownstreamApis__<Name>__RelativePath` | Default relative path | No |
| `DownstreamApis__<Name>__RequestAppToken` | Use app token instead of OBO | No (false) |

### Token Acquisition Options

Fine-tune token acquisition behavior:

```yaml
- name: DownstreamApis__Graph__AcquireTokenOptions__Tenant
  value: "<specific-tenant-id>"  # Override tenant for multi-tenant scenarios

- name: DownstreamApis__Graph__AcquireTokenOptions__AuthenticationScheme
  value: "Bearer"

- name: DownstreamApis__Graph__AcquireTokenOptions__CorrelationId
  value: "<correlation-id>"  # For request tracing
```

### Signed HTTP Request (SHR) Configuration

Enable Signed HTTP Requests for enhanced security:

```yaml
- name: DownstreamApis__SecureApi__AcquireTokenOptions__PopPublicKey
  value: "<base64-encoded-public-key>"

- name: DownstreamApis__SecureApi__AcquireTokenOptions__PopClaims
  value: '{"custom_claim": "value"}'
```

**SHR Keys:**

| Key Pattern | Description |
|-------------|-------------|
| `AcquireTokenOptions__PopPublicKey` | Public key for PoP token (base64 encoded) |
| `AcquireTokenOptions__PopClaims` | Additional claims for PoP token (JSON) |

> **Note**: The configuration uses `PopPublicKey` as the external-facing terminology for Signed HTTP Requests, avoiding internal "AT-POP" naming.

### Token Cache Configuration

Control token caching behavior:

```yaml
- name: TokenCache__EnableDistributedCache
  value: "false"  # Use in-memory cache (default)

# For distributed cache (Redis)
- name: TokenCache__EnableDistributedCache
  value: "true"
- name: TokenCache__RedisConnection
  value: "<redis-connection-string>"
```

### Logging Configuration

Configure logging levels and outputs:

```yaml
- name: Logging__LogLevel__Default
  value: "Information"
- name: Logging__LogLevel__Microsoft.Identity.Web
  value: "Debug"
- name: Logging__LogLevel__Microsoft.AspNetCore
  value: "Warning"

# Application Insights (optional)
- name: ApplicationInsights__ConnectionString
  value: "<app-insights-connection-string>"
```

### ASP.NET Core Settings

```yaml
- name: ASPNETCORE_ENVIRONMENT
  value: "Production"  # or "Development"
- name: ASPNETCORE_URLS
  value: "http://+:5000"
```

## Configuration Overrides

### Per-Request Overrides

All token acquisition endpoints accept query parameters prefixed with `optionsOverride.` to override configuration at request time:

**Examples:**

```bash
# Override scopes
GET /AuthorizationHeader/Graph?optionsOverride.Scopes=User.Read&optionsOverride.Scopes=Mail.Read

# Request application token instead of OBO
GET /AuthorizationHeader/Graph?optionsOverride.RequestAppToken=true

# Override tenant for multi-tenant scenarios
GET /AuthorizationHeader/Graph?optionsOverride.AcquireTokenOptions.Tenant=<tenant-id>

# Override relative path
GET /DownstreamApi/Graph?optionsOverride.RelativePath=me/messages

# Enable SHR for this request
GET /AuthorizationHeader/Graph?optionsOverride.AcquireTokenOptions.PopPublicKey=<base64-key>
```

### Agent Identity Overrides

Specify agent identity parameters at request time:

```bash
# Autonomous agent (application context)
GET /AuthorizationHeader/Graph?AgentIdentity=<agent-client-id>

# Delegated agent with username
GET /AuthorizationHeader/Graph?AgentIdentity=<agent-client-id>&AgentUsername=user@contoso.com

# Delegated agent with user object ID
GET /AuthorizationHeader/Graph?AgentIdentity=<agent-client-id>&AgentUserId=<user-object-id>
```

**Important Rules:**
- `AgentUsername` and `AgentUserId` **require** `AgentIdentity` to be specified
- `AgentUsername` and `AgentUserId` are **mutually exclusive**
- `AgentIdentity` alone acquires an application token for the agent (autonomous mode)
- `AgentIdentity` with `AgentUsername` or `AgentUserId` acquires a user token for the agent identity (delegated mode)

See [Agent Identities](agent-identities.md) for detailed semantics.

## Complete Configuration Example

### Kubernetes ConfigMap

```yaml
apiVersion: v1
kind: ConfigMap
metadata:
  name: sidecar-config
data:
  ASPNETCORE_ENVIRONMENT: "Production"
  ASPNETCORE_URLS: "http://+:5000"
  
  AzureAd__Instance: "https://login.microsoftonline.com/"
  AzureAd__TenantId: "common"
  AzureAd__ClientId: "your-app-client-id"
  AzureAd__Scopes: "access_as_user"
  
  DownstreamApis__Graph__BaseUrl: "https://graph.microsoft.com/v1.0"
  DownstreamApis__Graph__Scopes: "User.Read Mail.Read"
  
  DownstreamApis__MyApi__BaseUrl: "https://api.contoso.com"
  DownstreamApis__MyApi__Scopes: "api://myapi/.default"
  DownstreamApis__MyApi__RequestAppToken: "false"
  
  Logging__LogLevel__Default: "Information"
  Logging__LogLevel__Microsoft.Identity.Web: "Debug"
```

### Kubernetes Secret

```yaml
apiVersion: v1
kind: Secret
metadata:
  name: sidecar-secrets
type: Opaque
stringData:
  AzureAd__ClientCredentials__0__ClientSecret: "your-client-secret"
```

### Deployment with ConfigMap and Secret

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: myapp
spec:
  template:
    spec:
      containers:
      - name: sidecar
        image: mcr.microsoft.com/identity/sidecar:latest
        envFrom:
        - configMapRef:
            name: sidecar-config
        - secretRef:
            name: sidecar-secrets
```

## Environment-Specific Configuration

### Development

```yaml
- name: ASPNETCORE_ENVIRONMENT
  value: "Development"
- name: Logging__LogLevel__Default
  value: "Debug"
- name: AzureAd__TenantId
  value: "<dev-tenant-id>"
```

### Staging

```yaml
- name: ASPNETCORE_ENVIRONMENT
  value: "Staging"
- name: Logging__LogLevel__Default
  value: "Information"
- name: AzureAd__TenantId
  value: "<staging-tenant-id>"
```

### Production

```yaml
- name: ASPNETCORE_ENVIRONMENT
  value: "Production"
- name: Logging__LogLevel__Default
  value: "Warning"
- name: Logging__LogLevel__Microsoft.Identity.Web
  value: "Information"
- name: AzureAd__TenantId
  value: "<prod-tenant-id>"
- name: ApplicationInsights__ConnectionString
  value: "<app-insights-connection>"
```

## Validation

The sidecar validates configuration at startup and logs errors for:
- Missing required settings (`TenantId`, `ClientId`)
- Invalid credential configurations
- Malformed downstream API definitions
- Invalid URLs or scope formats

Check container logs for validation messages:

```bash
kubectl logs <pod-name> -c sidecar
```

## Best Practices

1. **Use Secrets for Credentials**: Store client secrets and certificates in Kubernetes Secrets or Azure Key Vault
2. **Separate Configuration per Environment**: Use ConfigMaps to manage environment-specific settings
3. **Enable Appropriate Logging**: Use Debug logging in development, Information/Warning in production
4. **Configure Health Checks**: Ensure health check endpoints are properly configured
5. **Use Managed Identity**: Prefer Azure AD Workload Identity over client secrets when possible
6. **Validate at Deploy Time**: Test configuration in staging before production deployment

## Next Steps

- [Agent Identities](agent-identities.md) - Understand agent identity patterns
- [Endpoints Reference](endpoints.md) - Explore HTTP API endpoints
- [Security Best Practices](security.md) - Secure your configuration
- [Troubleshooting](troubleshooting.md) - Resolve configuration issues
