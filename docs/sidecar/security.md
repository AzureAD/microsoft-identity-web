# Security Best Practices

This document provides comprehensive security guidance for deploying and operating the Microsoft Entra Identity Sidecar.

## Security Overview

The sidecar handles sensitive operations including token acquisition, storage, and validation. Proper security configuration is critical to protect:

- User and application credentials
- Access tokens and refresh tokens
- API secrets and certificates
- User identity information

## Network Security

### Restrict Sidecar Access

**The sidecar should only be accessible from your application container.**

#### Configure Kestrel to Listen on Localhost

Configure the sidecar to listen only on localhost (127.0.0.1) to ensure it's not accessible from outside the pod:

```yaml
containers:
- name: sidecar
  image: mcr.microsoft.com/entra-sdk/auth-sidecar:1.0.0
  env:
  - name: Kestrel__Endpoints__Http__Url
    value: "http://127.0.0.1:5000"
```

Alternatively, use Kestrel's host filtering with AllowedHosts to restrict access:

```yaml
containers:
- name: sidecar
  image: mcr.microsoft.com/entra-sdk/auth-sidecar:1.0.0
  env:
  - name: AllowedHosts
    value: "localhost;127.0.0.1"
```

### Use Localhost Communication

Configure your application to communicate with the sidecar via localhost:

```yaml
containers:
- name: app
  env:
  - name: SIDECAR_URL
    value: "http://localhost:5000"  # Pod-local communication only
```

**Never expose the sidecar externally:**

```yaml
# ❌ DO NOT DO THIS
apiVersion: v1
kind: Service
metadata:
  name: sidecar-service
spec:
  type: LoadBalancer  # Exposes sidecar publicly - INSECURE
  selector:
    app: myapp
  ports:
  - port: 5000
```

## Credential Management

### Use Managed Identity (Preferred)

The most secure approach is Azure AD Workload Identity:

```yaml
apiVersion: v1
kind: ServiceAccount
metadata:
  name: myapp-sa
  annotations:
    azure.workload.identity/client-id: "<managed-identity-client-id>"

---
apiVersion: apps/v1
kind: Deployment
metadata:
  name: myapp
spec:
  template:
    metadata:
      labels:
        azure.workload.identity/use: "true"
    spec:
      serviceAccountName: myapp-sa
      containers:
      - name: sidecar
        image: mcr.microsoft.com/entra-sdk/auth-sidecar:1.0.0
        env:
        - name: AzureAd__ClientId
          value: "<managed-identity-client-id>"
        # No secrets needed - uses workload identity
```

**Benefits:**
- No secrets to store or rotate
- Automatic credential management
- Azure RBAC integration
- Audit trail in Azure AD

### Certificate-Based Authentication

Prefer certificates over client secrets:

#### Azure Key Vault (Recommended)

```yaml
- name: AzureAd__ClientCredentials__0__SourceType
  value: "KeyVault"
- name: AzureAd__ClientCredentials__0__KeyVaultUrl
  value: "https://your-keyvault.vault.azure.net"
- name: AzureAd__ClientCredentials__0__KeyVaultCertificateName
  value: "your-cert-name"
```

**Benefits:**
- Centralized certificate management
- Access policies and auditing
- Automatic rotation support
- No certificate in container image

#### Certificate from Kubernetes Secret

```yaml
apiVersion: v1
kind: Secret
metadata:
  name: app-cert
type: Opaque
data:
  certificate.pfx: <base64-encoded-pfx>
  certificate.password: <base64-encoded-password>

---
containers:
- name: sidecar
  volumeMounts:
  - name: cert-volume
    mountPath: /certs
    readOnly: true
  env:
  - name: AzureAd__ClientCredentials__0__SourceType
    value: "Path"
  - name: AzureAd__ClientCredentials__0__CertificateDiskPath
    value: "/certs/certificate.pfx"
  - name: AzureAd__ClientCredentials__0__CertificatePassword
    valueFrom:
      secretKeyRef:
        name: app-cert
        key: certificate.password

volumes:
- name: cert-volume
  secret:
    secretName: app-cert
    items:
    - key: certificate.pfx
      path: certificate.pfx
    defaultMode: 0400  # Read-only for owner
```

### Client Secrets (Avoid if possible)

If client secrets must be used:

```yaml
apiVersion: v1
kind: Secret
metadata:
  name: app-secrets
type: Opaque
stringData:
  client-secret: "<your-client-secret>"

---
containers:
- name: sidecar
  env:
  - name: AzureAd__ClientCredentials__0__SourceType
    value: "ClientSecret"
  - name: AzureAd__ClientCredentials__0__ClientSecret
    valueFrom:
      secretKeyRef:
        name: app-secrets
        key: client-secret
```

**Security Requirements:**
- Store in Kubernetes Secrets with encryption at rest enabled
- Rotate regularly (every 90 days recommended)
- Use separate secrets per environment
- Never commit secrets to source control
- Use Azure Key Vault or external secret managers

### Secret Rotation

Implement automated secret rotation:

