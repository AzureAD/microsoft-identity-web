# Scenario: Using Managed Identity

This guide demonstrates how to use Azure Managed Identity with the sidecar to eliminate credential management.

## Overview

Azure AD Workload Identity enables Kubernetes pods to authenticate to Azure services using managed identities without storing any credentials. The sidecar automatically uses workload identity when properly configured.

## Prerequisites

- Azure Kubernetes Service (AKS) cluster with OIDC issuer and workload identity enabled
- Azure managed identity with appropriate permissions
- Federated identity credential configured

## Setup Steps

### 1. Enable Workload Identity on AKS

```bash
# Create or update AKS cluster with workload identity
az aks create \
  --resource-group myResourceGroup \
  --name myAKSCluster \
  --enable-oidc-issuer \
  --enable-workload-identity

# Get OIDC issuer URL
export AKS_OIDC_ISSUER=$(az aks show \
  --resource-group myResourceGroup \
  --name myAKSCluster \
  --query "oidcIssuerProfile.issuerUrl" -o tsv)
```

### 2. Create Managed Identity

```bash
# Create managed identity
az identity create \
  --resource-group myResourceGroup \
  --name myapp-identity

# Get identity details
export IDENTITY_CLIENT_ID=$(az identity show \
  --resource-group myResourceGroup \
  --name myapp-identity \
  --query clientId -o tsv)

export IDENTITY_OBJECT_ID=$(az identity show \
  --resource-group myResourceGroup \
  --name myapp-identity \
  --query principalId -o tsv)
```

### 3. Grant Permissions

```bash
# Grant Microsoft Graph permissions
az ad app permission add \
  --id $IDENTITY_CLIENT_ID \
  --api 00000003-0000-0000-c000-000000000000 \
  --api-permissions e1fe6dd8-ba31-4d61-89e7-88639da4683d=Scope  # User.Read

# Grant admin consent
az ad app permission admin-consent --id $IDENTITY_CLIENT_ID
```

### 4. Create Federated Identity Credential

```bash
# Create federated credential for Kubernetes service account
az identity federated-credential create \
  --name myapp-federated-identity \
  --identity-name myapp-identity \
  --resource-group myResourceGroup \
  --issuer $AKS_OIDC_ISSUER \
  --subject system:serviceaccount:default:myapp-sa
```

### 5. Create Kubernetes Service Account

```yaml
apiVersion: v1
kind: ServiceAccount
metadata:
  name: myapp-sa
  namespace: default
  annotations:
    azure.workload.identity/client-id: "<MANAGED_IDENTITY_CLIENT_ID>"
```

Apply:
```bash
kubectl apply -f serviceaccount.yaml
```

## Deployment Configuration

### Complete Pod Configuration

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: myapp
  namespace: default
spec:
  replicas: 3
  selector:
    matchLabels:
      app: myapp
  template:
    metadata:
      labels:
        app: myapp
        azure.workload.identity/use: "true"  # Required for workload identity
    spec:
      serviceAccountName: myapp-sa
      containers:
      # Application container
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
      
      # Sidecar container
      - name: sidecar
        image: mcr.microsoft.com/entra-sdk/auth-sidecar:1.0.0
        ports:
        - containerPort: 5000
        env:
        # Azure AD Configuration
        - name: AzureAd__Instance
          value: "https://login.microsoftonline.com/"
        - name: AzureAd__TenantId
          value: "common"  # Or specific tenant ID
        - name: AzureAd__ClientId
          value: "<MANAGED_IDENTITY_CLIENT_ID>"
        # No client secret or certificate needed!
        
        # Downstream API Configuration
        - name: DownstreamApis__Graph__BaseUrl
          value: "https://graph.microsoft.com/v1.0"
        - name: DownstreamApis__Graph__Scopes
          value: "User.Read Mail.Read"
        
        # Logging
        - name: Logging__LogLevel__Default
          value: "Information"
        - name: Logging__LogLevel__Microsoft.Identity.Web
          value: "Information"
        
        resources:
          requests:
            memory: "128Mi"
            cpu: "100m"
          limits:
            memory: "256Mi"
            cpu: "250m"
        
        livenessProbe:
          httpGet:
            path: /healthz
            port: 5000
          initialDelaySeconds: 10
          periodSeconds: 10
        
        readinessProbe:
          httpGet:
            path: /healthz
            port: 5000
          initialDelaySeconds: 5
          periodSeconds: 5
```

## Verification

### Test Workload Identity

```bash
# Check pod labels
kubectl get pod -l app=myapp -o yaml | grep -A 5 "labels:"

# Verify service account
kubectl get pod -l app=myapp -o yaml | grep serviceAccountName

# Check sidecar logs
kubectl logs -l app=myapp -c sidecar

