# Reverse Proxy Deep Dive (Envoy, YARP, Nginx)

## Goals

- Shield sidecar from direct external access.
- Apply unified authZ, rate limiting, observability.

## Envoy Example

Core idea: route `/identity-sidecar/*` to `127.0.0.1:5080`, rewrite prefix, apply JWT filter before routing.

## YARP (.NET) Example

`appsettings.json`:
```json
{
  "ReverseProxy": {
    "Routes": {
      "sidecar": {
        "ClusterId": "sidecarCluster",
        "Match": { "Path": "/identity-sidecar/{**catch-all}" },
        "Transforms": [ { "PathRemovePrefix": "/identity-sidecar" } ]
      }
    },
    "Clusters": {
      "sidecarCluster": {
        "Destinations": { "sidecar": { "Address": "http://localhost:5080/" } }
      }
    }
  }
}
```

## Nginx

```
location /identity-sidecar/ {
  proxy_pass http://identity-sidecar:5080/;
  proxy_set_header Host $host;
  proxy_set_header X-Forwarded-For $remote_addr;
}
```

## Security Layering

| Concern | Mitigation |
|---------|------------|
| Unauthorized invocation | Upstream authN/Z on path |
| Abuse / flooding | Rate limits, circuit breakers |
| Token leakage | Strip sensitive headers; no token logs |

## Observability

Forward `X-Correlation-ID` from client → proxy → sidecar for diagnostic joins.

## Multi-Service Sharing

You may centralize sidecar usage, but evaluate isolation vs operational simplicity; sharing increases blast radius.
