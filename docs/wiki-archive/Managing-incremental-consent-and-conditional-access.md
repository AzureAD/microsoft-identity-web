## Cache eviction, incremental consent and conditional access: they require user interaction

### Session cookie and token cache in web apps

Sometimes, in web apps, the user can be signed-in, but the token cache does not contain the token to access the web APIs. This is expected and happens, in particular if:
- you are using an in-memory token cache, and have restarted your application
- you have setup cache eviction times, which remove tokens from the cache earlier than the session cookie (which says the user is signed-in)

In that case, you'll get an `MsalUiRequiredException`, which is expected. This article explains how Microsoft identity web can handle automatically the challenge of the user, which most of the times, will be silent, and have the token cache repopulated. See how do do that:
- [in MVC controllers](#in-mvc-controllers)
- [in Razor pages](#in-razor-pages)
- [in Blazor server](#in-blazor-server)

This article also explains how to handle, in the very same way incremental consent and conditional access.

### Incremental consent and static permissions

#### Incremental consent

The Microsoft identity platform allows users to incrementally consent to your application access to more resources / web APIs on their behalf (that is to consent to more scopes) as they are needed. This is called incremental [consent](https://docs.microsoft.com/azure/active-directory/develop/v2-permissions-and-consent). This means that, in a web app, a controller / Razor or Blazor page action could require some scopes, and then another controller action could require more scopes, and this will trigger an interaction with the user of the web application so that the user can consent for these new scopes.

#### Case of user flow in B2C

If you're building an Azure AD B2C application and use several user flows, you'll also need to handle incremental consent, as interaction will be required with the user.

#### Static permissions

You can also decide to not handle incremental consent. In that case you define the permissions at app registration, and have the tenant administrator consent to them all (admin consent). Then, as you request tokens, you'll use the `{resource}/.default` [syntax](https://docs.microsoft.com/azure/active-directory/develop/v2-permissions-and-consent#the-default-scope) to get an access token for all pre-approved scopes for this given resource. You can also request specific pre-approved scopes if you wish.

If, at some point, your app requests more scopes than what the admin has consented, you'll receive an `MsalUiRequiredException`, and you'll know that you need to have more scopes pre-approved by the tenant admin.

If you are a Microsoft employee building a first party application, static permissions are the way to go.

### Conditional access

It can also happen that, when requesting a token to call a (downstream) web API, your web app or web API receives a claims challenge exception instructing the app that the user needs to provide more claims (for instance the user needs to perform multi-factor authentication). This happens for some specific web APIs for which the tenant administrator has added [conditional access](https://docs.microsoft.com/en-us/azure/active-directory/develop/v2-conditional-access-dev-guide) policies. From your point of view, as an application developer, this looks the same as handing incremental consent: the user needs will get through a consent screen, which will trigger more user flows, such as performing multi-factor authentication.

You can choose to not handle incremental consent, however, you should handle conditional access, as your web app / API could be non-functional if installed in tenants where the tenant admins decide to enable conditional access. And given incremental consent and conditional access is handled similarly we recommend you handle these scenarios in your applications.

### How to handle conditional access and incremental consent: depends on your type of app / technology

The way for your app to handle conditional access and incremental consent is different depending on if you are building:

- a [web app](#handling-incremental-consent-or-conditional-access-in-web-apps) (where interaction is possible with the user),
- or a [web API](#handling-incremental-consent-or-conditional-access-in-web-apis) (where interaction is not possible, and therefore where the information needs to be propagated back to the client).

For web apps, it's also different depending on the technology you use:

- ASP.NET Core [MVC controller](#in-mvc-controllers),
- [Razor page](#in-razor-pages)
- [Blazor page](#in-blazor-server).

## Handling incremental consent or conditional access in web apps

### Startup.cs

In web apps, handling conditional access and incremental consent requires an `AccountController` in the **MicrosoftIdentity** area. Microsoft.Identity.Web provides a default account controller [AccountController](https://github.com/AzureAD/microsoft-identity-web/blob/master/src/Microsoft.Identity.Web.UI/Areas/MicrosoftIdentity/Controllers/AccountController.cs) in **Microsoft.Identity.Web.UI**, as well as default Razor [pages](https://github.com/AzureAD/microsoft-identity-web/tree/master/src/Microsoft.Identity.Web.UI/Areas/MicrosoftIdentity/Pages/Account), which you can [override](https://docs.microsoft.com/aspnet/core/razor-pages/ui-class?tabs=visual-studio#override-views-partial-views-and-pages) in your application if you want to provide a different user interface. To use it you'll need to add `.AddMicrosoftIdentityUI()` to `AddControllersWithViews` in the `ConfigureServices(IServiceCollection services)` method in your **startup.cs** file:

```CSharp
public void ConfigureServices(IServiceCollection services)
{
 services.AddMicrosoftIdentityWebAppAuthentication(Configuration, "AzureAd")
           .EnableTokenAcquisitionToCallDownstreamApi(scopes)
             .AddInMemoryTokenCaches();

 // more here
 services.AddControllersWithViews()
         .AddMicrosoftIdentityUI();
}
```

You'll also need to make sure that routes are mapped to the controller, which might not be the case by default if you create a Blazor application and don't use the Microsoft.Identity.Web templates. For this make sure that in the `app.UseEndPoints` delegate in the `Configure(IApplicationBuilder app, IWebHostEnvironment env)` method, you call `endpoints.MapControllers()`.

```CSharp
public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
{
  app.UseAuthentication();
  app.UseAuthorization();

  // more here
  app.UseEndpoints(endpoints =>
  {
   endpoints.MapControllers();
   // More here
  });
}
```

### In MVC controllers

In MVC controllers, you'll need to use the `[AuthorizeForScopes]` attribute:

- on the controller itself. This will provide the default scope for all actions in the controller, unless you add an `[AuthorizeForScopes]` attribute on a particular action
- on controller actions when you want an action to have a different (probably more granular) scope.

#### The AuthorizeForScopes attribute - how does it work?

`AuthorizeForScopesAttribute` inherits from `ExceptionFilterAttribute` which means that it processes the MSAL.NET MsalUiRequiredException to handle conditional access and incremental consent. This also means that if you catch MSAL exception, you should re-throw the caught MSAL exceptions so that the attribute can handle it

#### How to use the AuthorizeForScopes attribute

The code snippet shows the Authorize for scopes attribute using "user.read" on all actions but `{"user.read", "user.write"}` on the `Write` controller action:

```CSharp
[Authorize]
[AuthorizeForScopes(Scopes = new string[] {"user.read"})]
public class HomeController : Controller
{
 private readonly ITokenAcquisition _tokenAcquisition;

 public HomeController(ITokenAcquisition tokenAcquisition)
 {
  _tokenAcquisition = tokenAcquisition;
 }

 public async Task<IActionResult> Index()
 {
  var accessToken = tokenAcquisition.GetAccessTokenForUserAsync(new string[] {"user.read"});
  // Call API
  return View();
 }

 [AuthorizeForScopes(Scopes = new string[] {"user.read", "user.write"})]
 public async Task<IActionResult> Write()
 {
  var accessToken = tokenAcquisition.GetAccessTokenForUserAsync(new string[] {"user.read", "user.write"});
  // Call API
  return View();
 }
}
```

Alternatively to hard coding the scopes in the code, you can specify the scopes in the appsetttings.json file as a space separated strings, and reference the corresponding configuration section/key in the code using the `ScopeKeySection` property of the `AuthorizeForScopes` attribute.

```JSON
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    /* more here*/
  },
  "CalledApi": {
    "CalledApiScopes": "user.read mail.read",
    "CalledApiUrl": "https://graph.microsoft.com/v1.0"
  },
```

In the controller:

```CSharp
 [AuthorizeForScopes(ScopeKeySection = "CalledApi:CalledApiScopes")]
 public async Task<IActionResult> Index()
 {
  var accessToken = tokenAcquisition.GetAccessTokenForUserAsync(scopesFromResources);
  // Call API
  return View();
 }
```

### In Razor pages

In Razor pages, you'll need to use the [AuthorizeForScopes] attribute on the class representing the Razor page.

```CSharp
namespace RazorSample.Pages
{
    [AuthorizeForScopes(ScopeKeySection = "CalledApi:CalledApiScopes")]
    public class IndexModel : PageModel
    {
        private readonly GraphServiceClient _graphServiceClient;

        public IndexModel(GraphServiceClient graphServiceClient)
        {
            _graphServiceClient = graphServiceClient;
        }

        public async Task OnGet()
        {
            var user = await _graphServiceClient.Me.Request().GetAsync();

            ViewData["ApiResult"] = user.DisplayName;
        }
    }
```

### In Blazor server

In Blazor server, you'll need to inject a service, and catch the exceptions so that the user is re-signed-in and consents / performs conditional access. The code below presents a Blazor page named **callwebapi** in a Blazor server assembly.

#### In the Startup.cs file

You'll need to register the Microsoft Identity consent and conditional access handler service. For this, in startup.cs

Replace

```CSharp
 services.AddServerSideBlazor();
```

by

```CSharp
 services.AddServerSideBlazor()
         .AddMicrosoftIdentityConsentHandler();
```

#### In the Blazor page itself

In each Blazor page acquiring tokens, you'll use the Microsoft Identity consent and conditional access handler service to handle the exception:

You need to:

- add a using for Microsoft.Identity.Web
- inject the `MicrosoftIdentityConsentAndConditionalAccessHandler` service.

  ```CSharp
  @using Microsoft.Identity.Web
  @inject MicrosoftIdentityConsentAndConditionalAccessHandler ConsentHandler
  ```

- When you acquire your token (or call a method that acquires a token), if you get an exception, you'll need to process it with the `MicrosoftIdentityConsentAndConditionalAccessHandler`:

  ```CSharp
    catch (Exception ex)
    {
     ConsentHandler.HandleException(ex);
    }
  ```

For instance:

```CSharp
@page "/callwebapi"

@using MySample
@using Microsoft.Identity.Web
@inject MicrosoftIdentityConsentAndConditionalAccessHandler ConsentHandler
@inject IDownstreamWebApi downstreamAPI

<h1>Call an API</h1>

<p>This component demonstrates fetching data from a Web API.</p>

@if (apiResult == null)
{
    <p><em>Loading...</em></p>
}
else
{
    <h2>API Result</h2>
    @apiResult
}

@code {
    private string apiResult;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            // downstreamAPI.CallWebApiAsync calls ITokenAcquisition.GetAccessTokenForUserAsync
            apiResult = await downstreamAPI.CallWebApiAsync("me");
        }
        catch (Exception ex)
        {
            ConsentHandler.HandleException(ex);
        }
    }
}
```

## Case of Ajax calls

See [Ajax calls and incremental consent and conditional access](1.2.0#ajax-calls-can-now-participate-in-incremental-consent-and-conditional-access), introduced in 1.2.0

## Handling incremental consent or conditional access in web APIs

In a web API, your controller action will need to explicity call `ITokenAcquisition.ReplyForbiddenWithWwwAuthenticateHeader` so that the web API replies to the client with a wwwAuthenticate header containing information about missing claims.

```CSharp
public async Task<string> CallGraphApiOnBehalfOfUser()
{
 string[] scopes = { "user.read" };

 // we use MSAL.NET to get a token to call the API On Behalf Of the current user
 try
 {
  string accessToken = await _tokenAcquisition.GetAccessTokenForUserAsync(scopes);
  dynamic me = await CallGraphApiOnBehalfOfUser(accessToken);
  return me.UserPrincipalName;
 }
 catch (MicrosoftIdentityWebChallengeUserException ex)
 {
  await _tokenAcquisition.ReplyForbiddenWithWwwAuthenticateHeaderAsync(scopes, ex.MsalUiRequiredException);
  return string.Empty;
 }
 catch (MsalUiRequiredException ex)
 {
  await _tokenAcquisition.ReplyForbiddenWithWwwAuthenticateHeaderAsync(scopes, ex);
  return string.Empty;
 }
}
```

Note that in some cases (Blazor applications, SignalR), `ReplyForbiddenWithWwwAuthenticateHeaderAsync` cannot access the HttpContext, and you'll get an InvalidOperationException with the following message: "IDW10002: Current HttpContext and HttpResponse argument are null. Pass an HttpResponse argument. ". If that's the case, you can pass the `HttpResponse` as the last argument:

```CSharp
 catch (MsalUiRequiredException ex)
 {
  await _tokenAcquisition.ReplyForbiddenWithWwwAuthenticateHeaderAsync(scopes, ex, HttpContext.Reponse);
  return string.Empty;
 }
}
```

## Clearing the authentication cookie instead?

It's recommended that you let Microsoft.Identity.Web handle this exception as seen in this article. However, if you really want to remove the sign-in session cookie when the cache is empty, see the code snippet proposed in [clear session auth cookie if cache is missing account](https://github.com/AzureAD/microsoft-identity-web/issues/13#issuecomment-878528492)