# Test token acquisition
kubectl exec -it $(kubectl get pod -l app=myapp -o name | head -1) -c app -- \
  curl -H "Authorization: Bearer <test-token>" \
  http://localhost:5000/AuthorizationHeader/Graph
```

### Verify Environment Variables

```bash
# Check identity environment variables in pod
kubectl exec -it $(kubectl get pod -l app=myapp -o name | head -1) -c sidecar -- env | grep AZURE

# Should see:
# AZURE_CLIENT_ID=<managed-identity-client-id>
# AZURE_TENANT_ID=<tenant-id>
# AZURE_FEDERATED_TOKEN_FILE=/var/run/secrets/azure/tokens/azure-identity-token
```

## Application Code

Your application code remains the same - no changes needed:

```typescript
// TypeScript example
async function getUserProfile(incomingToken: string) {
  const sidecarUrl = process.env.SIDECAR_URL!;
  
  const response = await fetch(
    `${sidecarUrl}/DownstreamApi/Graph?optionsOverride.RelativePath=me`,
    {
      headers: {
        'Authorization': incomingToken
      }
    }
  );
  
  const result = await response.json();
  return JSON.parse(result.content);
}
```

The sidecar automatically uses workload identity for token acquisition.

## Multiple Environments

### Development

Use client secret for local development:

```yaml
# dev-secrets.yaml (local only, not committed)
apiVersion: v1
kind: Secret
metadata:
  name: sidecar-secrets-dev
type: Opaque
stringData:
  AzureAd__ClientCredentials__0__SourceType: "ClientSecret"
  AzureAd__ClientCredentials__0__ClientSecret: "<dev-client-secret>"
```

### Production

Use workload identity (no secrets):

```yaml
# prod-serviceaccount.yaml
apiVersion: v1
kind: ServiceAccount
metadata:
  name: myapp-sa-prod
  annotations:
    azure.workload.identity/client-id: "<prod-managed-identity-client-id>"
```

## Troubleshooting

### Pod Fails to Start

```bash
# Check pod events
kubectl describe pod -l app=myapp

# Check sidecar logs
kubectl logs -l app=myapp -c sidecar
```

Common issues:
- Missing service account annotation
- Missing pod label `azure.workload.identity/use: "true"`
- Incorrect client ID in service account annotation

### Token Acquisition Fails

```bash
# Check logs for AADSTS errors
kubectl logs -l app=myapp -c sidecar | grep AADSTS
```

Common issues:
- Federated credential not configured correctly
- Issuer URL mismatch
- Subject pattern mismatch (`system:serviceaccount:<namespace>:<service-account>`)
- Missing permissions or admin consent

### Environment Variables Not Set

```bash
# Verify workload identity webhook is running
kubectl get pods -n kube-system | grep azure-workload-identity-webhook

# Check pod mutation
kubectl get pod -l app=myapp -o yaml | grep -A 10 "env:"
```

## Best Practices

1. **Separate Identities per Environment**: Use different managed identities for dev, staging, production
2. **Least Privilege**: Grant only required permissions to managed identity
3. **Monitor Usage**: Enable diagnostic logging for managed identity
4. **Review permissions regularly**: While no secrets to rotate, review permissions regularly
5. **Document Permissions**: Maintain documentation of granted permissions
6. **Test Thoroughly**: Verify workload identity in staging before production
7. **Use Labels**: Properly label pods with `azure.workload.identity/use: "true"`

## Benefits

- ✅ **No Secrets**: Eliminates credential storage and rotation
- ✅ **Automatic Renewal**: Tokens automatically renewed by Azure
- ✅ **Audit Trail**: All authentication events logged in Azure AD
- ✅ **RBAC Integration**: Works with Azure role-based access control
- ✅ **Simplified Operations**: Reduces operational complexity
- ✅ **Better Security**: Reduces credential exposure and theft risk

## Comparison with Other Methods

| Method | Security | Complexity | Maintenance |
|--------|----------|------------|-------------|
| **Workload Identity** | ⭐⭐⭐⭐⭐ | ⭐⭐⭐ | ⭐⭐⭐⭐⭐ |
| Certificate (Key Vault) | ⭐⭐⭐⭐ | ⭐⭐⭐ | ⭐⭐⭐⭐ |
| Certificate (K8s Secret) | ⭐⭐⭐ | ⭐⭐ | ⭐⭐⭐ |
| Client Secret | ⭐⭐ | ⭐ | ⭐⭐ |

## Next Steps

- [Installation Guide](../installation.md#azure-kubernetes-service-aks-with-managed-identity) - Detailed AKS setup
- [Security Best Practices](../security.md) - Security configuration
- [Configuration Reference](../configuration.md) - All configuration options
- [Troubleshooting](../troubleshooting.md) - Resolve issues
