# Reverse Proxy Integration

This document provides guidance for integrating the Microsoft Entra Identity Sidecar with reverse proxies and API gateways.

## Overview

In microservice architectures, reverse proxies and API gateways often sit in front of services to handle:

- Request routing
- Load balancing
- SSL termination
- Rate limiting
- Authentication/Authorization

When using the sidecar, careful configuration is required to ensure authentication flows work correctly through the proxy layer.

## Common Scenarios

### Scenario 1: Reverse Proxy in Front of Application

The reverse proxy handles external traffic and forwards to your application, which communicates with the sidecar:

```
Client → Reverse Proxy → Application → Sidecar → Microsoft Entra ID
                                     ↓
                               Downstream APIs
```

### Scenario 2: API Gateway Pattern

An API gateway aggregates multiple services, each with their own sidecar:

```
Client → API Gateway → Service A → Sidecar A
                    ↘ Service B → Sidecar B
                    ↘ Service C → Sidecar C
```

## Envoy Proxy

Envoy is a popular service proxy commonly used in Kubernetes environments.

### Configuration Principles

1. **Preserve Authorization Headers**: Ensure Envoy forwards the `Authorization` header
2. **SSL/TLS Termination**: Handle at the edge, not between app and sidecar
3. **Header Management**: Pass through necessary headers
4. **Timeout Configuration**: Set appropriate timeouts for token acquisition

### Envoy Configuration Example

```yaml
static_resources:
  listeners:
  - name: listener_0
    address:
      socket_address:
        address: 0.0.0.0
        port_value: 8080
    filter_chains:
    - filters:
      - name: envoy.filters.network.http_connection_manager
        typed_config:
          "@type": type.googleapis.com/envoy.extensions.filters.network.http_connection_manager.v3.HttpConnectionManager
          stat_prefix: ingress_http
          codec_type: AUTO
          route_config:
            name: local_route
            virtual_hosts:
            - name: backend
              domains: ["*"]
              routes:
              - match:
                  prefix: "/api"
                route:
                  cluster: application_cluster
                  timeout: 30s
          http_filters:
          # Preserve headers
          - name: envoy.filters.http.router
            typed_config:
              "@type": type.googleapis.com/envoy.extensions.filters.http.router.v3.Router
  
  clusters:
  - name: application_cluster
    connect_timeout: 5s
    type: STRICT_DNS
    lb_policy: ROUND_ROBIN
    load_assignment:
      cluster_name: application_cluster
      endpoints:
      - lb_endpoints:
        - endpoint:
            address:
              socket_address:
                address: application-service
                port_value: 8080
```

### Envoy with JWT Authentication

Envoy can validate JWTs before forwarding to your application:

```yaml
http_filters:
- name: envoy.filters.http.jwt_authn
  typed_config:
    "@type": type.googleapis.com/envoy.extensions.filters.http.jwt_authn.v3.JwtAuthentication
    providers:
      microsoft_entra:
        issuer: https://sts.windows.net/<tenant-id>/
        audiences:
        - api://your-api-id
        remote_jwks:
          http_uri:
            uri: https://login.microsoftonline.com/<tenant-id>/discovery/v2.0/keys
            cluster: microsoft_entra_jwks
            timeout: 5s
          cache_duration:
            seconds: 300
    rules:
    - match:
        prefix: /api
      requires:
        provider_name: microsoft_entra
- name: envoy.filters.http.router
  typed_config:
    "@type": type.googleapis.com/envoy.extensions.filters.http.router.v3.Router

clusters:
- name: microsoft_entra_jwks
  connect_timeout: 5s
  type: STRICT_DNS
  lb_policy: ROUND_ROBIN
  load_assignment:
    cluster_name: microsoft_entra_jwks
    endpoints:
    - lb_endpoints:
      - endpoint:
          address:
            socket_address:
              address: login.microsoftonline.com
              port_value: 443
  transport_socket:
    name: envoy.transport_sockets.tls
    typed_config:
      "@type": type.googleapis.com/envoy.extensions.transport_sockets.tls.v3.UpstreamTlsContext
      sni: login.microsoftonline.com
```

