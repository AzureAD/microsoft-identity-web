# Installation Guide

This guide covers how to acquire, deploy, and configure the Microsoft Entra Identity Sidecar container.

## Container Image

The sidecar is distributed as a container image from Microsoft Container Registry (MCR):

```
mcr.microsoft.com/entra-sdk/auth-sidecar:<tag>
```

### Version Tags

- `latest` - Latest stable release
- `<version>` - Specific version (e.g., `1.0.0`)
- `<version>-preview` - Preview releases

> **Note**: The container image is currently in preview. Check the [GitHub releases page](https://github.com/AzureAD/microsoft-identity-web/releases) for the latest available tags.

## Deployment Patterns

The sidecar is designed to run as a companion container alongside your application. The most common deployment patterns are:

### Kubernetes Sidecar Pattern

Deploy the sidecar in the same pod as your application container:

```yaml
apiVersion: v1
kind: Pod
metadata:
  name: myapp
spec:
  containers:
  # Your application container
  - name: app
    image: myregistry/myapp:latest
    ports:
    - containerPort: 8080
    env:
    - name: SIDECAR_URL
      value: "http://localhost:5000"
  
  # Sidecar container
  - name: sidecar
    image: mcr.microsoft.com/identity/sidecar:latest
    ports:
    - containerPort: 5000
    env:
    - name: AzureAd__TenantId
      value: "your-tenant-id"
    - name: AzureAd__ClientId
      value: "your-client-id"
    - name: AzureAd__ClientCredentials__0__SourceType
      value: "KeyVault"
    - name: AzureAd__ClientCredentials__0__KeyVaultUrl
      value: "https://your-keyvault.vault.azure.net"
    - name: AzureAd__ClientCredentials__0__KeyVaultCertificateName
      value: "your-cert-name"
```

### Kubernetes Deployment

A more complete deployment example:

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: myapp-deployment
spec:
  replicas: 3
  selector:
    matchLabels:
      app: myapp
  template:
    metadata:
      labels:
        app: myapp
    spec:
      serviceAccountName: myapp-sa
      containers:
      - name: app
        image: myregistry/myapp:latest
        ports:
        - containerPort: 8080
        env:
        - name: SIDECAR_URL
          value: "http://localhost:5000"
        resources:
          requests:
            memory: "256Mi"
            cpu: "250m"
          limits:
            memory: "512Mi"
            cpu: "500m"
      
      - name: sidecar
        image: mcr.microsoft.com/identity/sidecar:latest
        ports:
        - containerPort: 5000
        env:
        - name: AzureAd__TenantId
          valueFrom:
            configMapKeyRef:
              name: app-config
              key: tenant-id
        - name: AzureAd__ClientId
          valueFrom:
            configMapKeyRef:
              name: app-config
              key: client-id
        - name: AzureAd__Instance
          value: "https://login.microsoftonline.com/"
        resources:
          requests:
            memory: "128Mi"
            cpu: "100m"
          limits:
            memory: "256Mi"
            cpu: "250m"
        livenessProbe:
          httpGet:
            path: /health
            port: 5000
          initialDelaySeconds: 10
          periodSeconds: 10
        readinessProbe:
          httpGet:
            path: /health
            port: 5000
          initialDelaySeconds: 5
          periodSeconds: 5
```

### Docker Compose

For local development or Docker-based deployments:

```yaml
version: '3.8'

services:
  app:
    image: myregistry/myapp:latest
    ports:
      - "8080:8080"
    environment:
      - SIDECAR_URL=http://sidecar:5000
    depends_on:
      - sidecar
    networks:
      - app-network

  sidecar:
    image: mcr.microsoft.com/identity/sidecar:latest
    ports:
      - "5000:5000"
    environment:
      - AzureAd__TenantId=${TENANT_ID}
      - AzureAd__ClientId=${CLIENT_ID}
      - AzureAd__ClientCredentials__0__SourceType=ClientSecret
      - AzureAd__ClientCredentials__0__ClientSecret=${CLIENT_SECRET}
    networks:
      - app-network

networks:
  app-network:
    driver: bridge
```

## Azure Kubernetes Service (AKS) with Managed Identity

When deploying to AKS, you can use Azure Managed Identity to authenticate the sidecar without storing credentials in configuration:

### Prerequisites

1. Enable Azure AD Workload Identity on your AKS cluster
2. Create a managed identity and assign it appropriate permissions
3. Create a service account with workload identity federation

### Step 1: Create Managed Identity

```bash
# Create managed identity
az identity create \
  --resource-group myResourceGroup \
  --name myapp-identity

# Get the identity details
IDENTITY_CLIENT_ID=$(az identity show \
  --resource-group myResourceGroup \
  --name myapp-identity \
  --query clientId -o tsv)

IDENTITY_OBJECT_ID=$(az identity show \
  --resource-group myResourceGroup \
  --name myapp-identity \
  --query principalId -o tsv)
```

### Step 2: Assign Permissions

Grant the managed identity permissions to call downstream APIs:

```bash
# Example: Grant permission to call Microsoft Graph
az ad app permission add \
  --id $IDENTITY_CLIENT_ID \
  --api 00000003-0000-0000-c000-000000000000 \
  --api-permissions e1fe6dd8-ba31-4d61-89e7-88639da4683d=Scope
```

### Step 3: Configure Workload Identity

```bash
# Create service account with workload identity
export AKS_OIDC_ISSUER=$(az aks show \
  --resource-group myResourceGroup \
  --name myAKSCluster \
  --query "oidcIssuerProfile.issuerUrl" -o tsv)

az identity federated-credential create \
  --name myapp-federated-identity \
  --identity-name myapp-identity \
  --resource-group myResourceGroup \
  --issuer $AKS_OIDC_ISSUER \
  --subject system:serviceaccount:default:myapp-sa
```

### Step 4: Deploy with Workload Identity

```yaml
apiVersion: v1
kind: ServiceAccount
metadata:
  name: myapp-sa
  namespace: default
  annotations:
    azure.workload.identity/client-id: "<MANAGED_IDENTITY_CLIENT_ID>"

---
apiVersion: apps/v1
kind: Deployment
metadata:
  name: myapp-deployment
spec:
  template:
    metadata:
      labels:
        azure.workload.identity/use: "true"
    spec:
      serviceAccountName: myapp-sa
      containers:
      - name: app
        image: myregistry/myapp:latest
        env:
        - name: SIDECAR_URL
          value: "http://localhost:5000"
      
      - name: sidecar
        image: mcr.microsoft.com/entra-sdk/auth-sidecar:<tag>
        ports:
        - containerPort: 5000
        env:
        - name: AzureAd__TenantId
          value: "your-tenant-id"
        - name: AzureAd__ClientId
          value: "<MANAGED_IDENTITY_CLIENT_ID>"
        # No client secret or certificate needed - uses workload identity
```

### Managed Identity Best Practices

1. **Use Workload Identity**: Prefer Azure AD Workload Identity over pod identity for AKS
2. **Least Privilege**: Grant only the minimum required permissions to the managed identity
3. **Separate Identities**: Use different managed identities for different environments (dev, staging, prod)
4. **Monitor Usage**: Enable diagnostic logging to track managed identity token acquisition

## Network Configuration

### Internal Communication

The sidecar should only be accessible from your application container, typically via localhost when using the sidecar pattern:

```yaml
containers:
- name: app
  env:
  - name: SIDECAR_URL
    value: "http://localhost:5000"  # Same pod, localhost communication
```

### Network Policies

Apply Kubernetes network policies to restrict sidecar access:

```yaml
apiVersion: networking.k8s.io/v1
kind: NetworkPolicy
metadata:
  name: sidecar-network-policy
spec:
  podSelector:
    matchLabels:
      app: myapp
  policyTypes:
  - Ingress
  - Egress
  ingress:
  # No external ingress rules - only pod-local communication
  egress:
  - to:
    - namespaceSelector:
        matchLabels:
          name: kube-system
    ports:
    - protocol: TCP
      port: 53  # DNS
  - to:
    - podSelector: {}
  - to:
    # Allow outbound to Microsoft Entra ID
    ports:
    - protocol: TCP
      port: 443
```

## Health Checks

The sidecar exposes health check endpoints:

```yaml
livenessProbe:
  httpGet:
    path: /health
    port: 5000
  initialDelaySeconds: 10
  periodSeconds: 10
  timeoutSeconds: 5
  failureThreshold: 3

readinessProbe:
  httpGet:
    path: /health
    port: 5000
  initialDelaySeconds: 5
  periodSeconds: 5
  timeoutSeconds: 3
  failureThreshold: 3
```

## Resource Requirements

Recommended resource allocations:

**Minimum:**
- Memory: 128Mi
- CPU: 100m

**Recommended:**
- Memory: 256Mi
- CPU: 250m

**High Traffic:**
- Memory: 512Mi
- CPU: 500m

Adjust based on:
- Token acquisition frequency
- Number of configured downstream APIs
- Cache size requirements

## Scaling Considerations

The sidecar is designed to scale with your application:

1. **Stateless Design**: Each sidecar instance maintains its own token cache
2. **Horizontal Scaling**: Scale by adding more application pods (each with its own sidecar)
3. **Cache Warming**: Consider implementing cache warming strategies for high-traffic scenarios

## Troubleshooting Installation

### Container Won't Start

Check container logs:
```bash
kubectl logs <pod-name> -c sidecar
```

Common issues:
- Invalid configuration values
- Network connectivity to Microsoft Entra ID
- Missing credentials or certificates

### Health Check Failures

Verify the sidecar is responding:
```bash
kubectl exec <pod-name> -c sidecar -- curl http://localhost:5000/health
```

### Permission Issues

Ensure the managed identity or service principal has:
- Correct application permissions
- Admin consent granted (if required)
- Proper role assignments for Azure resources

## Next Steps

- [Configuration Reference](configuration.md) - Learn about all configuration options
- [Security Best Practices](security.md) - Secure your deployment
- [Endpoints Reference](endpoints.md) - Explore the HTTP API
