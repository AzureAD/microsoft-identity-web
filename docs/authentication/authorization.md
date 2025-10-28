# Authorization in Web APIs with Microsoft.Identity.Web

This guide explains how to implement authorization in ASP.NET Core web APIs using Microsoft.Identity.Web. Authorization ensures that authenticated callers have the necessary **scopes** (delegated permissions) or **app permissions** (application permissions) to access protected resources.

---

## 📋 Table of Contents

- [Overview](#overview)
- [Authorization Concepts](#authorization-concepts)
- [Scope Validation with RequiredScope](#scope-validation-with-requiredscope)
- [App Permissions with RequiredScopeOrAppPermission](#app-permissions-with-requiredscopeorapppermission)
- [Authorization Policies](#authorization-policies)
- [Tenant Filtering](#tenant-filtering)
- [Best Practices](#best-practices)

---

## Overview

### Authentication vs Authorization

| Concept | Purpose | Result |
|---------|---------|--------|
| **Authentication** | Verify identity | 401 Unauthorized if fails |
| **Authorization** | Verify permissions | 403 Forbidden if insufficient |

### What Gets Validated?

When a web API receives an access token, Microsoft.Identity.Web validates:

1. **Token signature** - Is it from a trusted authority?
2. **Token audience** - Is it intended for this API?
3. **Token expiration** - Is it still valid?
4. **Scopes/Roles** - Doe the client app and the subject (user) have the right permissions?

This guide focuses on **#4 - validating scopes and app permissions**.

---

## Authorization Concepts

### Scopes (Delegated Permissions)

**Used when:** A user delegates permission to an app to act on their behalf.

**Token claim:** `scp` or `scope` for the client app
**Example values:** `"access_as_user"`, `"User.Read"`, `"Files.ReadWrite"`

**Token claim:** `roles`
**Example values:** `"admin"`, `"SimpleUser"` for the  user.


**Scenario:** Web API on behalf of signed-in user.

### App Permissions (Application Permissions)

**Used when:** Web API called by an app acting as itself (no user context), like a daemon/background service.

**Token claim:** `roles`
**Example values:** `"Mail.Read.All"`, `"User.Read.All"`

**Scenario:** Daemon app calls web API using client credentials.

---

## Scope Validation with RequiredScope

The `RequiredScope` attribute validates that the access token contains at least one of the specified scopes.

### Quick Start

**1. Enable authorization in your API:**

```csharp
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Web;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));

builder.Services.AddAuthorization(); // Required for authorization

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization(); // Must be after UseAuthentication
app.MapControllers();

app.Run();
```

**2. Protect controllers or actions:**

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.Identity.Web.Resource;

[Authorize]
[RequiredScope("access_as_user")]
public class TodoListController : ControllerBase
{
    [HttpGet]
    public IActionResult GetTodos()
    {
        // Only accessible if token has "access_as_user" scope
        return Ok(new[] { "Todo 1", "Todo 2" });
    }
}
```

### Usage Patterns

#### Pattern 1: Hardcoded Scopes

**Use when:** Scopes are fixed and known at development time.

```csharp
[Authorize]
[RequiredScope("access_as_user")]
public class TodoListController : ControllerBase
{
    // All actions require "access_as_user" scope
}
```

**Multiple scopes (any one matches):**

```csharp
[Authorize]
[RequiredScope("read", "write", "admin")]
public class TodoListController : ControllerBase
{
    // Token must have "read" OR "write" OR "admin"
}
```

#### Pattern 2: Scopes from Configuration

**Use when:** Scopes should be configurable per environment.

**appsettings.json:**
```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "your-tenant-id",
    "ClientId": "your-api-client-id",
    "Scopes": "access_as_user read write"
  }
}
```

**Controller:**
```csharp
[Authorize]
[RequiredScope(RequiredScopesConfigurationKey = "AzureAd:Scopes")]
public class TodoListController : ControllerBase
{
    // Scopes read from configuration
}
```

**✅ Advantage:** Change scopes without recompiling.

#### Pattern 3: Action-Level Scopes

**Use when:** Different actions require different permissions.

```csharp
[Authorize]
public class TodoListController : ControllerBase
{
    [HttpGet]
    [RequiredScope("read")]
    public IActionResult GetTodos()
    {
        return Ok(todos);
    }

    [HttpPost]
    [RequiredScope("write")]
    public IActionResult CreateTodo([FromBody] Todo todo)
    {
        // Only tokens with "write" scope can create
        return CreatedAtAction(nameof(GetTodos), todo);
    }

    [HttpDelete("{id}")]
    [RequiredScope("admin")]
    public IActionResult DeleteTodo(int id)
    {
        // Only tokens with "admin" scope can delete
        return NoContent();
    }
}
```

### How It Works

When a request arrives:

1. ASP.NET Core authentication middleware validates the token
2. `RequiredScope` attribute checks for the `scp` or `scope` claim
3. If token contains at least one matching scope → ✅ Request proceeds
4. If no matching scope found → ❌ 403 Forbidden response

**Error response example:**
```json
{
  "error": "insufficient_scope",
  "error_description": "The token does not have the required scope 'access_as_user'."
}
```

---

## App Permissions with RequiredScopeOrAppPermission

The `RequiredScopeOrAppPermission` attribute validates either **scopes** (delegated) OR **app permissions** (application).

### When to Use

**✅ Use `RequiredScopeOrAppPermission` when:**
- Your API serves both user-delegated apps AND daemon/service apps
- Same endpoint should accept tokens from web apps (scopes) or background services (app permissions)

**❌ Use `RequiredScope` when:**
- Your API only serves user-delegated requests

### Quick Start

```csharp
using Microsoft.Identity.Web.Resource;

[Authorize]
[RequiredScopeOrAppPermission(
    AcceptedScope = new[] { "access_as_user" },
    AcceptedAppPermission = new[] { "TodoList.ReadWrite.All" }
)]
public class TodoListController : ControllerBase
{
    [HttpGet]
    public IActionResult GetTodos()
    {
        // Accessible with EITHER:
        // - User-delegated token with "access_as_user" scope, OR
        // - App-only token with "TodoList.ReadWrite.All" app permission
        return Ok(todos);
    }
}
```

### Configuration-Based App Permissions

**appsettings.json:**
```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "your-tenant-id",
    "ClientId": "your-api-client-id",
    "Scopes": "access_as_user",
    "AppPermissions": "TodoList.ReadWrite.All TodoList.Admin"
  }
}
```

**Controller:**
```csharp
[Authorize]
[RequiredScopeOrAppPermission(
    RequiredScopesConfigurationKey = "AzureAd:Scopes",
    RequiredAppPermissionsConfigurationKey = "AzureAd:AppPermissions"
)]
public class TodoListController : ControllerBase
{
    // Scopes and app permissions from configuration
}
```

### Token Claim Differences

| Token Type | Claim | Example Value |
|------------|-------|---------------|
| **User-delegated** | `scp` or `scope` | `"access_as_user User.Read"` |
| **App-only** | `roles` | `["TodoList.ReadWrite.All"]` |

**Example: User-delegated token:**
```json
{
  "aud": "api://your-api-client-id",
  "iss": "https://login.microsoftonline.com/.../v2.0",
  "scp": "access_as_user",
  "sub": "user-object-id",
  ...
}
```

**Example: App-only token:**
```json
{
  "aud": "api://your-api-client-id",
  "iss": "https://login.microsoftonline.com/.../v2.0",
  "roles": ["TodoList.ReadWrite.All"],
  "sub": "app-object-id",
  ...
}
```

---

## Authorization Policies

For more complex authorization scenarios, use ASP.NET Core authorization policies.

### Why Use Policies?

- **Centralized logic** - Define authorization rules once, reuse everywhere
- **Composable** - Combine multiple requirements (scopes + claims + custom logic)
- **Testable** - Easier to unit test authorization logic
- **Flexible** - Custom requirements beyond scope validation

### Pattern 1: Policy with RequireScope

```csharp
using Microsoft.Identity.Web;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("TodoReadPolicy", policyBuilder =>
    {
        policyBuilder.RequireScope("read", "access_as_user");
    });

    options.AddPolicy("TodoWritePolicy", policyBuilder =>
    {
        policyBuilder.RequireScope("write", "admin");
    });
});

var app = builder.Build();
```

**Controller:**
```csharp
[Authorize]
public class TodoListController : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = "TodoReadPolicy")]
    public IActionResult GetTodos()
    {
        return Ok(todos);
    }

    [HttpPost]
    [Authorize(Policy = "TodoWritePolicy")]
    public IActionResult CreateTodo([FromBody] Todo todo)
    {
        return CreatedAtAction(nameof(GetTodos), todo);
    }
}
```

### Pattern 2: Policy with ScopeAuthorizationRequirement

```csharp
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.Resource;

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("CustomPolicy", policyBuilder =>
    {
        policyBuilder.AddRequirements(
            new ScopeAuthorizationRequirement(new[] { "access_as_user" })
        );
    });
});
```

### Pattern 3: Default Policy (Applies to All [Authorize])

```csharp
builder.Services.AddAuthorization(options =>
{
    var defaultPolicy = new AuthorizationPolicyBuilder()
        .RequireScope("access_as_user")
        .Build();

    options.DefaultPolicy = defaultPolicy;
});
```

Now every `[Authorize]` attribute automatically requires the "access_as_user" scope:

```csharp
[Authorize] // Automatically requires "access_as_user" scope
public class TodoListController : ControllerBase
{
    // All actions protected by default policy
}
```

### Pattern 4: Combining Multiple Requirements

```csharp
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminPolicy", policyBuilder =>
    {
        policyBuilder.RequireScope("admin");
        policyBuilder.RequireRole("Admin"); // Also check role claim
        policyBuilder.RequireAuthenticatedUser();
    });
});
```

### Pattern 5: Configuration-Based Policy

```csharp
var requiredScopes = builder.Configuration["AzureAd:Scopes"]?.Split(' ');

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("ApiAccessPolicy", policyBuilder =>
    {
        if (requiredScopes != null)
        {
            policyBuilder.RequireScope(requiredScopes);
        }
    });
});
```

---

## Tenant Filtering

Restrict API access to users from specific tenants only.

### Use Case

**Scenario:** Your multi-tenant API should only accept tokens from approved customer tenants.

### Implementation

```csharp
builder.Services.AddAuthorization(options =>
{
    string[] allowedTenants =
    {
        "14c2f153-90a7-4689-9db7-9543bf084dad", // Contoso tenant
        "af8cc1a0-d2aa-4ca7-b829-00d361edb652", // Fabrikam tenant
        "979f4440-75dc-4664-b2e1-2cafa0ac67d1"  // Northwind tenant
    };

    options.AddPolicy("AllowedTenantsOnly", policyBuilder =>
    {
        policyBuilder.RequireClaim(
            "http://schemas.microsoft.com/identity/claims/tenantid",
            allowedTenants
        );
    });

    // Apply to all endpoints by default
    options.DefaultPolicy = options.GetPolicy("AllowedTenantsOnly");
});
```

### Configuration-Based Tenant Filtering

**appsettings.json:**
```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "ClientId": "your-api-client-id",
    "AllowedTenants": [
      "14c2f153-90a7-4689-9db7-9543bf084dad",
      "af8cc1a0-d2aa-4ca7-b829-00d361edb652"
    ]
  }
}
```

**Startup:**
```csharp
var allowedTenants = builder.Configuration.GetSection("AzureAd:AllowedTenants")
    .Get<string[]>();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AllowedTenantsOnly", policyBuilder =>
    {
        policyBuilder.RequireClaim(
            "http://schemas.microsoft.com/identity/claims/tenantid",
            allowedTenants ?? Array.Empty<string>()
        );
    });
});
```

### Combined: Scopes + Tenant Filtering

```csharp
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("SecureApiAccess", policyBuilder =>
    {
        // Require specific scope
        policyBuilder.RequireScope("access_as_user");

        // AND require specific tenant
        policyBuilder.RequireClaim(
            "http://schemas.microsoft.com/identity/claims/tenantid",
            allowedTenants
        );
    });
});
```

---

## Best Practices

### ✅ Do's

**1. In Web APIs, always use `[Authorize]` with scope validation:**
```csharp
[Authorize] // Authentication
[RequiredScope("access_as_user")] // Authorization
public class MyController : ControllerBase { }
```

**2. Use configuration for environment-specific scopes:**
```csharp
[RequiredScope(RequiredScopesConfigurationKey = "AzureAd:Scopes")]
```

**3. Apply least privilege:**
```csharp
[HttpGet]
[RequiredScope("read")] // Only read permission needed