**Benefits:**
- Validation happens at the edge
- Invalid tokens rejected early
- Reduce load on application and sidecar

**Note**: Even if Envoy validates tokens, your application should still pass them to the sidecar for OBO token acquisition.

### Istio Integration

Istio builds on Envoy and provides additional features:

```yaml
apiVersion: security.istio.io/v1beta1
kind: RequestAuthentication
metadata:
  name: jwt-auth
  namespace: default
spec:
  selector:
    matchLabels:
      app: myapp
  jwtRules:
  - issuer: "https://sts.windows.net/<tenant-id>/"
    jwksUri: "https://login.microsoftonline.com/<tenant-id>/discovery/v2.0/keys"
    audiences:
    - "api://your-api-id"

---
apiVersion: security.istio.io/v1beta1
kind: AuthorizationPolicy
metadata:
  name: require-jwt
  namespace: default
spec:
  selector:
    matchLabels:
      app: myapp
  action: ALLOW
  rules:
  - from:
    - source:
        requestPrincipals: ["*"]
```

## YARP (Yet Another Reverse Proxy)

YARP is a reverse proxy toolkit for .NET applications.

### Basic YARP Configuration

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

app.MapReverseProxy();

app.Run();
```

```jsonc
// appsettings.json
{
  "ReverseProxy": {
    "Routes": {
      "api-route": {
        "ClusterId": "api-cluster",
        "Match": {
          "Path": "/api/{**catch-all}"
        },
        "Transforms": [
          {
            "PathPattern": "/{**catch-all}"
          }
        ]
      }
    },
    "Clusters": {
      "api-cluster": {
        "Destinations": {
          "destination1": {
            "Address": "http://application-service:8080"
          }
        }
      }
    }
  }
}
```

### YARP with Microsoft.Identity.Web

You can use Microsoft.Identity.Web with YARP to validate tokens at the proxy:

```csharp
var builder = WebApplication.CreateBuilder(args);

// Add authentication
builder.Services.AddMicrosoftIdentityWebApiAuthentication(builder.Configuration);

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapReverseProxy();

app.Run();
```

### YARP with Header Transformation

Forward authentication context to downstream services:

```json
{
  "ReverseProxy": {
    "Routes": {
      "api-route": {
        "ClusterId": "api-cluster",
        "Match": {
          "Path": "/api/{**catch-all}"
        },
        "Transforms": [
          {
            "PathPattern": "/{**catch-all}"
          },
          {
            "RequestHeaderOriginalHost": "true"
          },
          {
            "X-Forwarded": "Append"
          },
          {
            "RequestHeader": "X-User-Id",
            "Set": "{User.Claims[oid]}"
          },
          {
            "RequestHeader": "X-Tenant-Id",
            "Set": "{User.Claims[tid]}"
          }
        ]
      }
    }
  }
}
```

## Nginx

Nginx is a widely-used web server and reverse proxy.

### Basic Nginx Configuration

```nginx
upstream application {
    server application-service:8080;
}

server {
    listen 80;
    server_name api.example.com;
    
    location /api {
        proxy_pass http://application;
        proxy_http_version 1.1;
        
        # Preserve original headers
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        
        # Pass Authorization header
        proxy_set_header Authorization $http_authorization;
        proxy_pass_header Authorization;
        
        # Timeout settings
        proxy_connect_timeout 5s;
        proxy_send_timeout 30s;
        proxy_read_timeout 30s;
    }
}
```

### Nginx with SSL Termination

```nginx
server {
    listen 443 ssl http2;
    server_name api.example.com;
    
    ssl_certificate /etc/nginx/ssl/cert.pem;
    ssl_certificate_key /etc/nginx/ssl/key.pem;
    ssl_protocols TLSv1.2 TLSv1.3;
    ssl_ciphers HIGH:!aNULL:!MD5;
    
    location /api {
        proxy_pass http://application;
        proxy_http_version 1.1;
        
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto https;
        proxy_set_header Authorization $http_authorization;
        
        proxy_connect_timeout 5s;
        proxy_send_timeout 30s;
        proxy_read_timeout 30s;
    }
}

