## Using Microsoft.Identity.Web to protect ASP.NET Core web apps

Microsoft identity web supports ASP.NET Core web apps that sign-in users in Microsoft Entra ID, Azure AD B2C, and Microsoft Entra External IDs. Optionally these apps can call downstream web APIs. Web apps typically run on a server and serve HTML pages.

See:
- [Create a new ASP.NET Core web app](new-web-app) using the command line, or the Visual Studio wizard.
- [Add authentication to an existing web app](existing-web-app)
- [Migrate an ASP.NET Core 3.1 web app to use Microsoft Identity Web](migrating-aspnetcore3x-webapps)

## Advanced scenarios

### Enabling a sign-up experience
You can enable your web app to allow users to sign up and create a new guest account. First, set up your tenant and the app as described in [Add a self-service sign-up user flow to an app](https://aka.ms/msal-net-prompt-create). Next, set `Prompt` property in `OpenIdConnectOptions` (or in `MicrosoftIdentityOptions` which inherits from it) to `"create"` to trigger the sign-up experience. Your app can have a **Sign up** button linking to an Action which sets the `Prompt` property, like in the example below. After the user goes through the sign-up process, they will be logged into the app.

```csharp
[HttpGet("{scheme?}")]
public IActionResult SignUp([FromRoute] string scheme)
{
    scheme ??= OpenIdConnectDefaults.AuthenticationScheme;
    var parameters = new Dictionary<string, object>
    {
        { "prompt", "create" },
    };
    OAuthChallengeProperties oAuthChallengeProperties = new OAuthChallengeProperties(new Dictionary<string, string>(), parameters);
    oAuthChallengeProperties.RedirectUri = Url.Content("~/");

    return Challenge(
        oAuthChallengeProperties,
        scheme);
}
```

### Using delegate events instead of the configuration section

`AddMicrosoftIdentityWebApp` (applied to authentication builders) has another override, which takes delegates instead of a configuration section. The override with
a configuration section actually calls the override with delegates. See the source code for [AddMicrosoftIdentityWebApp with configuration section](https://github.com/AzureAD/microsoft-identity-web/blob/2f133d17230bf753acbd7b70ceb5a0a3378adaba/src/Microsoft.Identity.Web/WebAppExtensions/WebAppAuthenticationBuilderExtensions.cs#L36)

In advanced scenarios you might want to add configuration by code, or if you want to subscribe to OpenIdConnect events. For instance if you want to provide a custom processing when the token is validated, you could use code like the following:

```csharp
services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
        .AddMicrosoftIdentityWebApp(options =>
{
    Configuration.Bind("AzureAD", options);
    options.Events ??= new OpenIdConnectEvents();
    options.Events.OnTokenValidated += OnTokenValidatedFunc;
});
```

with `OnTokenValidatedFunc` like the following:

```csharp
private async Task OnTokenValidatedFunc(TokenValidatedContext context)
{
    // Custom code here
    await Task.CompletedTask.ConfigureAwait(false);
}
```

In the above code, your handler will be executed after any existing handlers. In the below code, your code will be executed before any other existing handlers.

```csharp
services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
        .AddMicrosoftIdentityWebApp(options =>
{
    Configuration.Bind("AzureAD", options);
    options.Events ??= new OpenIdConnectEvents();
    var existingHandlers = options.Events.OnTokenValidated;
    options.Events.OnTokenValidated = OnTokenValidatedFunc;
    options.Events.OnTokenValidated += existingHandlers;
});
```

This override also allows you to set an optional delegate to initialize the `CookiesAuthenticationOptions`.

#### Specify the authentication scheme

In more advanced scenarios, for instance if you use several IdPs, you might want to specify an
authentication scheme (here using the default authentication scheme, which makes this code
snippet equivalent to the previous one)

```CSharp
      services.AddAuthentication("MyAuthenticationScheme")
              .AddMicrosoftIdentityWebApp(Configuration, 
                 openIdConnectAuthenticationScheme: "MyAuthenticationScheme");
```

### Additional resources to sign-in user to a web app with Microsoft identity platform

See also:

- [ASP.NET Core Web app incremental tutorial](https://github.com/Azure-Samples/active-directory-aspnetcore-webapp-openidconnect-v2/tree/master/1-WebApp-OIDC/1-1-MyOrg) chapter 1.1, *Sign in users in your organization*
- [Web App that signs-in users](https://docs.microsoft.com/azure/active-directory/develop/scenario-web-app-sign-user-overview) scenario overview in the Microsoft identity platform documentation, and related articles


#### Programmatically invoke a B2C journey

This [example](https://github.com/AzureAD/microsoft-identity-web/blob/330eb11c464b0e3b622a608d702097a13e477d10/tests/B2CWebAppCallsWebApi/Client/Controllers/TodoListController.cs#L51-L58), demonstrates how to programmatically trigger a B2C user flow from a controller action or razor. In the `AuthorizeForScopes` attribute, you will define the scopes and B2C user flow, and then include the same in `GetAccessTokenForUserAsync`. Microsoft Identity Web will handle the challenge for you.

```CSharp
  [AuthorizeForScopes(Scopes = new string[] { Scope }, UserFlow = EditProfile)] // Must be the same user flow as used in `GetAccessTokenForUserAsync()`
  public async Task<ActionResult> ClaimsEditProfile()
  {
   // We get a token, but we don't use it. It's only to trigger the user flow
   await _tokenAcquisition.GetAccessTokenForUserAsync(
                new string[] { Scope },
                userFlow: EditProfile);
   return View(Claims, null);
  }
```

### Web apps that sign in users and call web APIs on behalf of the signed-in user - Startup.cs

If you want your web app to call web APIs, add the `.EnableTokenAcquisitionToCallDownstreamApi()` line, and then choose a token cache implementation, for example `.AddInMemoryTokenCaches()`:

![WebAppBuilderExtensionsMethods](https://user-images.githubusercontent.com/19942418/78593320-4b2da280-77fb-11ea-9c65-7a7714cc6514.png)

![WebAppServiceExtensionsMethods](https://user-images.githubusercontent.com/19942418/78593166-0b66bb00-77fb-11ea-8e97-411651142627.png)

```CSharp
using Microsoft.Identity.Web;

public class Startup
{
 const string scopesToRequest = "user.read";
  ...
  public void ConfigureServices(IServiceCollection services)
  {
   ...
   services.AddMicrosoftIdentityWebAppAuthentication(Configuration)
                .EnableTokenAcquisitionToCallDownstreamApi(new string[] { scopesToRequest })
                     .AddInMemoryTokenCaches();
   ...
  }
  ...
}
```

By default, `AddMicrosoftIdentityWebAppAuthentication` and the override of `AddMicrosoftIdentityWebApp` taking a configuration object get the configuration from the "AzureAD" section of the configuration files. It has
several parameters you can change.

The proposed token cache serialization is in memory. You can also use the session cache, or various distributed caches.

#### Optimization

Note that you don't need to pass-in the scopes to request when calling `EnableTokenAcquisitionToCallDownstreamApi`. You can do that just in time in the controller (see [Web app controller](#web-app-controller) below)

```CSharp
using Microsoft.Identity.Web;

public class Startup
{
  ...
  public void ConfigureServices(IServiceCollection services)
  {
   ...
   services.AddMicrosoftIdentityWebAppAuthentication(Configuration)
                .EnableTokenAcquisitionToCallDownstreamApi()
                  .AddInMemoryTokenCaches();
   ...
  }
  ...
}
```

### Web app controller

For your web app to call web APIs on behalf of the signed-in user, add a parameter of type `ITokenAcquisition` to the constructor of your controller (the `ITokenAcquisition` service will be injected by dependency injection by ASP.NET Core).

![ITokenAcquisition](https://user-images.githubusercontent.com/19942418/78518063-162a3d00-7774-11ea-9866-9bf94f4435de.png)

```CSharp
using Microsoft.Identity.Web;

[Authorize]
public class HomeController : Controller
{
  readonly ITokenAcquisition tokenAcquisition;

  public HomeController(ITokenAcquisition tokenAcquisition)
  {
   this.tokenAcquisition = tokenAcquisition;
  }
  ...
```

Then, in your controller actions, call `ITokenAcquisition.GetAccessTokenForUserAsync`, passing the scopes for which to request a token. The other methods of ITokenAcquisition are used from the `EnableTokenAcquisitionToCallDownstreamApi()` method and similar methods for web APIs (see below).

```CSharp
[Authorize]
public class HomeController : Controller
{
  readonly ITokenAcquisition tokenAcquisition;
  ...
  [AuthorizeForScopes(Scopes = new[] { "user.read" })]
  public async Task<IActionResult> Action()
  {
   string[] scopes = new []{"user.read"};
   string token = await tokenAcquisition.GetAccessTokenForUserAsync(scopes);
   ...
   // call the downstream API with the bearer token in the Authorize header
  }
```

The controller action is decorated with the `AuthorizeForScopesAttribute` which enables it to process the `MsalUiRequiredException` that could be thrown by the service implementing `ITokenAcquisition.GetAccessTokenForUserAsync`. The web app can then interact with the user and ask them to consent to the scopes, or re-sign in if needed.

![AuthorizeForScopes](https://user-images.githubusercontent.com/19942418/78518130-6d301200-7774-11ea-93a5-0184e097c2cf.png)

### Web apps that acquire tokens on their own behalf (daemon scenarios, client credential flow)

If your application wants to call a web API on behalf of itself (not of behalf of a user), you can use `ITokenAcquisition.GetAccessTokenForAppAsync` in the controller. The code in the startup.cs file is the same as when you call an API on behalf of a user, and the constructor of your controller or Razor page injects an `ITokenAcquisition` service.

```CSharp
[Authorize]
public class HomeController : Controller
{
  readonly ITokenAcquisition tokenAcquisition;
  ...
  public async Task<IActionResult> Action()
  {
   string[] scopes = new []{"users.read.all"};
   string token = await tokenAcquisition.GetAccessTokenForAppAsync(scopes);
   ...
   // call the downstream API with the bearer token in the Authorize header
  }
```

# More information about the scenarios

For more details on the end to end scenario, see:
- [Scenario: Web app that signs in users](https://docs.microsoft.com/azure/active-directory/develop/scenario-web-app-sign-user-overview?tabs=aspnetcore)
- [Scenario: A web app that calls web APIs](https://docs.microsoft.com/azure/active-directory/develop/scenario-web-app-call-api-overview)

# Microsoft Identity Web and Protocols

## OAuth 2.0 protocols used in web apps.

In web apps, Microsoft.Identity.Web leverages the following OAuth 2.0 protocols:
- [OAuth 2.0 authorization code flow](https://docs.microsoft.com/azure/active-directory/develop/v2-oauth2-auth-code-flow) and [Token refresh](https://docs.microsoft.com/azure/active-directory/develop/v2-oauth2-auth-code-flow#refresh-the-access-token) for GetTokenForUser
- [OAuth 2.0 client credentials flow](https://docs.microsoft.com/azure/active-directory/develop/v2-oauth2-client-creds-grant-flow) for GetTokenForApp.

## AADB2C90088 invalid_grant error

If you have an AAD B2C web app, which just signs in users (does not call a web API), and you include a valid client secret for the web app in the `appsettings.json` file, you will get this error when switching between policies:

`Microsoft.AspNetCore.Authentication.OpenIdConnect.OpenIdConnectHandler: Error: Message contains error: 'invalid_grant', error_description: 'AADB2C90088: The provided grant has not been issued for this endpoint. Actual Value : B2C_1_susi_v3 and Expected Value : B2C_1_edit_profile`

### How do I resolve the error?

Do one of the following:

1. Remove the client secret in `appsettings.json`. If the web app is only signing in users and NOT calling a downstream API, it does not need to have the client secret included in the `appsettings.json` file.

2. Enable MSAL .NET to correctly handle the auth code redemntion by including the following in `Startup.cs`:

```csharp
services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
                    .AddMicrosoftIdentityWebApp(Configuration, "AzureAdB2C")
                        .EnableTokenAcquisitionToCallDownstreamApi()
                        .AddInMemoryTokenCaches();
```

### Why?

This occurs because each policy, or user flow, is a separate authorization server in AAD B2C, meaning each user flow issues their own tokens. 
When only signing-in users, your client app only needs the ID token, which contains information about the signed in user. However, if the client secret is included in the `appsettings.json`, Microsoft Identity Web assumes the web app will eventually call a downstream API, so it requests a code and ID token. ASP .NET then receives the code, but cannot complete the authorization code flow, so MSAL .NET is invoked to redeem the code for tokens, but the ID token and the code were not provided by the same authority (for example, one for su_si policy and one for edit_profile policy), so an invalid_grant error is thrown.


## See also

[Authenticate using Azure AD and OpenID Connect](https://docs.microsoft.com/azure/architecture/multitenant-identity/authenticate)