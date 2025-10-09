# Installation & Runtime Deployment

> Image coordinates placeholder: `mcr.microsoft.com/identity/sidecar:<tag>` (will update when finalized).

## Prerequisites

- Microsoft Entra (Azure AD) app registration (client ID; certificate credential preferred).
- Necessary API permissions granted / consented.
- Container runtime (Docker / Kubernetes).
- Network egress to Microsoft Entra endpoints and target downstream APIs.

## Supported Base Images

| Variant | Dockerfile | Notes |
|---------|------------|-------|
| Linux (default) | `src/Microsoft.Identity.Web.Sidecar/Dockerfile` | General purpose |
| Azure Linux | `src/Microsoft.Identity.Web.Sidecar/Dockerfile.AzureLinux` | Azure-tuned base |
| Windows NanoServer | `src/Microsoft.Identity.Web.Sidecar/DockerFile.NanoServer` | Windows environments |

## Pull the Image

```bash
docker pull mcr.microsoft.com/identity/sidecar:latest
# (Replace with pinned tag/digest once published)
```

## Local (Docker Compose) Example

```yaml
services:
  app:
    build: .
    environment:
      SIDECAR_BASE_URL: http://sidecar:5080
    depends_on: [ sidecar ]

  sidecar:
    image: mcr.microsoft.com/identity/sidecar:latest
    container_name: identity-sidecar
    ports:
      - "5080:5080"
    environment:
      AzureAd__TenantId: <tenant-guid>
      AzureAd__ClientId: <client-id>
      # Prefer certificate (X.509) or managed identity (in Azure) over secrets
      DownstreamApis__Graph__Scopes__0: https://graph.microsoft.com/User.Read
      DownstreamApis__Graph__BaseUrl: https://graph.microsoft.com
      DownstreamApis__Graph__RelativePath: /v1.0/me
      DownstreamApis__Graph__HttpMethod: GET
```

## Kubernetes Sidecar Pattern

```yaml
containers:
- name: app
  image: ghcr.io/contoso/sample-app:1.0
  env:
  - name: SIDECAR_BASE_URL
    value: http://localhost:5080
- name: identity-sidecar
  image: mcr.microsoft.com/identity/sidecar:latest
  ports:
  - containerPort: 5080
  env:
  - name: AzureAd__TenantId
    value: "<tenant-guid>"
  - name: AzureAd__ClientId
    value: "<client-id>"
```

### Credential-less on AKS (Managed / Workload Identity)

- Configure workload identity or pod identity binding.
- Omit explicit `ClientCredentials`.
- Sidecar acquires tokens via the managed identity endpoint / federation.

### Reverse Proxy (Optional)

Use an internal reverse proxy (Envoy, YARP, Nginx) to control access to sidecar endpoints. See [reverse-proxy.md](reverse-proxy.md).

## Health / Validation

Call `GET /Validate` with a known bearer token to test parsing.

## Next Steps

- Configure more downstream APIs: [configuration.md](configuration.md)
- Use endpoints: [endpoints.md](endpoints.md)
- Explore scenarios: `docs/sidecar/scenarios/*`