server {
    listen 80;
    server_name api.example.com;
    return 301 https://$server_name$request_uri;
}
```

### Nginx with JWT Validation (nginx-jwt Module)

```nginx
# Requires nginx-jwt module
location /api {
    # JWT validation
    auth_jwt "API";
    auth_jwt_key_file /etc/nginx/jwt_keys.json;
    
    # Validate claims
    auth_jwt_require $jwt_claim_aud == "api://your-api-id";
    auth_jwt_require $jwt_claim_iss == "https://sts.windows.net/<tenant-id>/";
    
    proxy_pass http://application;
    proxy_set_header Authorization $http_authorization;
}
```

### Nginx JWT Keys Configuration

```json
{
  "keys": [
    {
      "kty": "RSA",
      "use": "sig",
      "kid": "key-id",
      "n": "...",
      "e": "AQAB"
    }
  ]
}
```

Regularly update JWT keys from Microsoft Entra ID discovery endpoint:

```bash
#!/bin/bash
# Update JWT keys from Microsoft Entra ID
curl -s "https://login.microsoftonline.com/<tenant-id>/discovery/v2.0/keys" \
  -o /etc/nginx/jwt_keys.json

nginx -s reload
```

## API Gateway Patterns

### Pattern 1: Edge Authentication

API gateway validates tokens; services trust gateway:

```
Client (JWT) → Gateway (validates) → Service (trusted) → Sidecar
```

**Configuration:**
- Gateway: Validate JWT, forward to service
- Service: Trust requests from gateway
- Sidecar: Use incoming token for OBO

**Pros:**
- Centralized authentication
- Simpler service logic
- Reduced validation overhead

**Cons:**
- Trust boundary at gateway
- Requires secure gateway-to-service communication

### Pattern 2: End-to-End Authentication

Each service validates tokens independently:

```
Client (JWT) → Gateway (forwards) → Service (validates) → Sidecar
```

**Configuration:**
- Gateway: Forward token without validation
- Service: Validate token
- Sidecar: Use validated token for OBO

**Pros:**
- Defense in depth
- No trust assumptions
- Fine-grained authorization per service

**Cons:**
- Repeated validation overhead
- More complex service configuration

### Pattern 3: Token Exchange at Gateway

Gateway exchanges client token for service-specific token:

```
Client (JWT) → Gateway (exchanges) → Service (new JWT) → Sidecar
```

**Configuration:**
- Gateway: Exchange token using OBO
- Service: Validate new token
- Sidecar: Use new token for downstream calls

**Pros:**
- Least privilege (service-specific tokens)
- Gateway controls token scope
- Audit trail at gateway

**Cons:**
- Additional token acquisition at gateway
- More complex gateway logic
- Higher latency

## Header Handling

### Required Headers

Always forward these headers from proxy to application:

```
Authorization: Bearer <token>
```

### Optional Headers

Consider forwarding for tracing and debugging:

```
X-Forwarded-For: <client-ip>
X-Forwarded-Proto: https
X-Forwarded-Host: <original-host>
X-Request-Id: <correlation-id>
X-Correlation-Id: <correlation-id>
```

### Custom Headers for Sidecar

If your application adds custom headers for the sidecar:

```http
GET /DownstreamApi/Graph HTTP/1.1
Authorization: Bearer <incoming-token>
X-Correlation-Id: <correlation-id>
```

Ensure the proxy forwards custom headers:

```nginx
# Nginx example
proxy_set_header X-Correlation-Id $http_x_correlation_id;
```

## Timeout Configuration

Set appropriate timeouts for token acquisition:

| Component | Recommended Timeout | Reason |
|-----------|---------------------|--------|
| Client → Gateway | 60s | Allow time for full request |
| Gateway → Service | 30s | Service processing time |
| Service → Sidecar | 10s | Token acquisition time |
| Sidecar → Microsoft Entra ID | 5s | External API call |

### Envoy Timeouts

```yaml
route:
  timeout: 30s
  idle_timeout: 60s
  per_try_timeout: 5s
