# Quickstart: Protect an ASP.NET Core Web API

This guide shows you how to protect a web API with Microsoft Entra ID (formerly Azure AD) using Microsoft.Identity.Web.

**Time to complete:** ~10 minutes

## Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- A Microsoft Entra ID tenant ([create a free account](https://azure.microsoft.com/free/?WT.mc_id=A261C142F))
- An app registration for your API

## Option 1: Create from Template (Fastest)

### 1. Create the project

```bash
dotnet new webapi --auth SingleOrg --name MyWebApi
cd MyWebApi
```

### 2. Configure app registration

Update `appsettings.json` with your app registration details:

```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "your-tenant-id",
    "ClientId": "your-api-client-id"
  }
}
```

### 3. Run the API

```bash
dotnet run
```

Your API is now protected at `https://localhost:5001`.

✅ **Done!** Requests now require a valid access token.

---

## Option 2: Add to Existing Web API

### 1. Install NuGet package

```bash
dotnet add package Microsoft.Identity.Web
```

**Current version:** 3.14.1

### 2. Configure authentication in `Program.cs`

```csharp
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Web;

var builder = WebApplication.CreateBuilder(args);

// Add authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddMicrosoftIdentityWebApi(builder.Configuration, "AzureAd");

// Add authorization
builder.Services.AddAuthorization();

builder.Services.AddControllers();

var app = builder.Build();

app.UseHttpsRedirection();

app.UseAuthentication(); // ⭐ Add authentication middleware
app.UseAuthorization();

app.MapControllers();

app.Run();
```

### 3. Add configuration to `appsettings.json`

```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "your-tenant-id",
    "ClientId": "your-api-client-id"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.Identity.Web": "Information"
    }
  }
}
```

### 4. Protect your API endpoints

**Require authentication for all endpoints:**

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Authorize] // ⭐ Require valid access token
[ApiController]
[Route("api/[controller]")]
public class WeatherForecastController : ControllerBase
{
    [HttpGet]
    public IEnumerable<WeatherForecast> Get()
    {
        // Access user information
        var userId = User.FindFirst("oid")?.Value;
        var userName = User.Identity?.Name;

        return Enumerable.Range(1, 5).Select(index => new WeatherForecast
        {
            Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            TemperatureC = Random.Shared.Next(-20, 55),
            Summary = "Protected data"
        });
    }
}
```

**Require specific scopes:**

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class TodoController : ControllerBase
{
    [HttpGet]
    [RequiredScope("access_as_user")] // ⭐ Require specific scope
    public IActionResult GetAll()
    {
        return Ok(new[] { "Todo 1", "Todo 2" });
    }

    [HttpPost]
    [RequiredScope("write")] // ⭐ Different scope for write operations
    public IActionResult Create([FromBody] string item)
    {
        return Created("", item);
    }
}
```

### 5. Run and test

```bash
dotnet run
```

Test with a tool like Postman or curl:

```bash
curl -H "Authorization: Bearer YOUR_ACCESS_TOKEN" https://localhost:5001/api/weatherforecast
```

✅ **Success!** Your API now validates bearer tokens.

---

## App Registration Setup

### 1. Register your API