```yaml
# Example using External Secrets Operator
apiVersion: external-secrets.io/v1beta1
kind: ExternalSecret
metadata:
  name: sidecar-secrets
spec:
  refreshInterval: 1h
  secretStoreRef:
    name: azure-keyvault
    kind: SecretStore
  target:
    name: sidecar-secrets
  data:
  - secretKey: client-secret
    remoteRef:
      key: app-client-secret
```

## Signed HTTP Requests (SHR)

SHR provides additional security by cryptographically binding tokens to HTTP requests.

### When to Use SHR

Use SHR for:
- High-security scenarios requiring proof-of-possession
- APIs that accept PoP tokens
- Protection against token theft and replay attacks

### Configuring SHR

#### Generate Key Pair

```bash
# Generate RSA key pair
openssl genrsa -out private.pem 2048
openssl rsa -in private.pem -pubout -out public.pem

# Base64 encode public key for configuration (Linux/macOS)
base64 -w 0 public.pem > public.pem.b64

# Base64 encode public key for configuration (Windows PowerShell)
[Convert]::ToBase64String([System.IO.File]::ReadAllBytes("public.pem")) | Out-File -Encoding ASCII public.pem.b64
```

#### Configure SHR in Kubernetes

```yaml
apiVersion: v1
kind: Secret
metadata:
  name: shr-keys
type: Opaque
data:
  public-key: <base64-encoded-public-key>
  private-key: <base64-encoded-private-key>

---
containers:
- name: sidecar
  env:
  - name: DownstreamApis__SecureApi__AcquireTokenOptions__PopPublicKey
    valueFrom:
      secretKeyRef:
        name: shr-keys
        key: public-key
```

#### Per-Request SHR

```http
GET /AuthorizationHeader/Graph?optionsOverride.AcquireTokenOptions.PopPublicKey=<base64-key> HTTP/1.1
Authorization: Bearer eyJ0eXAiOiJKV1QiLCJhbGc...
```

### SHR Best Practices

1. **Protect Private Keys**: Store private keys securely and never expose them
2. **Rotate Keys Regularly**: Implement key rotation policies
3. **Validate PoP Tokens**: Ensure downstream APIs properly validate PoP tokens
4. **Use Per-API Keys**: Different keys for different APIs improve isolation
5. **Monitor Key Usage**: Audit and monitor SHR key usage

## Token Security

### Token Caching

The sidecar caches tokens in memory by default:

```yaml
# In-memory cache (default)
- name: TokenCache__EnableDistributedCache
  value: "false"
```

For distributed scenarios with multiple replicas:

```yaml
# Distributed cache (Redis)
- name: TokenCache__EnableDistributedCache
  value: "true"
- name: TokenCache__RedisConnection
  value: "redis-host:6379,password=<redis-password>,ssl=true"
```

**Cache Security:**
- Encrypt Redis connection (use SSL/TLS)
- Use Redis authentication
- Configure Redis network policies
- Set appropriate token cache expiration
- Monitor cache access patterns

### Token Validation

Configure strict token validation:

```yaml
- name: AzureAd__Audience
  value: "api://<your-api-id>"  # Validate expected audience

- name: AzureAd__Scopes
  value: "access_as_user"  # Enforce required scopes
```

## Container Security

### Run as Non-Root User

Configure the sidecar to run as a non-root user:

```yaml
containers:
- name: sidecar
  image: mcr.microsoft.com/entra-sdk/auth-sidecar:1.0.0
  securityContext:
    runAsNonRoot: true
    runAsUser: 1000
    runAsGroup: 3000
    allowPrivilegeEscalation: false
    capabilities:
      drop:
      - ALL
    readOnlyRootFilesystem: true
    seccompProfile:
      type: RuntimeDefault
```

### Pod Security Standards

Apply Pod Security Standards:

```yaml
apiVersion: v1
kind: Namespace
metadata:
  name: myapp-namespace
  labels:
    pod-security.kubernetes.io/enforce: restricted
    pod-security.kubernetes.io/audit: restricted
    pod-security.kubernetes.io/warn: restricted
```

### Image Security

1. **Use Official Images**: Only use images from `mcr.microsoft.com/identity/sidecar`
2. **Pin Versions**: Use specific version tags, not `latest`
3. **Scan Images**: Regularly scan for vulnerabilities
4. **Verify Signatures**: Validate image signatures before deployment

```yaml
containers:
- name: sidecar
  image: mcr.microsoft.com/identity/sidecar:1.0.0  # Specific version
  imagePullPolicy: Always
```

## Monitoring and Auditing

### Enable Comprehensive Logging

```yaml
- name: Logging__LogLevel__Default
  value: "Information"
- name: Logging__LogLevel__Microsoft.Identity.Web
  value: "Information"

# Avoid Debug in production (may log sensitive data)
# - name: Logging__LogLevel__Microsoft.Identity.Web
#   value: "Debug"  # ❌ Not for production
```

### Application Insights Integration

```yaml
- name: ApplicationInsights__ConnectionString
  valueFrom:
    secretKeyRef:
      name: monitoring-secrets
      key: appinsights-connection
```

### Audit Token Acquisition

