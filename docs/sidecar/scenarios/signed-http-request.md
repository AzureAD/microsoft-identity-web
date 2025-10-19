# Scenario: Signed HTTP Requests (SHR)

This guide demonstrates how to use Signed HTTP Requests (SHR) with the sidecar for enhanced security through proof-of-possession tokens.

## Overview

Signed HTTP Requests provide cryptographic proof that the token holder possesses the private key corresponding to a public key, preventing token theft and replay attacks.

## Prerequisites

- Sidecar deployed and running
- RSA key pair generated
- Downstream API that supports PoP tokens

## Generate Key Pair

```bash
# Generate RSA private key
openssl genrsa -out private.pem 2048

# Extract public key
openssl rsa -in private.pem -pubout -out public.pem

# Base64 encode public key for configuration
base64 -w 0 public.pem > public.pem.b64

# View base64-encoded key
cat public.pem.b64
```

## Configuration

### Sidecar Configuration

```yaml
apiVersion: v1
kind: Secret
metadata:
  name: shr-keys
type: Opaque
data:
  public-key: <base64-encoded-public-key>

---
apiVersion: v1
kind: ConfigMap
metadata:
  name: sidecar-config
data:
  # ... other configuration ...
  DownstreamApis__SecureApi__BaseUrl: "https://api.contoso.com"
  DownstreamApis__SecureApi__Scopes: "api://secureapi/.default"
  DownstreamApis__SecureApi__AcquireTokenOptions__PopPublicKey: "<base64-public-key>"
```

## Usage Examples

### TypeScript

```typescript
// Request PoP token
async function getPopToken(incomingToken: string, publicKey: string): Promise<string> {
  const sidecarUrl = process.env.SIDECAR_URL!;
  
  const response = await fetch(
    `${sidecarUrl}/AuthorizationHeader/SecureApi?` +
    `optionsOverride.AcquireTokenOptions.PopPublicKey=${encodeURIComponent(publicKey)}`,
    {
      headers: {
        'Authorization': incomingToken
      }
    }
  );
  
  const data = await response.json();
  return data.authorizationHeader; // Returns "PoP <pop-token>"
}

// Use PoP token with signed request
async function callSecureApi(incomingToken: string, publicKey: string, privateKey: string) {
  // Get PoP token from sidecar
  const popToken = await getPopToken(incomingToken, publicKey);
  
  // Make request to API with PoP token
  const response = await fetch('https://api.contoso.com/secure/data', {
    headers: {
      'Authorization': popToken
    }
  });
  
  return await response.json();
}
```

### Python

```python
import base64
import requests

def get_pop_token(incoming_token: str, public_key: str) -> str:
    """Get a PoP token from the sidecar."""
    sidecar_url = os.getenv('SIDECAR_URL', 'http://localhost:5000')
    
    response = requests.get(
        f"{sidecar_url}/AuthorizationHeader/SecureApi",
        params={
            'optionsOverride.AcquireTokenOptions.PopPublicKey': public_key
        },
        headers={'Authorization': incoming_token}
    )
    
    response.raise_for_status()
    data = response.json()
    return data['authorizationHeader']

def call_secure_api(incoming_token: str, public_key_b64: str):
    """Call API with PoP token."""
    pop_token = get_pop_token(incoming_token, public_key_b64)
    
    response = requests.get(
        'https://api.contoso.com/secure/data',
        headers={'Authorization': pop_token}
    )
    
    return response.json()
```

## Per-Request SHR

Override SHR settings per request:

```typescript
// Enable SHR for specific request
const response = await fetch(
  `${sidecarUrl}/AuthorizationHeader/Graph?` +
  `optionsOverride.AcquireTokenOptions.PopPublicKey=${encodeURIComponent(publicKey)}`,
  {
    headers: { 'Authorization': incomingToken }
  }
);
```

## Key Management

### Secure Key Storage

```yaml
# Store keys in Kubernetes Secret
apiVersion: v1
kind: Secret
metadata:
  name: shr-keys
type: Opaque
data:
  public-key: <base64-encoded-public-key>
  private-key: <base64-encoded-private-key>

---
# Mount keys in application
volumes:
- name: shr-keys
  secret:
    secretName: shr-keys
    defaultMode: 0400

containers:
- name: app
  volumeMounts:
  - name: shr-keys
    mountPath: /keys
    readOnly: true
```

### Key Rotation

```bash
#!/bin/bash
# Script to rotate SHR keys

# Generate new key pair
openssl genrsa -out private-new.pem 2048
openssl rsa -in private-new.pem -pubout -out public-new.pem
base64 -w 0 public-new.pem > public-new.pem.b64

# Update Kubernetes secret
kubectl create secret generic shr-keys-new \
  --from-file=public-key=public-new.pem.b64 \
  --from-file=private-key=private-new.pem \
  --dry-run=client -o yaml | kubectl apply -f -

# Update deployment to use new keys
kubectl rollout restart deployment myapp
```

## Validating PoP Tokens

The downstream API must validate the PoP token:

1. Verify JWT signature using public key from token
2. Validate standard JWT claims (iss, aud, exp)
3. Verify `cnf` claim contains expected public key
4. Validate HTTP request signature

## Benefits

- **Token Binding**: Token bound to public key
- **Replay Prevention**: Cannot reuse token without private key  
- **Enhanced Security**: Protection against token theft
- **Proof of Possession**: Cryptographic proof of key ownership

## Best Practices

1. **Secure Private Keys**: Never expose private keys
2. **Rotate Keys**: Implement regular key rotation
3. **Per-API Keys**: Use different keys for different APIs
4. **Monitor Usage**: Audit PoP token usage
5. **Test Thoroughly**: Verify PoP token validation

## Next Steps

- [Security Best Practices](../security.md#signed-http-requests-shr)
- [Configuration Reference](../configuration.md#signed-http-request-shr-configuration)
- [Endpoints Reference](../endpoints.md)
