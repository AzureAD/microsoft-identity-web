The Microsoft Identity Web library also enables web APIs to work with the Microsoft identity platform, enabling them to process access tokens for both work and school and Microsoft personal accounts, as well as Azure AD B2C.

## Why use Microsoft.Identity.Web in web APIs?

Currently, ASP.NET Core 3.1 web app templates (`dotnet new webapi -auth`) create web APIs that are protected with the Azure AD v1.0 endpoint, allowing users to sign in with their organizational accounts (also called *Work or school accounts*).

This library adds `ServiceCollection` and `AuthenticationBuilder` extension methods for use in the ASP.NET Core web app **Startup.cs** file. These extension methods enable the web app to sign in users with the Microsoft identity platform and, optionally, enable the web app to call APIs on behalf of the signed-in user.

> .NET Core 5.0 now has project templates using directly Microsoft.Identity.Web

## Protected web APIs - Startup.cs

Assuming you have a similar configuration in `appsettings.json`:

```Json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "Domain": "msidentitysamplestesting.onmicrosoft.com",
    "TenantId": "7f58f645-c190-4ce5-9de4-e2b7acd2a6ab",
    "ClientId": "a4c2469b-cf84-4145-8f5f-cb7bacf814bc"
  },
...
}
```

To enable users to sign in with the Microsoft identity platform:

1. Add the Microsoft.Identity.Web and Microsoft.Identity.Web.UI NuGet packages
2. Remove the AzureAD.UI and AzureADB2C.UI NuGet packages
3. Replace this code in your web API's *Startup.cs* file:

   ```CSharp
   using Microsoft.Identity.Web;

   public class Startup
   {
     ...
     public void ConfigureServices(IServiceCollection services)
     {
      ...
      services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
           .AddAzureAdBearer(options => Configuration.Bind("AzureAd", options));
      ...
     }
     ...
   }
   ```

   ...with this code, using the `AuthenticationBuilder`:

   ```CSharp
   using Microsoft.Identity.Web;

   public class Startup
   {
     ...
     public void ConfigureServices(IServiceCollection services)
     {
      ...
      services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
              .AddMicrosoftIdentityWebApi(Configuration);
      ...
     }
     ...
   }
   ```

   or with this code, using the services directly:

   ```CSharp
   using Microsoft.Identity.Web;

   public class Startup
   {
     ...
     public void ConfigureServices(IServiceCollection services)
     {
      ...
         services.AddMicrosoftIdentityWebApiAuthentication(Configuration);
      ...
     }
     ...
   }
   ```

   This method enables your web API to be protected using the Microsoft identity platform. This includes validating the token in all scenarios (single- and multi-tenant applications) in the Azure public and national clouds.

See also:

- [ASP.NET Core Web API incremental tutorial](https://github.com/Azure-Samples/active-directory-dotnet-native-aspnetcore-v2) chapter 1.1, [Protect the web API](https://github.com/Azure-Samples/active-directory-dotnet-native-aspnetcore-v2/tree/master/1.%20Desktop%20app%20calls%20Web%20API)
- [Protected web API](https://docs.microsoft.com/azure/active-directory/develop/scenario-protected-web-api-overview) scenario overview in the Microsoft identity platform documentation, and related articles

### What if the App ID URI of your application is not api://{ClientID}

The configuration file above assumes that the App ID URI for your application (the base segment of scopes exposed by your Web API) is api://{ClientID}. This is the default when your register your application with the application registration portal. However, you can override it. In that case, you'll want to explicitly set the `Audience` in your configuration to match the App ID URI for your Web API

```Json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "Domain": "msidentitysamplestesting.onmicrosoft.com",
    "TenantId": "7f58f645-c190-4ce5-9de4-e2b7acd2a6ab",
    "ClientId": "a4c2469b-cf84-4145-8f5f-cb7bacf814bc",
    "Audience": "api://myappreg.azurewebsites.net"
  },
...
}
```

#### Case of a B2C Web API

Assuming you have a similar configuration in `appsettings.json`:

```Json
{
  "AzureAdB2C": {
    "Instance": "https://fabrikamb2c.b2clogin.com",
    "ClientId": "90c0fe63-bcf2-44d5-8fb7-b8bbc0b29dc6",
    "Domain": "fabrikamb2c.onmicrosoft.com",
    "SignedOutCallbackPath": "/signout/B2C_1_susi",
    "SignUpSignInPolicyId": "b2c_1_susi",
    "ResetPasswordPolicyId": "b2c_1_reset",
    "EditProfilePolicyId": "b2c_1_edit_profile" // Optional profile editing policy
  },
  // more here
}
```

To enable the web API to accept tokens emitted by Azure AD B2C, have the following code in your Web API:

```CSharp
using Microsoft.Identity.Web;

public class Startup
{
  ...
  public void ConfigureServices(IServiceCollection services)
  {
   ...
      services.AddMicrosoftIdentityWebApiAuthentication(Configuration, "AzureAdB2C");
   ...
  }
  ...
}
```

### Verification of scopes or app roles in the controller actions

In the controller methods, protected web APIs needs to verify that the the token used to called them has the right:

- scopes, in the case of APIs called on behalf of a user
- app roles, in the case of APIs called by daemon applications

#### Verify scopes in Web APIs called on behalf of users

A web API that is called on behalf of users needs to verify the scopes in the controller actions. This can be done:
- from Microsoft.Identity.Web 1.6, using the `[RequiredScopes]` attribute
- before Microsoft.Identity.Web 1.6, using the `VerifyUserHasAnyAcceptedScope` extension method on the HttpContext.

##### Using the `RequiredScopes` attribute

The `RequiredScopes` attribute can be set on a controller, a controller action, a razor page to declare the scopes required by a web API
and validate that at least one of these scopes is available in the token

These required scopes can be declared in two ways: 
- hardcoding them, 
- or declaring them in the configuration. 

Depending on the way you chose, use either one or the other of the constructors.

If the token obtained for this API on behalf of the authenticated user does not have any of the scopes, in its **scope** claim, the attribute 
ensures that the HTTP response is updated providing a status code 403 (Forbidden) and writes to the response body a message telling which scopes are expected in the token.

```CSharp
[Authorize]
[RequiredScope(HomeController.scopeRequiredByAPI)
public class HomeController : Controller
{
 public const string[] scopeRequiredByAPI = new string[] { "access_as_user" };
  /// ...
  public async Task<IActionResult> Action()
  {
  }
}
```

You can also declare these required scopes in the configuration, and reference the configuration key:

For instance if, in the appsettings.json you have the following configuration:
```json
{
 "AzureAd" {
   // more settings
   "Scopes" : "access_as_user access_as_admin"
  }
}
```

then, you can reference it in the attribute:

```CSharp
[Authorize]
[RequiredScope(RequiredScopesConfigurationKey = "AzureAd:Scopes")
public class HomeController : Controller
{
  /// ...
  public async Task<IActionResult> Action()
  {
  }
}
```

##### Using the `VerifyUserHasAnyAcceptedScope` extension method on the HttpContext.

In versions of Microsoft.Identity.Web prior to 1.6.0, you had to use the `VerifyUserHasAnyAcceptedScope` extension method on the HttpContext:

```CSharp
[Authorize]
public class HomeController : Controller
{
 static string[] scopeRequiredByAPI = new string[] { "access_as_user" };
  ...
  public async Task<IActionResult> Action()
  {
   HttpContext.VerifyUserHasAnyAcceptedScope(scopeRequiredByAPI);
  {
  }
```

#### Web APIs called by daemon apps (using client credential flow)

A web API that accepts daemon applications needs to:
- either verify the application roles in the controller actions (See [application roles](https://docs.microsoft.com/azure/active-directory/develop/scenario-protected-web-api-verification-scope-app-roles#verify-app-roles-in-apis-called-by-daemon-apps)
- or be protected by an ACL-based authorization pattern to [control tokens without a roles claim](https://docs.microsoft.com/azure/active-directory/develop/v2-oauth2-client-creds-grant-flow#controlling-tokens-without-the-roles-claim)

##### Verification of app roles

```CSharp
[Authorize]
[RequiredScopeOrAppPermission(AppPermission= new []{"access_as_app"})]
public class HomeController : Controller
{
  public async Task<IActionResult> Action()
  {
   // Or HttpContext.ValidateAppRole("acceptedRole1")
   // Do the work
  }
```

Alternatively, it can use the `[Authorize("role")]` attributes on the controller or an action (or a razor page)

```CSharp
[Authorize("role")]
MyController
```

But for this, you'll need to map the Role claim to "roles" in the startup.cs


```CSharp
 services.Configure<OpenIdConnectOptions>(OpenIdConnectDefaults.AuthenticationScheme, options =>
 {
    // The claim in the Jwt token where App roles are available.
    options.TokenValidationParameters.RoleClaimType = "roles";
 });
```

This is not the best solution if you also need to do authorization based on groups.

For details see the web app incremental tutorial on [authorization by roles and groups](https://github.com/Azure-Samples/active-directory-aspnetcore-webapp-openidconnect-v2/tree/master/5-WebApp-AuthZ).


##### Checking for scopes or app permissions

The `RequiredScopeOrAppPermission` attribute, introduced in Microsoft.Identity.Web 1.25.0 enables you to test for scopes or application permissions using both parameters of the constructor (scopes and app permissions), or their settings equivalent. If you want to test and "and" condition, you can do it by having two of these attributes.

##### To support ACL-based authorization

If you want to enable the ACL-based authorization, you'll need to set the `AllowWebApiToBeAuthorizedByACL` to true in the configuration. otherwise, Microsoft Identity Web will no longer throw an exception when neither roles or scopes are not in the Claims provided If you set this property to true in the **appsettings.json** or programmatically, this is your responsibility to ensure the ACL mechanism.

```json
{
 "AzureAD"
 { 
  // other properties
  "AllowWebApiToBeAuthorizedByACL" : true,
  // other properties
 }
}
```

### Encrypted tokens

Web APIs can demand that they receive encrypted tokens to avoid that their client apps be tempted to crack-open the token (even if we discourage it), and therefore get access to claims about the user. For details on how to setup your web API for encyrpted tokens, see [Token encryption](Token-Decryption)

## Protected web APIs that call downstream APIs on behalf of a user (AAD)

### Startup.cs

If you want your web API to, moreover, call downstream web APIs, add the `.EnableTokenAcquisitionToCallDownstreamApi()` line, and then choose a token cache implementation, for example `.AddInMemoryTokenCaches()`:

```CSharp
using Microsoft.Identity.Web;

public class Startup
{
  ...
  public void ConfigureServices(IServiceCollection services)
  {
   ...
   services.AddMicrosoftIdentityWebApiAuthentication(Configuration)
              .EnableTokenAcquisitionToCallDownstreamApi()
                  .AddInMemoryTokenCaches();
   ...
  }
  ...
}
```

You can also benefit from a higher level API to call the protected downstream API, serialiazing and deserializing parameters if needed, and handling some of the HTTP errors: the `IDownstreamWebApi`. You can register as many of these as downstream web APIs by using `AddDownstreamWebApi` in the Startup.cs file:

```
services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
          .AddMicrosoftIdentityWebApi(Configuration, "AzureAd")
            .EnableTokenAcquisitionToCallDownstreamApi()
               .AddDownstreamWebApi("MyApi", Configuration.GetSection("GraphBeta"))
            .AddInMemoryTokenCaches();
```

The service can be initialized by a section in the appsettings.json like the following:

```JSon
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "ClientId": "[Client_id-of-web-api-eg-2ec40e65-ba09-4853-bcde-bcb60029e596]",
    "TenantId": "common",

   // To call an API
   "ClientSecret": "[Copy the client secret added to the app from the Azure portal]",
   "ClientCertificates": [
  ]
 },
 "GraphBeta": {
    "BaseUrl": "https://graph.microsoft.com/beta",
    "Scopes": "user.read"
    }
}
```

As with web apps, you can choose various token cache implementations. For details see  [Token cache serialization](token-cache-serialization).

Also you can use certificate instead of client secrets. For details see [using certificates](Certificates).

If you're certain that your web API will need some specific scopes, you can optionally pass them as arguments to `EnableTokenAcquisitionToCallDownstreamApi`.

### Web API controller

To enable your web API to call downstream APIs:

- Add (as in web apps) a parameter of type `ITokenAcquisition` to the constructor of your controller. The `ITokenAcquisition` service will be injected by dependency injection by ASP.NET Core.
- In your controller actions, verify that the token contains the scopes expected by the action. To do so, call the `VerifyUserHasAnyAcceptedScope` extension method on the `HttpContext`.
- Alternatively if your web API is called by a daemon app, use `ValidateAppRoles()` 

  <img alt="ScopesRequiredHttpContextExtensions" src="https://user-images.githubusercontent.com/13203188/64253176-f9e3ca80-cf1c-11e9-8fe9-df06cee11c25.png" width="80%"/>

- In your controller actions, call `ITokenAcquisition.GetAccessTokenForUserAsync`, passing the scopes for which to request a token.
- Alternatively if you controller calls a downstream API on behalf of itself (instead of on behalf of the user), call `ITokenAcquisition.GetAccessTokenForApplicationAsync`, passing the scopes for which to request a token.

The following code snippet shows how to combine these steps:

```CSharp
[Authorize]
public class HomeController : Controller
{
  readonly ITokenAcquisition tokenAcquisition;

  static string[] scopeRequiredByAPI = new string[] { "access_as_user" };
  ...
  public async Task<IActionResult> Action()
  {
   HttpContext.VerifyUserHasAnyAcceptedScope(scopeRequiredByAPI);
   string[] scopes = new []{"user.read"};
   try
   {
      string accessToken = await _tokenAcquisition.GetAccessTokenForUserAsync(scopes);
      // call the downstream API with the bearer token in the Authorize header
    }
    catch (MsalUiRequiredException ex)
    {
      _tokenAcquisition.ReplyForbiddenWithWwwAuthenticateHeader(HttpContext, scopes, ex);
    }
   ...
  }
```

Alternatively you can also inject an instance of `IDownstreamWebApi`.

```CSharp
 [Authorize]
 [AuthorizeForScopes(ScopeKeySection = "TodoList:Scopes")]
 public class TodoListController : Controller
 {
     private IDownstreamWebApi _downstreamWebApi;

     public TodoListController(IDownstreamWebApi downstreamWebApi)
     {
         _downstreamWebApi = downstreamWebApi;
     }

     public async Task<ActionResult> Details(int id)
     {
         var value = await _downstreamWebApi.CallWebApiForUserAsync(
             "MyApi",
             options =>
             {
                 options.RelativePath = $"me";
             });
         return View(value);
     }
```

Note that, you'll need to:
- create a static page in your Web API to inform the users that they have successfully signed-up to your web API (like [wwwroot/index.html](https://github.com/Azure-Samples/active-directory-dotnet-native-aspnetcore-v2/blob/master/2.%20Web%20API%20now%20calls%20Microsoft%20Graph/TodoListService/wwwroot/index.html))
- you need to add, in the appsettings.json, the following line:
  ```json
  "CallbackPath": "",
  ```

For details see the following sample: [Sign a user into a Desktop application using Microsoft Identity Platform and call a protected ASP.NET Core Web API, which calls Microsoft Graph on-behalf of the user](https://github.com/Azure-Samples/active-directory-dotnet-native-aspnetcore-v2/tree/master/2.%20Web%20API%20now%20calls%20Microsoft%20Graph)

### Handle conditional access

When your web API tries to get a token for the downstream API, the token acquisition service may throw a `MsalUiRequiredException`. The `MsalUiRequiredException` indicates that the user on the client calling the web API needs to perform additional actions, for example, multi-factor authentication.

Given that the web API isn't capable of performing such interaction itself, the exception needs to be passed to the client. To propagate the exception back to the client, catch the exception and call the `ITokenAcquisition.ReplyForbiddenWithWwwAuthenticateHeader` method.

### Web APIs that acquire tokens on their own behalf (daemon scenarios, client credential flow)

A web API that accepts daemon applications need to verify the application roles in the controller actions

```CSharp
HttpContext.ValidateAppRole("acceptedRole1")
```

If your web API wants to call a downstream web API on behalf of itself (not of behalf of a user), you can use `ITokenAcquisition.GetAccessTokenForAppAsync` in the controller. The code in the startup.cs file is the same as when you call an API on behalf of a user, and the constructor of your controller or Razor page injects an `ITokenAcquisition` service.

```CSharp
[Authorize]
public class HomeController : Controller
{
  readonly ITokenAcquisition tokenAcquisition;
  ...
  public async Task<IActionResult> Action()
  {
   string[] scopes = new []{"users.read.all"};
   HttpContext.ValidateAppRole("acceptedRole1")

   string token = await tokenAcquisition.GetAccessTokenForAppAsync(scopes);
   ...
   // call the downstream API with the bearer token in the Authorize header
  }
```

### Azure AD B2C

The Azure AD B2C service does not support web APIs calling downstream APIs. See [Azure AD B2C limitations](b2c-limitations) for details.

## Using multiple authentication schemes

In some more advanced scenarios, apps might need to support multiple authentication schemes. For example, users can be authenticated from Azure AD and Azure AD B2C. In Microsoft Identity Web, this can be setup like so:

```csharp
services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddMicrosoftIdentityWebApi(Configuration, "AzureAd")
            .EnableTokenAcquisitionToCallDownstreamApi()
            .AddInMemoryTokenCaches();
services.AddAuthentication()
        .AddMicrosoftIdentityWebApi(Configuration, "AzureAdB2C", "B2CScheme")
            .EnableTokenAcquisitionToCallDownstreamApi();
```

The schemes specified above can also be added to the default authorization policy. This means that you can decorate the controllers with the `[Authorize]` attribute and it will accept requests from all default schemes.

```csharp
services.AddAuthorization(options =>
{
    options.DefaultPolicy = new AuthorizationPolicyBuilder(
        JwtBearerDefaults.AuthenticationScheme,
        "B2CScheme")
        .RequireAuthenticatedUser()
        .Build();
});
```

More details can be found in ASP.NET documentation:

- [Authorize with a specific scheme in ASP.NET Core](https://docs.microsoft.com/en-us/aspnet/core/security/authorization/limitingidentitybyscheme?view=aspnetcore-3.1)
- [Create an ASP.NET Core web app with user data protected by authorization](https://docs.microsoft.com/en-us/aspnet/core/security/authorization/secure-data?view=aspnetcore-3.1#require-authenticated-users)

## Other forms of web APIs -gRPC services and Azure functions

gRPC services and Azure functions can also be considered as protected web APIs (as they can be called by client applications). This means that a lot of what is described above also applies to them. For details on how Microsoft identity web helps building protected gRPC and Azure functions see:

- [gRPC services](https://dev.to/425show/secure-grpc-service-with-net-core-and-azure-active-directory-4p6k)
- [Use Microsoft identity web with Azure functions](https://winsmarts.com/use-microsoft-identity-web-with-azure-functions-2a5c52824578) as well as the [maliksahil/ms-identity-azurefunctions-microsoft-identity-web](https://github.com/maliksahil/ms-identity-azurefunctions-microsoft-identity-web) code sample

A project template exists for gRPC services (worker2), and we are working on a project template for Azure functions.

# More information about the scenarios

For more details on the end to end scenario, see:
- [Scenario: Protected web API](https://docs.microsoft.com/azure/active-directory/develop/scenario-protected-web-api-overview)
- [Scenario: A web API that calls web APIs](https://docs.microsoft.com/azure/active-directory/develop/scenario-web-api-call-api-overview)
- [Scenario: Daemon application that calls web APIs](https://docs.microsoft.com/azure/active-directory/develop/scenario-daemon-overview)

# Microsoft Identity Web and Protocols

## OAuth 2.0 protocols used in web apps.

In web APIs, Microsoft.Identity.Web leverages the following OAuth 2.0 protocols:
- [OAuth 2.0 On-Behalf-Of flow](https://docs.microsoft.com/en-us/azure/active-directory/develop/v2-oauth2-on-behalf-of-flow) and [Token refresh](https://docs.microsoft.com/azure/active-directory/develop/v2-oauth2-auth-code-flow#refresh-the-access-token) for GetTokenForUser.
- [OAuth 2.0 client credentials flow](https://docs.microsoft.com/azure/active-directory/develop/v2-oauth2-client-creds-grant-flow) for GetTokenForApp.

## See also

- [Secure a backend web API for multitenant applications](https://docs.microsoft.com/azure/architecture/multitenant-identity/web-api)