Log and monitor:
- Token acquisition requests
- Failed authentication attempts
- Agent identity usage
- Unusual access patterns
- Token acquisition latency

### Security Alerts

Configure alerts for:
- Repeated authentication failures
- Token acquisition errors
- Unusual API access patterns
- Network policy violations
- Certificate expiration warnings

## Resource Limits

Set appropriate resource limits to prevent resource exhaustion attacks:

```yaml
containers:
- name: sidecar
  resources:
    requests:
      memory: "128Mi"
      cpu: "100m"
    limits:
      memory: "256Mi"
      cpu: "250m"
```

## Agent Identity Security

### Validate Agent Parameters

Don't use unvalidated user input for any of the options to the container API. Implement validation for agent identity parameters:

```typescript
function validateAgentParams(
  agentIdentity?: string,
  agentUsername?: string,
  agentUserId?: string
): void {
  // AgentUsername/AgentUserId require AgentIdentity
  if ((agentUsername || agentUserId) && !agentIdentity) {
    throw new Error("AgentUsername or AgentUserId require AgentIdentity");
  }
  
  // AgentUsername and AgentUserId are mutually exclusive
  if (agentUsername && agentUserId) {
    throw new Error("AgentUsername and AgentUserId are mutually exclusive");
  }
  
  // Validate GUID format for AgentUserId
  if (agentUserId && !isValidGuid(agentUserId)) {
    throw new Error("AgentUserId must be a valid GUID");
  }
}
```

### Protect Agent Credentials

- Store agent identity client IDs securely
- Rotate agent identity credentials regularly
- Monitor agent identity token acquisition
- Audit agent operations for compliance

### Least Privilege for Agents

Grant minimum required permissions:

```bash
# Grant only specific permissions to agent identity
az ad app permission add \
  --id $AGENT_IDENTITY_ID \
  --api 00000003-0000-0000-c000-000000000000 \
  --api-permissions e1fe6dd8-ba31-4d61-89e7-88639da4683d=Scope  # Only User.Read

# Avoid overly broad permissions
# ❌ Don't: --api-permissions 00000000-0000-0000-0000-000000000000=Role  # All permissions
```

## Compliance and Governance

### Data Residency

Ensure compliance with data residency requirements:

```yaml
# Use regional endpoints if required
- name: AzureAd__Instance
  value: "https://login.microsoftonline.de/"  # Germany
```

### Regulatory Compliance

- **GDPR**: Implement data minimization and retention policies
- **HIPAA**: Use appropriate Azure compliance offerings
- **SOC 2**: Enable audit logging and monitoring
- **PCI DSS**: Follow payment card data handling guidelines

### Conditional Access Policies

Configure Microsoft Entra ID Conditional Access:

1. Require MFA for token acquisition
2. Limit access to specific networks/locations
3. Enforce device compliance
4. Require approved client applications

## Incident Response

### Compromised Credentials

If credentials are compromised:

1. **Immediately Revoke**:
   ```bash
   # Revoke application credentials
   az ad app credential reset --id $APP_ID
   ```

2. **Rotate Secrets**:
   - Generate new client secret or certificate
   - Update Kubernetes Secrets
   - Redeploy sidecar containers

3. **Audit Access**:
   - Review Microsoft Entra ID sign-in logs
   - Check for unauthorized API access
   - Identify affected resources

4. **Notify Stakeholders**:
   - Inform security team
   - Document incident
   - Follow incident response procedures

### Token Exposure

If access tokens are exposed:

1. **Revoke User Sessions**: Force sign-out in Microsoft Entra ID
2. **Review Access Logs**: Identify unauthorized access
3. **Rotate Credentials**: Update all application credentials
4. **Enable MFA**: Require multi-factor authentication

## Security Checklist

- [ ] Restrict sidecar network access to localhost/pod-internal only
- [ ] Use Kubernetes Network Policies
- [ ] Implement managed identity (Azure AD Workload Identity)
- [ ] Store credentials in Azure Key Vault or Kubernetes Secrets
- [ ] Enable encryption at rest for Kubernetes Secrets
- [ ] Use certificate-based authentication over client secrets
- [ ] Rotate credentials regularly (90 days recommended)
- [ ] Configure strict token validation (audience, scopes)
- [ ] Run containers as non-root user
- [ ] Apply Pod Security Standards (restricted)
- [ ] Use specific image versions (not `latest`)
- [ ] Set resource limits to prevent exhaustion
- [ ] Enable comprehensive logging (but avoid Debug in production)
- [ ] Configure Application Insights or monitoring
- [ ] Set up security alerts for anomalies
- [  ]  Always validate user input for any of the options to the container API
- [ ] Apply least privilege to agent identities
- [ ] Configure Conditional Access policies
- [ ] Document incident response procedures
- [ ] Regular security audits and reviews

## Next Steps

- [Configuration Reference](configuration.md) - Configure secure settings
- [Agent Identities](agent-identities.md) - Secure agent operations
- [Troubleshooting](troubleshooting.md) - Diagnose security issues
- [Installation Guide](installation.md) - Deploy securely
