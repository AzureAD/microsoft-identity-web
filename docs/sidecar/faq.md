# Frequently Asked Questions (FAQ)

Common questions and answers about the Microsoft Entra Identity Sidecar.

## General Questions

### What is the Microsoft Entra Identity Sidecar?

The Microsoft Entra Identity Sidecar is a containerized service that provides token acquisition and validation capabilities for applications in any language. It runs as a separate container alongside your application and exposes HTTP endpoints for authentication operations.

### Why would I use the sidecar instead of Microsoft.Identity.Web?

Use the sidecar when:
- Building microservices in multiple languages (Node.js, Python, Go, Java, etc.)
- Deploying in Kubernetes or container environments
- Wanting centralized authentication configuration across services
- Building applications in languages without Microsoft Identity libraries

Use Microsoft.Identity.Web when:
- Building ASP.NET Core applications exclusively
- Performance is critical (in-process is faster)
- Wanting deep integration with .NET features

See [Comparison with Microsoft.Identity.Web](comparison.md) for detailed guidance.

### Is the sidecar production-ready?

The sidecar is currently in preview. Check the [GitHub repository](https://github.com/AzureAD/microsoft-identity-web) for the latest release status and production readiness guidelines.

### What container images are available?

The sidecar is distributed from Microsoft Container Registry:
```
mcr.microsoft.com/identity/sidecar:<tag>
```

Available tags:
- `latest` - Latest stable release
- `<version>` - Specific versions (e.g., `1.0.0`)
- `<version>-preview` - Preview releases

### Can I run the sidecar outside Kubernetes?

Yes! While designed for Kubernetes, the sidecar can run in any container environment:
- Docker Compose
- Azure Container Instances
- AWS ECS/Fargate
- Standalone Docker

See [Installation Guide](installation.md) for examples.

## Agent Identities

### What are agent identities?

Agent identities enable sophisticated authentication scenarios where an agent application operates either:
- **Autonomously** - in its own application context
- **Delegated** - on behalf of the user that called the agent web API.

See [Agent Identities](agent-identities.md) for comprehensive documentation.

### When should I use autonomous agent mode?

Use autonomous agent mode (`AgentIdentity` only) for:
- Batch processing without user context
- Background tasks
- System-to-system operations
- Scheduled jobs

Example:
```bash
GET /AuthorizationHeader/Graph?AgentIdentity=<agent-client-id>
```

### When should I use delegated agent mode?

Use delegated agent mode (`AgentIdentity` + `AgentUsername`/`AgentUserId`) for:
- Interactive agent applications
- AI assistants acting on behalf of users
- User-scoped automation
- Personalized workflows

Example:
```bash
GET /AuthorizationHeader/Graph?AgentIdentity=<agent-client-id>&AgentUsername=user@contoso.com
```

### Why can't I use AgentUsername without AgentIdentity?

`AgentUsername` and `AgentUserId` are modifiers for agent identity behavior. They require `AgentIdentity` to specify which agent identity to use. Without `AgentIdentity`, the sidecar doesn't know which agent context to operate in.

### Why are AgentUsername and AgentUserId mutually exclusive?

They represent two different ways to identify the same user:
- `AgentUsername` - User Principal Name (UPN) like `user@contoso.com`
- `AgentUserId` - Object ID (OID) like `12345678-1234-1234-1234-123456789012`

Allowing both could create ambiguity. Choose the identifier that best fits your scenario:
- Use `AgentUsername` when you have the user's UPN
- Use `AgentUserId` when you have the user's object ID

### Should I use AgentUsername or AgentUserId?

**Use `AgentUsername` when:**
- You have the user's UPN (email-like identifier)
- Building user-facing features where UPN is natural
- Users may have multiple object IDs (guest scenarios)

**Use `AgentUserId` when:**
- You have the user's object ID
- Object ID is your primary user identifier
- Building systems that store user identifiers
- Need consistent identification across tenants

Object IDs are generally more stable and recommended for long-running processes.

### How do I configure agent identities in Microsoft Entra ID?

See the [Agent Identities configuration section](agent-identities.md#microsoft-entra-id-configuration) for step-by-step instructions on:
- Creating agent identity app registrations
- Configuring Federated Identity Credentials (FIC)
- Assigning permissions
- Granting admin consent

## Configuration

### How do I store secrets securely?

Recommended approaches (in order of preference):

1. **Azure AD Workload Identity** (no secrets):
   ```yaml
   serviceAccountName: myapp-sa  # Configured with workload identity
   ```

2. **Azure Key Vault**:
   ```yaml
   - name: AzureAd__ClientCredentials__0__SourceType
     value: "KeyVault"
   - name: AzureAd__ClientCredentials__0__KeyVaultUrl
     value: "https://your-kv.vault.azure.net"
   ```

3. **Kubernetes Secrets**:
   ```yaml
   - name: AzureAd__ClientCredentials__0__ClientSecret
     valueFrom:
       secretKeyRef:
         name: app-secrets
         key: client-secret
   ```

See [Security Best Practices](security.md) for comprehensive guidance.

### Can I configure multiple downstream APIs?

Yes! Configure multiple APIs in your sidecar:

```yaml
env:
# Microsoft Graph
- name: DownstreamApis__Graph__BaseUrl
  value: "https://graph.microsoft.com/v1.0"
- name: DownstreamApis__Graph__Scopes
  value: "User.Read Mail.Read"

# Custom API
- name: DownstreamApis__MyApi__BaseUrl
  value: "https://api.contoso.com"
- name: DownstreamApis__MyApi__Scopes
  value: "api://myapi/.default"
```

Access them using their configured names:
```bash
GET /AuthorizationHeader/Graph
GET /AuthorizationHeader/MyApi
```

### Can I override configuration at request time?

Yes! Use `optionsOverride.*` query parameters:

```bash
# Override scopes
GET /AuthorizationHeader/Graph?optionsOverride.Scopes=User.Read

# Override tenant (multi-tenant scenarios)
GET /AuthorizationHeader/Graph?optionsOverride.AcquireTokenOptions.Tenant=<tenant-id>

# Request application token
GET /AuthorizationHeader/Graph?optionsOverride.RequestAppToken=true
```

See [Configuration Reference](configuration.md#configuration-overrides) for all override options.

### How do I configure for multi-tenant applications?

Set `TenantId` to `common`, `organizations`, or `consumers` in the configuration:

```yaml
env:
- name: AzureAd__TenantId
  value: "common"  # Allow any tenant
```

Override tenant at request time for specific users:

```bash
GET /AuthorizationHeader/Graph?optionsOverride.AcquireTokenOptions.Tenant=<user-tenant-id>
```

## Signed HTTP Requests (SHR)

### What are Signed HTTP Requests?

Signed HTTP Requests (SHR) provide proof-of-possession by cryptographically binding tokens to specific HTTP requests. This prevents token theft and replay attacks.

The sidecar uses `PopPublicKey` as the external-facing configuration parameter for SHR, avoiding internal "AT-POP" terminology.

### When should I use SHR?

Use SHR for:
- High-security scenarios requiring proof-of-possession
- APIs that specifically require PoP tokens
- Protection against token theft
- Compliance requirements for token binding

### How do I enable SHR?

Configure in downstream API settings:

```yaml
env:
- name: DownstreamApis__SecureApi__AcquireTokenOptions__PopPublicKey
  value: "<base64-encoded-public-key>"
```

Or override at request time:

```bash
GET /AuthorizationHeader/SecureApi?optionsOverride.AcquireTokenOptions.PopPublicKey=<base64-key>
```

See [Security Best Practices](security.md#signed-http-requests-shr) for detailed guidance.

### How do I generate keys for SHR?

**Linux/macOS (OpenSSL)**:
```bash
# Generate RSA key pair
openssl genrsa -out private.pem 2048
openssl rsa -in private.pem -pubout -out public.pem

# Base64 encode public key
base64 -w 0 public.pem > public.pem.b64
```

**Windows (PowerShell)**:
```powershell
# Generate RSA key pair
$rsa = [System.Security.Cryptography.RSA]::Create(2048)

# Export private key
$privateKey = $rsa.ExportRSAPrivateKeyPem()
Set-Content -Path "private.pem" -Value $privateKey

# Export public key
$publicKey = $rsa.ExportRSAPublicKeyPem()
Set-Content -Path "public.pem" -Value $publicKey

# Base64 encode public key
$publicKeyBytes = [System.Text.Encoding]::UTF8.GetBytes($publicKey)
$base64PublicKey = [Convert]::ToBase64String($publicKeyBytes)
Set-Content -Path "public.pem.b64" -Value $base64PublicKey
```

Store the base64-encoded public key in configuration and keep the private key secure for signing requests.

## Token Caching

### How does token caching work?

The sidecar automatically caches tokens in memory by default. When you request a token for the same resource and scopes, the cached token is returned if still valid.

### Can I use distributed cache?

Yes! Configure Redis for distributed caching across multiple sidecar instances:

```yaml
env:
- name: TokenCache__EnableDistributedCache
  value: "true"
- name: TokenCache__RedisConnection
  value: "redis-host:6379,password=<password>,ssl=true"
```

### When should I use distributed cache?

Use distributed cache when:
- Running multiple replicas of your application
- Wanting consistent token caching across instances
- Optimizing for token acquisition performance
- Reducing calls to Microsoft Entra ID

Use in-memory cache when:
- Running single instance
- Simplicity is important
- No need for cross-instance caching

### How long are tokens cached?

Tokens are cached based on their expiration time from Microsoft Entra ID:
- Access tokens: Typically 1 hour
- Refresh tokens: Typically 24 hours (sliding window)

The sidecar automatically refreshes tokens before expiration.

## Deployment

### What are the minimum resource requirements?

**Minimum (development)**:
- Memory: 128Mi
- CPU: 100m

**Recommended (production)**:
- Memory: 256Mi
- CPU: 250m

**High traffic**:
- Memory: 512Mi
- CPU: 500m

Adjust based on your token acquisition frequency and API call patterns.

### How do I scale the sidecar?

The sidecar scales with your application pods. Each application pod should have its own sidecar container:

```yaml
spec:
  replicas: 3  # Creates 3 pods, each with app + sidecar
  template:
    spec:
      containers:
      - name: app
        # ...
      - name: sidecar
        # ...
```

### Can I run one sidecar for multiple application instances?

**Not recommended.** The sidecar should run as a true sidecar - one per application pod. This provides:
- Isolation and security
- Simpler networking (localhost)
- Independent scaling
- Fault isolation

### Should the sidecar be externally accessible?

**No!** The sidecar should only be accessible from your application container via localhost. Exposing it externally would allow anyone to acquire tokens using your application's identity.

Use Kubernetes Network Policies to enforce this restriction. See [Security Best Practices](security.md#network-security).

## Authentication Flows

### Does the sidecar support On-Behalf-Of (OBO) flow?

Yes! The sidecar automatically uses OBO flow when:
- You provide an incoming user token
- Request a user token for a downstream API (that is `RequestAppToken` is false)
- `RequestAppToken` is not set to true

Example:
```bash
GET /AuthorizationHeader/Graph
Authorization: Bearer <user-token>
```

### Does the sidecar support client credentials flow?

Yes! Request application tokens using:

```bash
# Via configuration
GET /AuthorizationHeader/Graph?optionsOverride.RequestAppToken=true

# Or configure API for app tokens
env:
- name: DownstreamApis__MyApi__RequestAppToken
  value: "true"
```

### Can I use managed identity?

Yes! Configure Azure AD Workload Identity in AKS:

```yaml
serviceAccountName: myapp-sa  # Configured with workload identity
annotations:
  azure.workload.identity/client-id: "<managed-identity-client-id>"
```

See [Installation Guide](installation.md#azure-kubernetes-service-aks-with-managed-identity) for complete setup.

### Does the sidecar support B2C?

The sidecar supports Microsoft Entra ID and Azure AD B2C. Configure the B2C authority:

```yaml
env:
- name: AzureAd__Instance
  value: "https://<tenant-name>.b2clogin.com/"
- name: AzureAd__TenantId
  value: "<tenant-name>.onmicrosoft.com"
- name: AzureAd__ClientId
  value: "<b2c-client-id>"
```

## Performance

### What is the performance overhead?

The sidecar adds HTTP communication overhead:
- **Latency**: ~1-5ms for localhost communication
- **Cached tokens**: Minimal overhead (direct cache hit)
- **New tokens**: Microsoft Entra ID acquisition time (~100-500ms)

For most applications, this overhead is acceptable. Consider in-process Microsoft.Identity.Web for ultra-low-latency scenarios.

### How can I optimize performance?

1. **Token Caching**: Ensure caching is enabled (default)
2. **Connection Pooling**: Reuse HTTP connections to sidecar
3. **HTTP/2**: Use HTTP/2 for better performance
4. **Resource Allocation**: Provide adequate CPU/memory to sidecar
5. **Distributed Cache**: Use Redis for multi-instance scenarios

### Should I cache authorization headers in my application?

**Yes, carefully!** You can cache authorization headers for the token's lifetime:

```typescript
// Cache with expiration
const cacheKey = `auth-header:${service}:${scopes}`;
const ttl = 3600; // 1 hour (typical token lifetime)

let authHeader = cache.get(cacheKey);
if (!authHeader) {
  authHeader = await getAuthHeaderFromSidecar(service, scopes);
  cache.set(cacheKey, authHeader, ttl);
}
```

**Important**: 
- Use shorter TTL than token lifetime
- Clear cache on auth errors
- Don't cache across users (security risk)

## Troubleshooting

### How do I debug authentication issues?

1. **Check sidecar logs**:
   ```bash
   kubectl logs <pod-name> -c sidecar
   ```

2. **Validate incoming token**:
   ```bash
   curl -H "Authorization: Bearer <token>" \
     http://localhost:5000/Validate
   ```

3. **Enable detailed logging**:
   ```yaml
   - name: Logging__LogLevel__Microsoft.Identity.Web
     value: "Debug"
   ```

See [Troubleshooting Guide](troubleshooting.md) for comprehensive diagnostic steps.

### Why am I getting 400 Bad Request?

Common causes:
- **Missing AgentIdentity**: AgentUsername/AgentUserId require AgentIdentity
- **Mutually exclusive parameters**: Can't use AgentUsername and AgentUserId together
- **Invalid GUID**: AgentUserId must be a valid GUID
- **Missing required parameter**: Check error message for details

See [Troubleshooting - Agent Identity Validation](troubleshooting.md#3-400-bad-request---agent-identity-validation).

### Why is token acquisition slow?

Check:
1. **Token cache hit rate**: Are tokens being cached?
2. **Network latency**: Latency to login.microsoftonline.com
3. **Resource constraints**: Sidecar CPU/memory limits
4. **Credential type**: Certificate auth is faster than secret

### Where can I get help?

- **Documentation**: Read through this comprehensive documentation
- **GitHub Issues**: [microsoft-identity-web/issues](https://github.com/AzureAD/microsoft-identity-web/issues)
- **Microsoft Q&A**: [Azure Active Directory](https://docs.microsoft.com/answers/topics/azure-active-directory.html)
- **Stack Overflow**: Tag `[microsoft-identity-web]`

## Migration

### How do I migrate from Microsoft.Identity.Web to the sidecar?

See [Comparison - Migration Guidance](comparison.md#migration-guidance) for step-by-step instructions including:
- Deploying the sidecar container
- Migrating configuration
- Updating application code
- Removing dependencies
- Testing and validation

### Can I gradually migrate to the sidecar?

Yes! Use a hybrid approach:
- Keep existing ASP.NET Core services using Microsoft.Identity.Web
- Deploy new services or non-.NET services with sidecar
- Migrate existing services incrementally

Both can coexist in the same architecture.

## Security

### Is it safe to run the sidecar?

Yes, when properly configured:
- ✅ Run only in containerized environments
- ✅ Restrict access to localhost/pod-internal only
- ✅ Use Kubernetes Network Policies
- ✅ Store credentials securely (Key Vault, Secrets)
- ✅ Run as non-root user
- ✅ Enable audit logging

See [Security Best Practices](security.md) for comprehensive guidance.

### Should I rotate credentials?

Yes! Rotate credentials regularly:
- **Client secrets**: Every 90 days (recommended)
- **Certificates**: Before expiration (typically 1-2 years)
- **Keys for SHR**: Per security policy

Implement automated rotation using Azure Key Vault or external secret managers.

### What if my credentials are compromised?

Immediate actions:
1. **Revoke compromised credentials** in Microsoft Entra ID
2. **Generate new credentials**
3. **Update Kubernetes Secrets/Key Vault**
4. **Redeploy sidecar containers**
5. **Audit access logs** for unauthorized activity
6. **Document incident** and follow security procedures

See [Security - Incident Response](security.md#incident-response).

## Contributing

### Can I contribute to the sidecar?

Yes! The Microsoft Entra Identity Sidecar is part of the open-source Microsoft.Identity.Web project:
- GitHub: [AzureAD/microsoft-identity-web](https://github.com/AzureAD/microsoft-identity-web)
- Contributions welcome via pull requests
- Report issues on GitHub Issues

### Where is the sidecar source code?

The sidecar source code is in the Microsoft.Identity.Web repository:
```
src/Microsoft.Identity.Web.Sidecar/
```

Review the code, submit issues, and contribute improvements!

## Next Steps

- [Installation Guide](installation.md) - Deploy the sidecar
- [Configuration Reference](configuration.md) - Complete configuration options
- [Agent Identities](agent-identities.md) - Learn about agent patterns
- [Scenarios](scenarios/README.md) - Practical examples
- [Security Best Practices](security.md) - Secure your deployment
- [Troubleshooting](troubleshooting.md) - Resolve common issues
