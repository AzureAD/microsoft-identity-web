# Scenario: Validate an Authorization Header

This guide demonstrates how to validate an incoming bearer token received by your web API using the sidecar.

## Overview

In this scenario, you:
1. Receive a bearer token from a client in the Authorization header
2. Forward the token to the sidecar's `/Validate` endpoint
3. Receive validated token claims
4. Use the claims to make authorization decisions in your API

This approach allows your API to validate tokens without implementing token validation logic directly.

## Prerequisites

- Sidecar deployed and running
- Azure AD authentication configured in sidecar settings
- Client application sending valid bearer tokens

## Configuration

Configure the sidecar to validate tokens for your API:

```yaml
env:
- name: AzureAd__Instance
  value: "https://login.microsoftonline.com/"
- name: AzureAd__TenantId
  value: "your-tenant-id"
- name: AzureAd__ClientId
  value: "your-api-client-id"
- name: AzureAd__Audience
  value: "api://your-api-id"  # Expected audience in tokens
- name: AzureAd__Scopes
  value: "access_as_user"  # Optional: Required scopes
```

## Implementation Examples

### TypeScript/Node.js

```typescript
import fetch from 'node-fetch';

interface ValidateResponse {
  protocol: string;
  token: string;
  claims: {
    aud: string;
    iss: string;
    oid: string;
    sub: string;
    tid: string;
    upn?: string;
    scp?: string;
    roles?: string[];
    [key: string]: any;
  };
}

async function validateToken(authorizationHeader: string): Promise<ValidateResponse> {
  const sidecarUrl = process.env.SIDECAR_URL || 'http://localhost:5000';
  
  const response = await fetch(`${sidecarUrl}/Validate`, {
    headers: {
      'Authorization': authorizationHeader
    }
  });
  
  if (!response.ok) {
    throw new Error(`Token validation failed: ${response.statusText}`);
  }
  
  return await response.json() as ValidateResponse;
}

// Express.js middleware example
import express from 'express';

const app = express();

// Token validation middleware
async function requireAuth(req, res, next) {
  const authHeader = req.headers.authorization;
  
  if (!authHeader) {
    return res.status(401).json({ error: 'No authorization token provided' });
  }
  
  try {
    const validation = await validateToken(authHeader);
    
    // Attach claims to request object
    req.user = {
      id: validation.claims.oid,
      upn: validation.claims.upn,
      tenantId: validation.claims.tid,
      scopes: validation.claims.scp?.split(' ') || [],
      roles: validation.claims.roles || [],
      claims: validation.claims
    };
    
    next();
  } catch (error) {
    console.error('Token validation failed:', error);
    return res.status(401).json({ error: 'Invalid token' });
  }
}

// Protected endpoint
app.get('/api/protected', requireAuth, (req, res) => {
  res.json({
    message: 'Access granted',
    user: {
      id: req.user.id,
      upn: req.user.upn
    }
  });
});

// Scope-based authorization
app.get('/api/admin', requireAuth, (req, res) => {
  if (!req.user.roles.includes('Admin')) {
    return res.status(403).json({ error: 'Insufficient permissions' });
  }
  
  res.json({ message: 'Admin access granted' });
});

app.listen(8080);
```

### Python

```python
import os
import requests
from flask import Flask, request, jsonify
from functools import wraps

app = Flask(__name__)

def validate_token(authorization_header: str) -> dict:
    """Validate token using the sidecar."""
    sidecar_url = os.getenv('SIDECAR_URL', 'http://localhost:5000')
    
    response = requests.get(
        f"{sidecar_url}/Validate",
        headers={'Authorization': authorization_header}
    )
    
    if not response.ok:
        raise Exception(f"Token validation failed: {response.text}")
    
    return response.json()

# Token validation decorator
def require_auth(f):
    @wraps(f)
    def decorated_function(*args, **kwargs):
        auth_header = request.headers.get('Authorization')
        
        if not auth_header:
            return jsonify({'error': 'No authorization token provided'}), 401
        
        try:
            validation = validate_token(auth_header)
            
            # Attach user info to Flask's g object
            from flask import g
            g.user = {
                'id': validation['claims']['oid'],
                'upn': validation['claims'].get('upn'),
                'tenant_id': validation['claims']['tid'],
                'scopes': validation['claims'].get('scp', '').split(' '),
                'roles': validation['claims'].get('roles', []),
                'claims': validation['claims']
            }
            
            return f(*args, **kwargs)
        except Exception as e:
            print(f"Token validation failed: {e}")
            return jsonify({'error': 'Invalid token'}), 401
    
    return decorated_function

# Protected endpoint
@app.route('/api/protected')
@require_auth
def protected():
    from flask import g
    return jsonify({
        'message': 'Access granted',
        'user': {
            'id': g.user['id'],
            'upn': g.user['upn']
        }
    })

# Role-based authorization
@app.route('/api/admin')
@require_auth
def admin():
    from flask import g
    if 'Admin' not in g.user['roles']:
        return jsonify({'error': 'Insufficient permissions'}), 403
    
    return jsonify({'message': 'Admin access granted'})

if __name__ == '__main__':
    app.run(port=8080)
```