[HttpPost]
[RequiredScope("write")] // Write permission for modifications
```

**4. Use policies for complex authorization:**
```csharp
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy =>
    {
        policy.RequireScope("admin");
        policy.RequireClaim("department", "IT");
    });
});
```

**5. Enable detailed error responses in development:**
```csharp
if (builder.Environment.IsDevelopment())
{
    Microsoft.IdentityModel.Logging.IdentityModelEventSource.ShowPII = true;
}
```

### ❌ Don'ts

**1. Don't skip `[Authorize]` when using `RequiredScope`:**
```csharp
// ❌ Wrong - RequiredScope won't work without [Authorize]
[RequiredScope("access_as_user")]
public class MyController : ControllerBase { }

// ✅ Correct
[Authorize]
[RequiredScope("access_as_user")]
public class MyController : ControllerBase { }
```

**2. Don't hardcode tenant IDs in production:**
```csharp
// ❌ Wrong
policyBuilder.RequireClaim("tid", "14c2f153-90a7-4689-9db7-9543bf084dad");

// ✅ Better - use configuration
var tenants = Configuration.GetSection("AllowedTenants").Get<string[]>();
policyBuilder.RequireClaim("tid", tenants);
```

**3. Don't confuse scopes with roles:**
```csharp
// ❌ Wrong - This checks roles claim, not scopes
[RequiredScope("Admin")] // "Admin" is typically a role, not a scope

