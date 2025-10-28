# Quickstart: Sign in Users in an ASP.NET Core Web App

This guide shows you how to create a web app that signs in users with Microsoft Entra ID (formerly Azure AD) using Microsoft.Identity.Web.

**Time to complete:** ~10 minutes

## Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- A Microsoft Entra ID tenant ([create a free account](https://azure.microsoft.com/free/?WT.mc_id=A261C142F))
- An app registration in your Entra tenant

## Option 1: Create from Template (Fastest)

### 1. Create the project

```bash
dotnet new webapp --auth SingleOrg --name MyWebApp
cd MyWebApp
```

### 2. Configure app registration

Update `appsettings.json` with your app registration details:

```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "your-tenant-id",
    "ClientId": "your-client-id",
    "CallbackPath": "/signin-oidc"
  }
}
```

### 3. Run the application

```bash
dotnet run
```

Navigate to `https://localhost:5001` and click **Sign in**.

✅ **Done!** You now have a working web app that signs in users.

---

## Option 2: Add to Existing Web App

### 1. Install NuGet packages

```bash
dotnet add package Microsoft.Identity.Web
dotnet add package Microsoft.Identity.Web.UI
```

**Current version:** 3.14.1

### 2. Configure authentication in `Program.cs`

```csharp
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;

var builder = WebApplication.CreateBuilder(args);

// Add authentication
builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
                .AddMicrosoftIdentityWebApp(builder.Configuration, "AzureAd")
                .EnableTokenAcquisitionToCallDownstreamApi() // Optional: if calling APIs
                .AddInMemoryTokenCaches(); // For production, use distributed cache

// Add Razor Pages or MVC
builder.Services.AddRazorPages()
    .AddMicrosoftIdentityUI(); // Adds sign-in/sign-out UI

var app = builder.Build();

// Configure middleware
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication(); // ⭐ Add authentication middleware
app.UseAuthorization();

app.MapRazorPages();
app.MapControllers();

app.Run();
```

### 3. Add configuration to `appsettings.json`

```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "common", // or your tenant ID for single-tenant
    "ClientId": "your-client-id-from-app-registration",
    "CallbackPath": "/signin-oidc"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.Identity.Web": "Information"
    }
  }
}
```

**Tenant ID values:**
- `common` - Work/school + personal Microsoft accounts
- `organizations` - Work/school accounts only
- `consumers` - Personal Microsoft accounts only
- `<your-tenant-id>` - Specific tenant only (single-tenant app)

### 4. Protect your pages

**For Razor Pages:**

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

[Authorize] // ⭐ Require authentication
public class IndexModel : PageModel
{
    public void OnGet()
    {
        var userName = User.Identity?.Name;
        var userEmail = User.FindFirst("preferred_username")?.Value;
    }
}
```

**For MVC Controllers:**

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Authorize] // ⭐ Require authentication
public class HomeController : Controller
{
    public IActionResult Index()
    {
        var userName = User.Identity?.Name;
        return View();
    }
}
```

### 5. Add sign-in/sign-out links

**In your layout (`_Layout.cshtml`):**

```html
<ul class="navbar-nav">
    @if (User.Identity?.IsAuthenticated == true)
    {
        <li class="nav-item">
            <span class="nav-link">Hello @User.Identity.Name!</span>
        </li>
        <li class="nav-item">
            <a class="nav-link" asp-area="MicrosoftIdentity" asp-controller="Account" asp-action="SignOut">Sign out</a>
        </li>
    }
    else
    {
        <li class="nav-item">
            <a class="nav-link" asp-area="MicrosoftIdentity" asp-controller="Account" asp-action="SignIn">Sign in</a>
        </li>
    }
</ul>
```

### 6. Run and test

```bash
dotnet run
```

✅ **Success!** Your existing app now supports sign-in.

---

## App Registration Setup

If you haven't created an app registration yet:

### 1. Register your application

1. Sign in to the [Azure portal](https://portal.azure.com)
2. Navigate to **Microsoft Entra ID** > **App registrations** > **New registration**
3. Enter a name (e.g., "My Web App")
4. Select supported account types:
   - **Single tenant** - Users in your organization only
   - **Multi-tenant** - Users in any organization
   - **Multi-tenant + personal** - Users in any organization + personal Microsoft accounts
5. Add a redirect URI: `https://localhost:5001/signin-oidc` (for development)
6. Click **Register**

### 2. Note the application (client) ID

Copy the **Application (client) ID** from the overview page - you'll need this for `ClientId` in `appsettings.json`.

### 3. Note the directory (tenant) ID

Copy the **Directory (tenant) ID** from the overview page - you'll need this for `TenantId` in `appsettings.json`.

---

## Common Configuration Options

### Enable ID token issuance (if needed)

Some scenarios require enabling ID token:

1. In your app registration, go to **Authentication**
2. Under **Implicit grant and hybrid flows**, check **ID tokens**
3. Click **Save**

### Configure logout URL

1. In your app registration, go to **Authentication**
2. Under **Front-channel logout URL**, add: `https://localhost:5001/signout-oidc`
3. Click **Save**

---

## Next Steps

Now that you have a working web app with sign-in:

### Learn More

✅ **[Authorization Guide](../authentication/authorization.md)** - Protect controllers with policies and scopes
✅ **[Customization Guide](../advanced/customization.md)** - OpenID Connect events, login hints, claims transformation
✅ **[Logging & Diagnostics](../advanced/logging.md)** - Troubleshoot authentication issues with correlation IDs

### Advanced Scenarios

✅ **[Call downstream APIs](../calling-downstream-apis/from-web-apps.md)** - Call Microsoft Graph or your own API
✅ **[Configure token cache](../authentication/token-cache/README.md)** - Set up distributed caching for production
✅ **[Handle incremental consent](../advanced/incremental-consent-ca.md)** - Request additional permissions dynamically
✅ **[Deploy to Azure](../deployment/azure-app-service.md)** - Production deployment guidance

## Troubleshooting

### AADSTS50011: No reply address is registered

**Problem:** The redirect URI in your code doesn't match the app registration.

**Solution:** Ensure the redirect URI in your app registration matches your `CallbackPath` (`/signin-oidc` by default).

### AADSTS700016: Application not found

**Problem:** The `ClientId` in your configuration doesn't match any app registration.

**Solution:** Verify you've copied the correct Application (client) ID from your app registration.

### "Authority" configuration error

**Problem:** Missing or invalid `Instance` or `TenantId`.

**Solution:** Ensure `Instance` is `https://login.microsoftonline.com/` and `TenantId` is valid. See [Logging & Diagnostics](../advanced/logging.md) for detailed troubleshooting.

**See more:** [Web App Troubleshooting Guide](../scenarios/web-apps/troubleshooting.md)

---

## Learn More

- [Web Apps Scenario Documentation](../scenarios/web-apps/README.md)
- [Complete Web App Tutorial](https://learn.microsoft.com/azure/active-directory/develop/tutorial-web-app-dotnet-sign-in-users)
- [Microsoft.Identity.Web Samples](https://github.com/Azure-Samples/active-directory-aspnetcore-webapp-openidconnect-v2)