### Go

```go
package main

import (
    "encoding/json"
    "fmt"
    "net/http"
    "os"
    "strings"
)

type ValidateResponse struct {
    Protocol string                 `json:"protocol"`
    Token    string                 `json:"token"`
    Claims   map[string]interface{} `json:"claims"`
}

type User struct {
    ID       string
    UPN      string
    TenantID string
    Scopes   []string
    Roles    []string
    Claims   map[string]interface{}
}

func validateToken(authHeader string) (*ValidateResponse, error) {
    sidecarURL := os.Getenv("SIDECAR_URL")
    if sidecarURL == "" {
        sidecarURL = "http://localhost:5000"
    }
    
    req, err := http.NewRequest("GET", fmt.Sprintf("%s/Validate", sidecarURL), nil)
    if err != nil {
        return nil, err
    }
    
    req.Header.Set("Authorization", authHeader)
    
    client := &http.Client{}
    resp, err := client.Do(req)
    if err != nil {
        return nil, err
    }
    defer resp.Body.Close()
    
    if resp.StatusCode != http.StatusOK {
        return nil, fmt.Errorf("token validation failed: %s", resp.Status)
    }
    
    var validation ValidateResponse
    if err := json.NewDecoder(resp.Body).Decode(&validation); err != nil {
        return nil, err
    }
    
    return &validation, nil
}

// Middleware for token validation
func requireAuth(next http.HandlerFunc) http.HandlerFunc {
    return func(w http.ResponseWriter, r *http.Request) {
        authHeader := r.Header.Get("Authorization")
        
        if authHeader == "" {
            http.Error(w, "No authorization token provided", http.StatusUnauthorized)
            return
        }
        
        validation, err := validateToken(authHeader)
        if err != nil {
            http.Error(w, "Invalid token", http.StatusUnauthorized)
            return
        }
        
        // Extract user information from claims
        user := &User{
            ID:       validation.Claims["oid"].(string),
            TenantID: validation.Claims["tid"].(string),
            Claims:   validation.Claims,
        }
        
        if upn, ok := validation.Claims["upn"].(string); ok {
            user.UPN = upn
        }
        
        if scp, ok := validation.Claims["scp"].(string); ok {
            user.Scopes = strings.Split(scp, " ")
        }
        
        if roles, ok := validation.Claims["roles"].([]interface{}); ok {
            for _, role := range roles {
                user.Roles = append(user.Roles, role.(string))
            }
        }
        
        // Store user in context (simplified - use context.Context in production)
        r.Header.Set("X-User-ID", user.ID)
        r.Header.Set("X-User-UPN", user.UPN)
        
        next(w, r)
    }
}

func protectedHandler(w http.ResponseWriter, r *http.Request) {
    w.Header().Set("Content-Type", "application/json")
    json.NewEncoder(w).Encode(map[string]interface{}{
        "message": "Access granted",
        "user": map[string]string{
            "id":  r.Header.Get("X-User-ID"),
            "upn": r.Header.Get("X-User-UPN"),
        },
    })
}

func main() {
    http.HandleFunc("/api/protected", requireAuth(protectedHandler))
    
    fmt.Println("Server starting on :8080")
    http.ListenAndServe(":8080", nil)
}
```

### C#

```csharp
using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;

public class ValidateResponse
{
    public string Protocol { get; set; }
    public string Token { get; set; }
    public JsonElement Claims { get; set; }
}

public class TokenValidationService
{
    private readonly HttpClient _httpClient;
    private readonly string _sidecarUrl;
    
    public TokenValidationService(IHttpClientFactory httpClientFactory, IConfiguration config)
    {
        _httpClient = httpClientFactory.CreateClient();
        _sidecarUrl = config["SIDECAR_URL"] ?? "http://localhost:5000";
    }
    
    public async Task<ValidateResponse> ValidateTokenAsync(string authorizationHeader)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"{_sidecarUrl}/Validate");
        request.Headers.Add("Authorization", authorizationHeader);
        
        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
        
        return await response.Content.ReadFromJsonAsync<ValidateResponse>();
    }
}

// Middleware example
public class TokenValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly TokenValidationService _validationService;
    
    public TokenValidationMiddleware(RequestDelegate next, TokenValidationService validationService)
    {
        _next = next;
        _validationService = validationService;
    }
    
    public async Task InvokeAsync(HttpContext context)
    {
        var authHeader = context.Request.Headers["Authorization"].ToString();
        
        if (string.IsNullOrEmpty(authHeader))
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { error = "No authorization token" });
            return;
        }
        
        try
        {
            var validation = await _validationService.ValidateTokenAsync(authHeader);
            
            // Store claims in HttpContext.Items for use in controllers
            context.Items["UserClaims"] = validation.Claims;
            context.Items["UserId"] = validation.Claims.GetProperty("oid").GetString();
            
            await _next(context);
        }
        catch (Exception ex)
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { error = "Invalid token" });
        }
    }
}

// Controller example
[ApiController]
[Route("api")]
public class ProtectedController : ControllerBase
{
    [HttpGet("protected")]
    public IActionResult GetProtected()
    {
        var userId = HttpContext.Items["UserId"] as string;
        
        return Ok(new
        {
            message = "Access granted",
            user = new { id = userId }
        });
    }
}
```

