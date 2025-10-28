# Customizing Authentication with Microsoft.Identity.Web

This guide explains how to customize authentication behavior in ASP.NET Core applications using Microsoft.Identity.Web while preserving the library's built-in security features.

---

## 📋 Table of Contents

- [Overview](#overview)
- [Configuration Customization](#configuration-customization)
- [Event Handler Customization](#event-handler-customization)
- [Token Acquisition Customization](#token-acquisition-customization)
- [UI Customization](#ui-customization)
- [Sign-In Experience Customization](#sign-in-experience-customization)
- [Best Practices](#best-practices)

---

## Overview

Microsoft.Identity.Web provides secure defaults for authentication and authorization. However, you can customize many aspects while maintaining security:

### What Can Be Customized?

| Area | Customization Options |
|------|----------------------|
| **Configuration** | All `MicrosoftIdentityOptions`, `OpenIdConnectOptions`, `JwtBearerOptions` properties |
| **Events** | OpenID Connect events (`OnTokenValidated`, `OnRedirectToIdentityProvider`, etc.) |
| **Token Acquisition** | Correlation IDs, extra query parameters |
| **Claims** | Add custom claims to `ClaimsPrincipal` |
| **UI** | Sign-out pages, redirect behavior |
| **Sign-In** | Login hints, domain hints |

### Customization Methods

**Two approaches:**

1. **`Configure<TOptions>`** - Configures options before they're used
2. **`PostConfigure<TOptions>`** - Configures options after all `Configure` calls

**Order of execution:**
```
Configure → Configure → ... → PostConfigure → PostConfigure → ... → Options used
```

---

## Configuration Customization

### Understanding Configuration Mapping

The `"AzureAd"` section in `appsettings.json` maps to multiple classes:

- [`MicrosoftIdentityOptions`](https://learn.microsoft.com/dotnet/api/microsoft.identity.web.microsoftidentityoptions)
- [`ConfidentialClientApplicationOptions`](https://learn.microsoft.com/dotnet/api/microsoft.identity.client.confidentialclientapplicationoptions)

You can use any property from these classes in your configuration.

### Pattern 1: Configure MicrosoftIdentityOptions

```csharp
using Microsoft.Identity.Web;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"));

// Customize Microsoft Identity options
builder.Services.Configure<MicrosoftIdentityOptions>(options =>
{
    // Enable PII logging (development only!)
    options.EnablePiiLogging = true;

    // Custom client capabilities
    options.ClientCapabilities = new[] { "CP1", "CP2" };

    // Override token validation parameters
    options.TokenValidationParameters.ValidateLifetime = true;
    options.TokenValidationParameters.ClockSkew = TimeSpan.FromMinutes(5);
});

var app = builder.Build();
```

### Pattern 2: Configure OpenIdConnectOptions (Web Apps)

```csharp
builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"));

// Customize OpenIdConnect options
builder.Services.Configure<OpenIdConnectOptions>(
    OpenIdConnectDefaults.AuthenticationScheme,
    options =>
{
    // Override response type
    options.ResponseType = "code id_token";

    // Add extra scopes
    options.Scope.Add("offline_access");
    options.Scope.Add("profile");

    // Customize token validation
    options.TokenValidationParameters.NameClaimType = "preferred_username";
    options.TokenValidationParameters.RoleClaimType = "roles";

    // Set redirect URI
    options.CallbackPath = "/signin-oidc";

    // Configure cookie options
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
});
```

### Pattern 3: Configure JwtBearerOptions (Web APIs)

```csharp
using Microsoft.AspNetCore.Authentication.JwtBearer;

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));

// Customize JWT Bearer options
builder.Services.Configure<JwtBearerOptions>(
    JwtBearerDefaults.AuthenticationScheme,
    options =>
{
    // Customize audience validation
    options.TokenValidationParameters.ValidAudiences = new[]
    {
        "api://your-api-client-id",
        "https://your-api.com"
    };

    // Set custom claim mappings
    options.TokenValidationParameters.NameClaimType = "name";
    options.TokenValidationParameters.RoleClaimType = "roles";

    // Customize token validation
    options.TokenValidationParameters.ValidateLifetime = true;
    options.TokenValidationParameters.ClockSkew = TimeSpan.Zero; // No tolerance
});
```

### Pattern 4: Configure Cookie Options

```csharp
using Microsoft.AspNetCore.Authentication.Cookies;

// Configure cookie policy
builder.Services.Configure<CookiePolicyOptions>(options =>
{
    options.MinimumSameSitePolicy = SameSiteMode.Lax;
    options.Secure = CookieSecurePolicy.Always;
    options.HttpOnly = Microsoft.AspNetCore.CookiePolicy.HttpOnlyPolicy.Always;
});

// Configure cookie authentication options
builder.Services.Configure<CookieAuthenticationOptions>(
    CookieAuthenticationDefaults.AuthenticationScheme,
    options =>
{
    options.Cookie.Name = "MyApp.Auth";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.ExpireTimeSpan = TimeSpan.FromHours(1);
    options.SlidingExpiration = true;
});
```

---

## Event Handler Customization

OpenID Connect and JWT Bearer authentication provide events you can hook into. Microsoft.Identity.Web sets up event handlers—you can extend them without losing built-in functionality.

### Critical Pattern: Preserve Existing Handlers

**❌ Wrong - Overwrites Microsoft.Identity.Web's handler:**
```csharp
services.Configure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
{
    options.Events.OnTokenValidated = async context =>
    {
        // Your code - but you LOST the built-in validation!
        await Task.CompletedTask;
    };
});
```

**✅ Correct - Chains with existing handler:**
```csharp
services.Configure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
{
    var existingOnTokenValidatedHandler = options.Events.OnTokenValidated;

    options.Events.OnTokenValidated = async context =>
    {
        // Call Microsoft.Identity.Web's handler FIRST
        await existingOnTokenValidatedHandler(context);

        // Then your custom code
        // (executes AFTER built-in security checks)
        var identity = context.Principal.Identity as ClaimsIdentity;
        identity?.AddClaim(new Claim("custom_claim", "custom_value"));
    };
});
```

### Common Event Scenarios

#### Add Custom Claims After Token Validation

**Web API example:**

```csharp
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Security.Claims;

builder.Services.Configure<JwtBearerOptions>(
    JwtBearerDefaults.AuthenticationScheme,
    options =>
{
    var existingHandler = options.Events.OnTokenValidated;

    options.Events.OnTokenValidated = async context =>
    {
        // Preserve built-in validation
        await existingHandler(context);

        // Add custom claims
        var identity = context.Principal.Identity as ClaimsIdentity;

        // Example: Add department claim from database
        var userObjectId = context.Principal.FindFirst("oid")?.Value;
        if (!string.IsNullOrEmpty(userObjectId))
        {
            var department = await GetUserDepartment(userObjectId);
            identity?.AddClaim(new Claim("department", department));
        }

        // Example: Add application-specific role
        var email = context.Principal.FindFirst("email")?.Value;
        if (email?.EndsWith("@admin.com") == true)
        {
            identity?.AddClaim(new Claim(ClaimTypes.Role, "SuperAdmin"));
        }
    };
});
```

**Web App example:**

```csharp
using Microsoft.AspNetCore.Authentication.OpenIdConnect;

builder.Services.Configure<OpenIdConnectOptions>(
    OpenIdConnectDefaults.AuthenticationScheme,
    options =>
{
    var existingHandler = options.Events.OnTokenValidated;

    options.Events.OnTokenValidated = async context =>
    {
        // Preserve built-in processing
        await existingHandler(context);

        // Call Microsoft Graph to get additional user data
        var graphClient = context.HttpContext.RequestServices
            .GetRequiredService<GraphServiceClient>();

        var user = await graphClient.Me.GetAsync();

        var identity = context.Principal.Identity as ClaimsIdentity;
        identity?.AddClaim(new Claim("jobTitle", user?.JobTitle ?? ""));
        identity?.AddClaim(new Claim("department", user?.Department ?? ""));
    };
});
```

#### Add Query Parameters to Authorization Request

```csharp
builder.Services.Configure<OpenIdConnectOptions>(
    OpenIdConnectDefaults.AuthenticationScheme,
    options =>
{
    var existingHandler = options.Events.OnRedirectToIdentityProvider;

    options.Events.OnRedirectToIdentityProvider = async context =>
    {
        // Preserve existing behavior
        if (existingHandler != null)
        {
            await existingHandler(context);
        }

        // Add custom query parameters
        context.ProtocolMessage.Parameters.Add("slice", "testslice");
        context.ProtocolMessage.Parameters.Add("custom_param", "custom_value");

        // Conditional parameters based on request
        if (context.HttpContext.Request.Query.ContainsKey("prompt"))
        {
            context.ProtocolMessage.Prompt = context.HttpContext.Request.Query["prompt"];
        }
    };
});
```

#### Customize Authentication Failure Handling

```csharp
builder.Services.Configure<OpenIdConnectOptions>(
    OpenIdConnectDefaults.AuthenticationScheme,
    options =>
{
    options.Events.OnAuthenticationFailed = async context =>
    {
        // Log the error
        var logger = context.HttpContext.RequestServices
            .GetRequiredService<ILogger<Program>>();
        logger.LogError(context.Exception, "Authentication failed");

        // Customize error response
        context.Response.StatusCode = 401;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync($$"""
            {
                "error": "authentication_failed",
                "error_description": "{{context.Exception.Message}}"
            }
            """);

        context.HandleResponse(); // Suppress default error handling
    };
});
```

#### Handle Access Denied

```csharp
builder.Services.Configure<OpenIdConnectOptions>(
    OpenIdConnectDefaults.AuthenticationScheme,
    options =>
{
    options.Events.OnAccessDenied = async context =>
    {
        // User denied consent
        context.Response.Redirect("/Home/AccessDenied");
        context.HandleResponse();
        await Task.CompletedTask;
    };
});
```

---

## Token Acquisition Customization

### Using ITokenAcquisition with Custom Options

```csharp
using Microsoft.Identity.Web;

public class TodoListController : ControllerBase
{
    private readonly ITokenAcquisition _tokenAcquisition;

    public TodoListController(ITokenAcquisition tokenAcquisition)
    {
        _tokenAcquisition = tokenAcquisition;
    }

    [HttpGet]
    public async Task<IActionResult> GetTodos(Guid correlationId)
    {
        // Customize token acquisition
        var tokenAcquisitionOptions = new TokenAcquisitionOptions
        {
            // Set correlation ID for tracing
            CorrelationId = correlationId,

            // Add extra query parameters
            ExtraQueryParameters = new Dictionary<string, string>
            {
                { "slice", "test_slice" },
                { "dc", "westus2" }
            }
        };

        string token = await _tokenAcquisition.GetAccessTokenForUserAsync(
            new[] { "user.read" },
            tokenAcquisitionOptions: tokenAcquisitionOptions);

        // Use token to call API
        return Ok(await CallApiWithToken(token));
    }
}
```

### Using IDownstreamApi with Custom Options

```csharp
using Microsoft.Identity.Abstractions;

public class TodoListController : ControllerBase
{
    private readonly IDownstreamApi _downstreamApi;

    public TodoListController(IDownstreamApi downstreamApi)
    {
        _downstreamApi = downstreamApi;
    }

    [HttpGet("{id}")]
    public async Task<ActionResult> GetTodo(int id, Guid correlationId)
    {
        var result = await _downstreamApi.GetForUserAsync<Todo>(
            "TodoListService",
            options =>
            {
                options.RelativePath = $"api/todolist/{id}";

                // Customize token acquisition
                options.TokenAcquisitionOptions = new TokenAcquisitionOptions
                {
                    CorrelationId = correlationId,
                    ExtraQueryParameters = new Dictionary<string, string>
                    {
                        { "slice", "test_slice" }
                    }
                };
            });

        return Ok(result);
    }
}
```

---

## UI Customization

### Redirect to Specific Page After Sign-In

Use the `redirectUri` parameter:

```html
<!-- Razor view -->
<a href="/MicrosoftIdentity/Account/SignIn?redirectUri=/Dashboard">Sign In</a>

<!-- Or in controller -->
[HttpGet]
public IActionResult SignInToDashboard()
{
    return RedirectToAction("SignIn", "Account", new
    {
        area = "MicrosoftIdentity",
        redirectUri = "/Dashboard"
    });
}
```

### Customize Signed-Out Page

**Option 1: Override the Razor Page**

Create `Areas/MicrosoftIdentity/Pages/Account/SignedOut.cshtml`:

```cshtml
@page
@model Microsoft.Identity.Web.UI.Areas.MicrosoftIdentity.Pages.Account.SignedOutModel
@{
    ViewData["Title"] = "Signed out";
}

<div class="container text-center mt-5">
    <h1>You have been signed out</h1>
    <p>Thank you for using our application.</p>
    <a asp-area="" asp-controller="Home" asp-action="Index" class="btn btn-primary">
        Return to Home
    </a>
</div>
```

**Option 2: Redirect to Custom Page**

```csharp
builder.Services.Configure<OpenIdConnectOptions>(
    OpenIdConnectDefaults.AuthenticationScheme,
    options =>
{
    options.Events.OnSignedOutCallbackRedirect = context =>
    {
        context.Response.Redirect("/Home/SignedOut");
        context.HandleResponse();
        return Task.CompletedTask;
    };
});
```

---

## Sign-In Experience Customization

### Login Hint & Domain Hint

Streamline the sign-in experience by pre-populating usernames and directing to specific tenants.

#### What Are Hints?

| Hint | Purpose | Example |
|------|---------|---------|
| **loginHint** | Pre-populate username/email field | `"user@contoso.com"` |
| **domainHint** | Direct to specific tenant login page | `"contoso.com"` |

#### Usage Patterns

**Pattern 1: Controller-Based**

```csharp
using Microsoft.AspNetCore.Mvc;

public class AuthController : Controller
{
    [HttpGet]
    public IActionResult SignIn()
    {
        // Standard sign-in
        return RedirectToAction("SignIn", "Account", new
        {
            area = "MicrosoftIdentity",
            redirectUri = "/Dashboard"
        });
    }

    [HttpGet]
    public IActionResult SignInWithLoginHint()
    {
        // Pre-populate username
        return RedirectToAction("SignIn", "Account", new
        {
            area = "MicrosoftIdentity",
            redirectUri = "/Dashboard",
            loginHint = "user@contoso.com"
        });
    }

    [HttpGet]
    public IActionResult SignInWithDomainHint()
    {
        // Direct to Contoso tenant
        return RedirectToAction("SignIn", "Account", new
        {
            area = "MicrosoftIdentity",
            redirectUri = "/Dashboard",
            domainHint = "contoso.com"
        });
    }

    [HttpGet]
    public IActionResult SignInWithBothHints()
    {
        // Pre-populate AND direct to tenant
        return RedirectToAction("SignIn", "Account", new
        {
            area = "MicrosoftIdentity",
            redirectUri = "/Dashboard",
            loginHint = "user@contoso.com",
            domainHint = "contoso.com"
        });
    }
}
```

**Pattern 2: View-Based**

```html
<div class="sign-in-options">
    <h2>Sign In Options</h2>

    <!-- Standard sign-in -->
    <a href="/MicrosoftIdentity/Account/SignIn?redirectUri=/Dashboard"
       class="btn btn-primary">
        Sign In
    </a>

    <!-- With login hint -->
    <a href="/MicrosoftIdentity/Account/SignIn?redirectUri=/Dashboard&loginHint=user@contoso.com"
       class="btn btn-secondary">
        Sign In as user@contoso.com
    </a>

    <!-- With domain hint -->
    <a href="/MicrosoftIdentity/Account/SignIn?redirectUri=/Dashboard&domainHint=contoso.com"
       class="btn btn-secondary">
        Sign In (Contoso)
    </a>
</div>
```

**Pattern 3: Programmatic with OnRedirectToIdentityProvider**

```csharp
builder.Services.Configure<OpenIdConnectOptions>(
    OpenIdConnectDefaults.AuthenticationScheme,
    options =>
{
    var existingHandler = options.Events.OnRedirectToIdentityProvider;

    options.Events.OnRedirectToIdentityProvider = async context =>
    {
        if (existingHandler != null)
        {
            await existingHandler(context);
        }

        // Add hints based on application logic
        if (context.HttpContext.Request.Query.TryGetValue("tenant", out var tenant))
        {
            context.ProtocolMessage.DomainHint = tenant;
        }

        // Get suggested user from cookie or session
        var suggestedUser = context.HttpContext.Request.Cookies["LastSignedInUser"];
        if (!string.IsNullOrEmpty(suggestedUser))
        {
            context.ProtocolMessage.LoginHint = suggestedUser;
        }
    };
});
```

#### Use Cases

**E-commerce Platform:**
```csharp
// Pre-fill returning customer email
loginHint = customerEmail
```

**B2B Application:**
```csharp
// Direct to customer's tenant
domainHint = customerDomain
```

**Multi-Tenant SaaS:**
```csharp
// Route based on subdomain
domainHint = GetTenantFromSubdomain(Request.Host)
```

---

## Best Practices

### ✅ Do's

**1. Always preserve existing event handlers:**
```csharp
var existingHandler = options.Events.OnTokenValidated;
options.Events.OnTokenValidated = async context =>
{
    await existingHandler(context); // Call Microsoft.Identity.Web's handler
    // Your custom code
};
```

**2. Use correlation IDs for tracing:**
```csharp
var tokenOptions = new TokenAcquisitionOptions
{
    CorrelationId = Activity.Current?.Id ?? Guid.NewGuid()
};
```

**3. Validate custom claims:**
```csharp
var department = context.Principal.FindFirst("department")?.Value;
if (!IsValidDepartment(department))
{
    throw new UnauthorizedAccessException("Invalid department");
}
```

**4. Log customization errors:**
```csharp
try
{
    // Custom logic
}
catch (Exception ex)
{
    logger.LogError(ex, "Custom authentication logic failed");
    throw;
}
```

**5. Test both success and failure paths:**
```csharp
// Test with valid tokens
// Test with missing claims
// Test with expired tokens
// Test with wrong audience
```

### ❌ Don'ts

**1. Don't skip Microsoft.Identity.Web's event handlers:**
```csharp
// ❌ Wrong - loses built-in security checks
options.Events.OnTokenValidated = async context => { /* your code */ };

// ✅ Correct - preserves security
var existing = options.Events.OnTokenValidated;
options.Events.OnTokenValidated = async context =>
{
    await existing(context);
    /* your code */
};
```

**2. Don't enable PII logging in production:**
```csharp
// ❌ Wrong
options.EnablePiiLogging = true; // In production!

// ✅ Correct
if (builder.Environment.IsDevelopment())
{
    options.EnablePiiLogging = true;
}
```

**3. Don't bypass token validation:**
```csharp
// ❌ Wrong - insecure!
options.TokenValidationParameters.ValidateLifetime = false;
options.TokenValidationParameters.ValidateAudience = false;

// ✅ Correct - maintain security
options.TokenValidationParameters.ValidateLifetime = true;
options.TokenValidationParameters.ClockSkew = TimeSpan.FromMinutes(5);
```

**4. Don't hardcode sensitive values:**
```csharp
// ❌ Wrong
options.ClientSecret = "mysecret123";

// ✅ Correct
options.ClientSecret = builder.Configuration["AzureAd:ClientSecret"];
```

**5. Don't modify authentication in middleware:**
```csharp
// ❌ Wrong - configure in Startup, not middleware
app.Use(async (context, next) =>
{
    // Modifying auth options here is too late!
});
```

---

## Troubleshooting

### Customization Not Working

**Check execution order:**
1. `AddMicrosoftIdentityWebApp` / `AddMicrosoftIdentityWebApi` sets defaults
2. Your `Configure` calls run
3. `PostConfigure` calls run (if any)
4. Options are used

**Solution:** Use `PostConfigure` if `Configure` isn't working:
```csharp
services.PostConfigure<OpenIdConnectOptions>(
    OpenIdConnectDefaults.AuthenticationScheme,
    options => { /* your changes */ }
);
```

### Custom Claims Not Appearing

**Check:**
1. Is `OnTokenValidated` handler chained correctly?
2. Is authentication successful before adding claims?
3. Are claims added to the correct identity?

**Debug:**
```csharp
var claims = context.Principal.Claims.ToList();
logger.LogInformation($"Claims count: {claims.Count}");
foreach (var claim in claims)
{
    logger.LogInformation($"{claim.Type}: {claim.Value}");
}
```

### Events Not Firing

**Verify middleware order:**
```csharp
app.UseAuthentication(); // Must be first
app.UseAuthorization();  // Must be second
app.MapControllers();    // Then endpoints
```

---

## Additional Resources

- [ASP.NET Core Authentication](https://learn.microsoft.com/aspnet/core/security/authentication/)
- [OpenID Connect Events](https://learn.microsoft.com/dotnet/api/microsoft.aspnetcore.authentication.openidconnect.openidconnectevents)
- [JWT Bearer Events](https://learn.microsoft.com/dotnet/api/microsoft.aspnetcore.authentication.jwtbearer.jwtbearerevents)
- [Claims Transformation](https://learn.microsoft.com/aspnet/core/security/authentication/claims)

---

**Last Updated:** October 27, 2025
**Microsoft.Identity.Web Version:** 3.14.1+
