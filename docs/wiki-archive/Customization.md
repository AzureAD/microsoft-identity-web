# Customization in Configuration

The "AzureAd" (or "AzureADB2C") section of the appsettings.json is mapped to several classes:
- [MicrosoftIdentityOptions](https://docs.microsoft.com/dotnet/api/microsoft.identity.web.microsoftidentityoptions?view=azure-dotnet-preview)
- [ConfidentialClientApplicationOptions](https://docs.microsoft.com/en-us/dotnet/api/microsoft.identity.client.confidentialclientapplicationoptions?view=azure-dotnet-preview)

You can therefore use any of these settings in appsettings.json.

# Customization in the Startup.cs

If you want to customize options, like `OpenIdConnectOptions` or `JwtBearerOptions`, but still want to benefit from the implementation provided by Microsoft Identity Web; you can do so by using `Configure` and `PostConfigure` methods in `Startup.cs`.

Let's take, for example, the `AddMicrosoftIdentityWebApi` or `AddMicrosoftIdentityWebApiAuthentication` methods (used to be `AddProtectedWebApi` in Microsoft Identity Web 0.1.x). In it, you'll see this event set up:

```csharp
options.Events.OnTokenValidated = async context =>
{
    // This check is required to ensure that the web API only accepts tokens from tenants where it has been consented and provisioned.
    if (!context.Principal.Claims.Any(x => x.Type == ClaimConstants.Scope)
    && !context.Principal.Claims.Any(y => y.Type == ClaimConstants.Scp)
    && !context.Principal.Claims.Any(y => y.Type == ClaimConstants.Roles))
    {
         throw new UnauthorizedAccessException("Neither scope or roles claim was found in the bearer token.");
    }

    await Task.FromResult(0);
};
```

Say you want to augment the current `ClaimsPrincipal` by adding claims to it, and you have to do it on `OnTokenValidated`. However, you don't want to lose the `UnauthorizedAccessException` check existing in the event. To do so, in your `Startup.cs`, you'd have:

```csharp
services.AddMicrosoftIdentityWebApiAuthentication(Configuration);
services.Configure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
{
  var existingOnTokenValidatedHandler = options.Events.OnTokenValidated ;
  options.Events.OnTokenValidated = async context =>
  {
       await existingOnTokenValidatedHandler(context);
      // Your code to add extra claims that will be executed after the current event implementation.
  }
});

```

Other types of options can be customized in similar fashion:

### Cookie related options

  ```csharp
  services.Configure<CookiePolicyOptions>(options =>
  {
      // Custom code here.
  });
  ```

  ```csharp
  services.Configure<CookieAuthenticationOptions>(CookieAuthenticationDefaults.AuthenticationScheme, options =>
  {
      // Custom code here.
  });
  ```

### `OpenIdConnectOptions`

  ```csharp
  services.Configure<OpenIdConnectOptions>(OpenIdConnectDefaults.AuthenticationScheme, options =>
  {
      // Custom code here.
  });
  ```

If you want to override the default [`response_type`](https://datatracker.ietf.org/doc/html/rfc6749#section-3.1.1) of `code`, you can override it.
In the code, for example:
```CSharp
services.Configure<OpenIdConnectOptions>(OpenIdConnectDefaults.AuthenticationScheme, options =>
{
   options.ResponseType = "code id_token";
});
```

  For example to add extra query parameters to the URL sent to Azure AD:

  ```CSharp
  services.Configure<OpenIdConnectOptions>(OpenIdConnectDefaults.AuthenticationScheme, options =>
  {
   var previous = options.Events.OnRedirectToIdentityProvider;
   options.Events.OnRedirectToIdentityProvider = async context =>
   {
    if (previous != null)
    {
     await previous(context);
    }
    context.ProtocolMessage.Parameters.Add("slice", "testslice");
   };
  });
  ```

#### How to query Microsoft Graph on token validated

```CSharp
// Sign-in users with the Microsoft identity platform
services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
        .AddMicrosoftIdentityWebApp(
           options =>
           {
            Configuration.Bind("AzureAd", options);
            options.Events = new OpenIdConnectEvents();
            var previousOnTokenValidatedHandler = options.Events.OnTokenValidated;
            options.Events.OnTokenValidated = async context =>
            {
             // Let Microsoft.Identity.Web process the token
             await previousOnTokenValidatedHandler(context).ConfigureAwait(false);

             // Calls method to process groups overage claim.
             var overageGroupClaims = await GraphHelper.GetSignedInUsersGroups(context);             
            };
           })
           .EnableTokenAcquisitionToCallDownstreamApi(options => Configuration.Bind("AzureAd", options), initialScopes)
             .AddMicrosoftGraph(Configuration.GetSection("GraphAPI"))
             .AddInMemoryTokenCaches();
```

For more details, see the active-directory-aspnetcore-webapp-openidconnect-v2 sample in chapter [5-2-Groups/Startup.cs#L41-L56](https://github.com/Azure-Samples/active-directory-aspnetcore-webapp-openidconnect-v2/blob/012df8a0682948226a5916fc751dcdddc1c98558/5-WebApp-AuthZ/5-2-Groups/Startup.cs#L41-L56).


### `MicrosoftIdentityOptions`

  ```csharp
  services.Configure<MicrosoftIdentityOptions>(options =>
  {
      // Custom code here.
  });
  ```

When configuring options, verify that the correct authentication scheme is passed in, or none at all. Additionally, the middleware configuration methods are invoked in the order in which they were called, with `PostConfigure` methods executing after all the `Configure` methods.

#  Customizations to acquire tokens

## Using the ITokenAcquisition interface

You have, from Microsoft.Identity.Web 1.0.0, the possibilty of passing tokenAcquisitionOptions to the ITokenAcquisition.GetAccessTokenForUserAsync() and .GetAccessTokenForAppAsync() methods in order to specify a `CorrelationId`, or extra query parameters.

```CSharp
 public async Task<IEnumerable<Todo>> GetAsync()
 {
  TokenAcquisitionOptions tokenAcquisitionOptions = new TokenAcquisitionOptions()
  {
   CorrelationId = correlationIdYouHaveReceived,
   ExtraQueryParameters = new Dictionary<string, string> 
     { { "slide", "test_slice" } }
  };

  string token = await _tokenAcquisition.GetAccessTokenForUserAsync(new string[] { "user.read" },
                tokenAcquisitionOptions: tokenAcquisitionOptions)
            .ConfigureAwait(false);
  // Do something with the token
 }
```

## Using the IDownstream API interface

If you are using the `IDownstreamApi` interface, you'll have the same capability in the `TokenAcquisitionOptions` member of the `DownstreamApiOptions` passed to the delegate that enable you to configure the web API to call:

```CSharp
public async Task<ActionResult> Details(int id)
{
 var value = await _downstreamWebApi.CallWebApiForUserAsync<object, Todo>(
    ServiceName,
    null,
    options =>
    {
     options.HttpMethod = HttpMethod.Get;
     options.RelativePath = $"api/todolist/{id}";
     options.TokenAcquisitionOptions.CorrelationId = correlationId;
     options.TokenAcquisitionOptions.ExtraQueryParameters = 
       new Dictionary<string, string> { { "slide", "test_slice" } };
    });
 return View(value);
}
```

# UI Customization

## Redirect to a particular page after sign-in

After sign-in you can redirect to a particular page by precising the `redirectUri` parameter of the `/MicrosoftIdentity/Account/SignIn` action:

```html
<a href="/MicrosoftIdentity/Account/SignIn?redirectUri=/TodoList">Sign In</a>
```

## Implement a custom `SignedOut` page

The Microsoft Identity Web UI is implemented with MVC, which can cause issues for developers using Blazor, especially with navigation components.
Adding a `Areas/MicrosoftIdentity/Pages/Account/SignedOut.cshtml` file will enable you to override the default `/MicrosoftIdentity/Account/SignedOut` page.

Also, to override the markup of the `SignedOut.html` page, the page can be overridden, as shown [here](https://docs.microsoft.com/en-us/aspnet/core/razor-pages/ui-class?view=aspnetcore-3.1&tabs=visual-studio#override-views-partial-views-and-pages). 

See also the discussions in issue [#758](https://github.com/AzureAD/microsoft-identity-web/issues/758)


# Customization to sign in experience

Microsoft Identity Web has the ability to pass optional login and domain hints to Azure Active Directory during authentication flows. These parameters streamline the sign-in experience by allowing applications to pre-suggest user accounts and direct authentication requests to specific organizational tenants. When implemented, these hints can reduce authentication steps, minimize user input requirements, and provide a more personalized user experience.

## Overview

Two new optional parameters have been added:

- **loginHint**: Pre-populates the username/email field in the authentication UI and URI
- **domainHint**: Directs the authentication to a specific tenant's login page

## Implementation Example
Here is an example controller
```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web;

namespace WebApp_OpenIDConnect_DotNet.Controllers
{
    public class AuthDemoController : Controller
    {
        [HttpGet] // Basic sign-in without hints
        public IActionResult StandardSignIn()
        {
            return RedirectToAction("SignIn", "Account", new 
            { 
                area = "MicrosoftIdentity", 
                redirectUri = "/AuthDemo/Profile" 
            });
        }

        [HttpGet] // Pre-populate the email field with a specific account
        public IActionResult SignInWithLoginHint()
        {
            return RedirectToAction("SignIn", "Account", new 
            { 
                area = "MicrosoftIdentity", 
                redirectUri = "/AuthDemo/Profile",
                loginHint = "user@contoso.com" 
            });
        }

        [HttpGet] // Direct to a specific organization's login page
        public IActionResult SignInWithDomainHint()
        {
            return RedirectToAction("SignIn", "Account", new 
            { 
                area = "MicrosoftIdentity", 
                redirectUri = "/AuthDemo/Profile",
                domainHint = "contoso.com" 
            });
        }

        [HttpGet] // Both pre-populate email and direct to specific tenant
        public IActionResult SignInWithBothHints()
        {
            return RedirectToAction("SignIn", "Account", new 
            { 
                area = "MicrosoftIdentity", 
                redirectUri = "/AuthDemo/Profile",
                loginHint = "user@contoso.com",
                domainHint = "contoso.com"
            });
        }

        [HttpGet] // Page to show after successful sign-in
        public IActionResult Profile()
        {
            return View();
        }
    }
}
```
Here is a sample view for using the above controller.
```html
@{
    ViewData["Title"] = "Authentication Demo";
}

<div class="container">
    <h1 class="mb-4">Authentication Experience Demo</h1>
    <div class="row">
        <div class="col-md-8">
            <div class="card mb-4">
                <div class="card-header">
                    <h2>Customized Sign-in Options</h2>
                </div>
                <div class="card-body">
                    <p class="lead">Choose a sign-in method to test different authentication experiences:</p>
                    
                    <div class="list-group">
                        <a asp-action="StandardSignIn" class="list-group-item list-group-item-action d-flex justify-content-between align-items-center">
                            <div>
                                <h5 class="mb-1">Standard Sign In</h5>
                                <p class="mb-1">Default sign-in experience without any hints</p>
                            </div>
                            <span class="badge bg-primary rounded-pill">Try</span>
                        </a>
                        
                        <a asp-action="SignInWithLoginHint" class="list-group-item list-group-item-action d-flex justify-content-between align-items-center">
                            <div>
                                <h5 class="mb-1">Sign In with Login Hint</h5>
                                <p class="mb-1">Pre-populates the username field with user@contoso.com</p>
                            </div>
                            <span class="badge bg-primary rounded-pill">Try</span>
                        </a>
                        
                        <a asp-action="SignInWithDomainHint" class="list-group-item list-group-item-action d-flex justify-content-between align-items-center">
                            <div>
                                <h5 class="mb-1">Sign In with Domain Hint</h5>
                                <p class="mb-1">Directs to contoso.com organization login page</p>
                            </div>
                            <span class="badge bg-primary rounded-pill">Try</span>
                        </a>
                        
                        <a asp-action="SignInWithBothHints" class="list-group-item list-group-item-action d-flex justify-content-between align-items-center">
                            <div>
                                <h5 class="mb-1">Sign In with Both Hints</h5>
                                <p class="mb-1">Pre-populates username and directs to specific organization</p>
                            </div>
                            <span class="badge bg-primary rounded-pill">Try</span>
                        </a>
                    </div>
                </div>
            </div>
            
            <div class="card">
                <div class="card-header">
                    <h3>About this demo</h3>
                </div>
                <div class="card-body">
                    <p>This demo showcases Microsoft Identity Web's ability to customize the sign-in experience by passing optional parameters:</p>
                    <ul>
                        <li><strong>loginHint</strong> - Pre-populates the username/email field in the authentication UI</li>
                        <li><strong>domainHint</strong> - Directs the authentication request to a specific tenant's login page</li>
                    </ul>
                    <p>These features help streamline authentication flows and reduce sign-in friction for users.</p>
                </div>
            </div>
        </div>
        
        <div class="col-md-4">
            <div class="card">
                <div class="card-header">
                    <h3>How it works</h3>
                </div>
                <div class="card-body">
                    <p>When you select any option:</p>
                    <ol>
                        <li>The controller redirects to Microsoft Identity's sign-in action</li>
                        <li>Authentication parameters are passed in the redirect</li>
                        <li>After successful authentication, you're redirected to the Profile page</li>
                    </ol>
                    <hr>
                    <p class="text-muted small">Note: For demonstration purposes, this uses "contoso.com" as the example domain.</p>
                </div>
            </div>
        </div>
    </div>
</div>
```