## Extracting Specific Claims

### User Identity

```typescript
// Extract user identity
const userId = validation.claims.oid;  // Object ID
const userPrincipalName = validation.claims.upn;  // User Principal Name
const tenantId = validation.claims.tid;  // Tenant ID
```

### Scopes and Roles

```typescript
// Extract scopes (delegated permissions)
const scopes = validation.claims.scp?.split(' ') || [];

// Check for specific scope
if (scopes.includes('User.Read')) {
  // Allow access
}

// Extract roles (application permissions)
const roles = validation.claims.roles || [];

// Check for specific role
if (roles.includes('Admin')) {
  // Allow admin access
}
```

### Agent Identity Claims

```typescript
// Check if token represents an agent user identity
const isAgentUser = validation.claims.xms_sub_fct?.includes('13');

// Get parent agent blueprint
const parentAgent = validation.claims.xms_par_app_azp;
```

## Authorization Patterns

### Scope-Based Authorization

```typescript
function requireScopes(requiredScopes: string[]) {
  return async (req, res, next) => {
    const validation = await validateToken(req.headers.authorization);
    const userScopes = validation.claims.scp?.split(' ') || [];
    
    const hasAllScopes = requiredScopes.every(scope => userScopes.includes(scope));
    
    if (!hasAllScopes) {
      return res.status(403).json({ 
        error: 'Insufficient scopes',
        required: requiredScopes,
        provided: userScopes
      });
    }
    
    req.user = { claims: validation.claims };
    next();
  };
}

// Usage
app.get('/api/profile', requireScopes(['User.Read']), (req, res) => {
  // Handle request
});
```

### Role-Based Authorization

```typescript
function requireRoles(requiredRoles: string[]) {
  return async (req, res, next) => {
    const validation = await validateToken(req.headers.authorization);
    const userRoles = validation.claims.roles || [];
    
    const hasAnyRole = requiredRoles.some(role => userRoles.includes(role));
    
    if (!hasAnyRole) {
      return res.status(403).json({ 
        error: 'Insufficient permissions',
        required: requiredRoles,
        provided: userRoles
      });
    }
    
    req.user = { claims: validation.claims };
    next();
  };
}

// Usage
app.get('/api/admin', requireRoles(['Admin', 'GlobalAdmin']), (req, res) => {
  // Handle request
});
```

## Error Handling

### Handle Validation Errors

```typescript
async function validateTokenSafely(authHeader: string): Promise<ValidateResponse | null> {
  try {
    return await validateToken(authHeader);
  } catch (error) {
    if (error.message.includes('401')) {
      console.error('Token is invalid or expired');
    } else if (error.message.includes('403')) {
      console.error('Token missing required scopes');
    } else {
      console.error('Token validation error:', error.message);
    }
    return null;
  }
}
```

### Common Validation Errors

| Error | Cause | Solution |
|-------|-------|----------|
| 401 Unauthorized | Invalid or expired token | Request new token from client |
| 403 Forbidden | Missing required scopes | Update scope configuration or token request |
| 400 Bad Request | Malformed authorization header | Check header format: `******` |

## Response Structure

The `/Validate` endpoint returns:

```json
{
  "protocol": "Bearer",
  "token": "******",
  "claims": {
    "aud": "api://your-api-id",
    "iss": "https://sts.windows.net/tenant-id/",
    "iat": 1234567890,
    "nbf": 1234567890,
    "exp": 1234571490,
    "oid": "user-object-id",
    "sub": "subject",
    "tid": "tenant-id",
    "upn": "user@contoso.com",
    "scp": "User.Read Mail.Read",
    "roles": ["Admin"]
  }
}
```

## Best Practices

1. **Validate Early**: Validate tokens at the API gateway or entry point
2. **Check Scopes**: Always verify token has required scopes for the operation
3. **Log Failures**: Log validation failures for security monitoring
4. **Handle Errors**: Provide clear error messages for debugging
5. **Use Middleware**: Implement validation as middleware for consistency
6. **Secure Sidecar**: Ensure sidecar is only accessible from your application

## Next Steps

- [Obtain Authorization Header](obtain-authorization-header.md) - Get tokens for downstream APIs
- [Security Best Practices](../security.md) - Secure your deployment
- [Endpoints Reference](../endpoints.md) - Complete API documentation
- [Troubleshooting](../troubleshooting.md) - Resolve validation issues