// ✅ Correct
[RequiredScope("access_as_user")] // Scope
[Authorize(Roles = "Admin")] // Role
```

**4. Don't expose sensitive scope information in error messages (production):**

Configure appropriate logging levels and error handling for production environments.

---

## Troubleshooting

### 403 Forbidden - Missing Scope

**Error:** API returns 403 even with valid token.

**Diagnosis:**
1. Decode token at [jwt.ms](https://jwt.ms)
2. Check `scp` or `scope` claim
3. Verify it matches your `RequiredScope` attribute

**Solution:**
- Ensure client app requests the correct scope when acquiring token
- Verify scope is exposed in API app registration
- Grant admin consent if required

### RequiredScope Not Working

**Symptom:** Attribute seems to be ignored.

**Check:**
1. Did you add `[Authorize]` attribute?
2. Is `app.UseAuthorization()` called after `app.UseAuthentication()`?
3. Is `services.AddAuthorization()` registered?

### Configuration Key Not Found

**Error:** Scope validation fails silently.

**Check:**
```json
{
  "AzureAd": {
    "Scopes": "access_as_user" // Matches RequiredScopesConfigurationKey
  }
}
```

Ensure configuration path matches exactly.

---

## Additional Resources

- [ASP.NET Core Authorization](https://learn.microsoft.com/aspnet/core/security/authorization/introduction)
- [Claims-based Authorization](https://learn.microsoft.com/aspnet/core/security/authorization/claims)
- [Policy-based Authorization](https://learn.microsoft.com/aspnet/core/security/authorization/policies)
- [Microsoft Identity Platform Scopes](https://learn.microsoft.com/azure/active-directory/develop/v2-permissions-and-consent)
- [Protected Web API Overview](https://learn.microsoft.com/azure/active-directory/develop/scenario-protected-web-api-overview)

---

**Last Updated:** October 27, 2025
**Microsoft.Identity.Web Version:** 3.14.1+