1. Sign in to the [Azure portal](https://portal.azure.com)
2. Navigate to **Microsoft Entra ID** > **App registrations** > **New registration**
3. Enter a name (e.g., "My Web API")
4. Select **Single tenant** (most common for APIs)
5. No redirect URI needed for APIs
6. Click **Register**

### 2. Expose an API scope

1. In your API app registration, go to **Expose an API**
2. Click **Add a scope**
3. Accept the default Application ID URI or customize it (e.g., `api://your-api-client-id`)
4. Add a scope:
   - **Scope name:** `access_as_user`
   - **Who can consent:** Admins and users
   - **Admin consent display name:** "Access My Web API"
   - **Admin consent description:** "Allows the app to access the web API on behalf of the signed-in user"
5. Click **Add scope**

### 3. Note the Application ID

Copy the **Application (client) ID** - this is your `ClientId` in `appsettings.json`.

---

## Create a Client App Registration (For Testing)

To call your API, you need a client app:

### 1. Register a client application

1. In **Microsoft Entra ID** > **App registrations**, create another registration
2. Name it (e.g., "My API Client")
3. Select account types
4. Add redirect URI: `https://localhost:7000/signin-oidc` (if it's a web app)
5. Click **Register**

### 2. Grant API permissions

1. In the client app registration, go to **API permissions**
2. Click **Add a permission** > **My APIs**
3. Select your API registration
4. Check the `access_as_user` scope
5. Click **Add permissions**
6. Click **Grant admin consent** (if required)

### 3. Create a client secret (for confidential clients)

1. Go to **Certificates & secrets**
2. Click **New client secret**
3. Add a description and expiration
4. Click **Add**
5. **Copy the secret value immediately** - you won't be able to see it again

---

## Test Your Protected API

### Using Postman

1. Create a new request in Postman
2. Set up OAuth 2.0 authentication:
   - **Grant Type:** Authorization Code (for user context) or Client Credentials (for app context)
   - **Auth URL:** `https://login.microsoftonline.com/{tenant-id}/oauth2/v2.0/authorize`
   - **Access Token URL:** `https://login.microsoftonline.com/{tenant-id}/oauth2/v2.0/token`
   - **Client ID:** Your client app's client ID
   - **Client Secret:** Your client app's secret
   - **Scope:** `api://your-api-client-id/access_as_user`
3. Click **Get New Access Token**
4. Use the token to call your API

### Using code (C# example)

```csharp
// In a console app or client application
using Microsoft.Identity.Client;

var app = ConfidentialClientApplicationBuilder
    .Create("client-app-id")
    .WithClientSecret("client-secret")
    .WithAuthority("https://login.microsoftonline.com/{tenant-id}")
    .Build();

var result = await app.AcquireTokenForClient(
    new[] { "api://your-api-client-id/.default" }
).ExecuteAsync();

var accessToken = result.AccessToken;

// Use the token to call your API
using var client = new HttpClient();
client.DefaultRequestHeaders.Authorization =
    new AuthenticationHeaderValue("Bearer", accessToken);

var response = await client.GetAsync("https://localhost:5001/api/weatherforecast");
```

---

## Common Configuration Options

### Require specific scopes in configuration

Instead of using the `[RequiredScope]` attribute, configure required scopes globally:

```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "your-tenant-id",
    "ClientId": "your-api-client-id",
    "Scopes": "access_as_user"
  }
}
```

### Accept tokens from multiple tenants

For multi-tenant APIs:

```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "common",
    "ClientId": "your-api-client-id"
  }
}
```

### Configure token validation

```csharp
builder.Services.AddMicrosoftIdentityWebApiAuthentication(builder.Configuration)
    .EnableTokenAcquisitionToCallDownstreamApi() // If your API calls other APIs
    .AddInMemoryTokenCaches();
```

---

## Next Steps

Now that you have a protected API:

### Learn More

✅ **[Authorization Guide](../authentication/authorization.md)** - RequiredScope attribute, authorization policies, tenant filtering
✅ **[Customization Guide](../advanced/customization.md)** - Configure JWT bearer options and validation parameters
✅ **[Logging & Diagnostics](../advanced/logging.md)** - Troubleshoot authentication issues with correlation IDs

### Advanced Scenarios

✅ **[Call downstream APIs](../calling-downstream-apis/from-web-apis.md)** - Call Microsoft Graph or other APIs on behalf of users
✅ **[Configure token cache](../authentication/token-cache/README.md)** - Production cache strategies for OBO scenarios
✅ **[Long-running processes](../scenarios/web-apis/long-running-processes.md)** - Handle background jobs with OBO tokens
✅ **[Deploy behind API Gateway](../advanced/api-gateways.md)** - Azure API Management, Azure Front Door, Application Gateway

## Troubleshooting

### 401 Unauthorized

**Problem:** API returns 401 even with a token.

**Possible causes:**
- Token audience (`aud` claim) doesn't match your API's `ClientId`
- Token is expired
- Token is for the wrong tenant
- Required scope is missing

**Solution:** Decode the token at [jwt.ms](https://jwt.ms) and verify the claims. See [Logging & Diagnostics](../advanced/logging.md) for detailed troubleshooting.

### AADSTS50013: Invalid signature

**Problem:** Token signature validation fails.

**Solution:** Ensure your `TenantId` and `ClientId` are correct. The token must be issued by the expected authority. Enable detailed logging to see validation errors.

### Scopes not found in token

**Problem:** `[RequiredScope]` attribute fails.

**Solution:**
1. Verify the client app has permission to the scope
2. Ensure admin consent was granted (if required)
3. See [Authorization Guide](../authentication/authorization.md) for complete scope validation patterns
3. Check that the scope is requested when acquiring the token (e.g., `api://your-api/.default` or specific scopes)

**See more:** [Web API Troubleshooting Guide](../scenarios/web-apis/troubleshooting.md)

---

## Learn More

- [Web API Scenario Documentation](../scenarios/web-apis/README.md)
- [Protected Web API Tutorial](https://learn.microsoft.com/azure/active-directory/develop/tutorial-web-api-dotnet-protect-endpoint)
- [API Samples](https://github.com/Azure-Samples/active-directory-dotnet-native-aspnetcore-v2)