```

### Nginx Timeouts

```nginx
proxy_connect_timeout 5s;
proxy_send_timeout 30s;
proxy_read_timeout 30s;
```

### YARP Timeouts

```json
{
  "Clusters": {
    "api-cluster": {
      "HttpRequest": {
        "Timeout": "00:00:30",
        "Version": "2.0"
      }
    }
  }
}
```

## Caching and Performance

### Token Caching at Proxy

Some proxies can cache responses. **Do not cache** authentication responses:

```nginx
# Nginx - Disable caching for auth endpoints
location /AuthorizationHeader {
    proxy_pass http://sidecar;
    proxy_no_cache 1;
    proxy_cache_bypass 1;
}
```

### Connection Pooling

Enable HTTP/2 and connection pooling for better performance:

```nginx
# Nginx
upstream sidecar {
    server localhost:5000;
    keepalive 32;
    keepalive_timeout 60s;
}

server {
    location / {
        proxy_pass http://sidecar;
        proxy_http_version 1.1;
        proxy_set_header Connection "";
    }
}
```

## Security Considerations

### TLS Between Components

Use TLS for all communication in production:

```
Client --[TLS]-> Gateway --[TLS]-> Service --[localhost]-> Sidecar
```

### Mutual TLS (mTLS)

For service-to-service communication:

```yaml
# Istio mTLS
apiVersion: security.istio.io/v1beta1
kind: PeerAuthentication
metadata:
  name: default
spec:
  mtls:
    mode: STRICT
```

### Rate Limiting

Implement rate limiting at the gateway:

```nginx
# Nginx rate limiting
limit_req_zone $binary_remote_addr zone=api_limit:10m rate=10r/s;

location /api {
    limit_req zone=api_limit burst=20 nodelay;
    proxy_pass http://application;
}
```

## Troubleshooting

### Authorization Header Not Forwarded

**Symptom**: 401 Unauthorized errors

**Solution**: Ensure proxy forwards Authorization header:

```nginx
proxy_set_header Authorization $http_authorization;
proxy_pass_header Authorization;
```

### Token Validation Fails at Proxy

**Symptom**: Proxy rejects valid tokens

**Solution**: 
- Verify JWT keys are current
- Check issuer and audience configuration
- Ensure clock synchronization

### Timeout Errors

**Symptom**: 504 Gateway Timeout

**Solution**:
- Increase proxy timeouts
- Check sidecar response times
- Verify network connectivity

### SSL/TLS Errors

**Symptom**: SSL handshake failures

**Solution**:
- Verify certificate validity
- Check certificate chain
- Ensure TLS version compatibility

## Best Practices

1. **Terminate SSL at Edge**: Handle SSL/TLS at the gateway, use HTTP internally
2. **Forward All Required Headers**: Especially Authorization and correlation IDs
3. **Set Appropriate Timeouts**: Balance responsiveness and reliability
4. **Enable Connection Pooling**: Reduce connection overhead
5. **Implement Health Checks**: Monitor sidecar and application health
6. **Use mTLS for Service-to-Service**: Secure internal communication
7. **Rate Limit at Edge**: Protect against abuse
8. **Log and Monitor**: Track authentication flows through proxy
9. **Cache Appropriately**: Never cache authentication responses
10. **Update JWT Keys Regularly**: Keep validation keys current

## Next Steps

- [Security Best Practices](security.md) - Comprehensive security guidance
- [Endpoints Reference](endpoints.md) - Sidecar HTTP API
- [Troubleshooting](troubleshooting.md) - Common issues and solutions
- [Installation Guide](installation.md) - Deploy the sidecar
