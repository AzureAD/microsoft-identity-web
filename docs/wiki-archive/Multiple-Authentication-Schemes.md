# Multiple authentication schemes

Microsoft Identity Web now supports multiple authentication schemes, as of **v.1.11.0**. 

This means, as an app developer, you can have several authentication schemes in the same ASP.NET Core app. Such as signing-in users with two identity providers (two Azure AD web app registration), or an Azure AD app and an Azure AD B2C app, or a web app and a web API. Basically mixing authentication schemes in the same ASP.NET Core app.

## Error IDW10503

If you get an error like he following when your controller calls Microsoft Graph, a downstream web API, or a token acquirer: `IDW10503: Cannot determine the cloud Instance. The provided authentication scheme was ''. Microsoft.Identity.Web inferred 'Bearer' as the authentication scheme. Available authentication schemes are 'Cookies,OpenIdConnect,Bearer'. See https://aka.ms/id-web/authSchemes`, you need to specify the authentication scheme to use to get the token (that is that maps to the right section of the appsettings.json)

| Method | How to specify the authentication scheme
| -- | -- |
| IDownstreamWebApi.methods | `_downstreamWebApi.GetForUserAsync<Task>("apiMonitor", authenticationScheme:"AuthSchemeYouWantToUse");`
| IDownstreamWebApi.methods | `graphServiceClient.Me.GetRequest().WithAuthenticationScheme("AuthSchemeYouWantToUse").GetAsync();`
| ITokenAcquisition.GetAccessTokenForUserAsync| `tokenAcquisition.GetTokenForUserAsync(scopes, authenticationScheme:"AuthSchemeYouWantToUse" )` |
| ITokenAcquisition.GetAccessTokenForAppAsync | `tokenAcquisition.GetTokenForAppAsync(scope, authenticationScheme:"AuthSchemeYouWantToUse" )`|

The rest of the article provides more details.

## Example

See [this developer test app](https://github.com/AzureAD/microsoft-identity-web/tree/master/tests/DevApps/MultipleAuthSchemes), which shows how to have both an Azure AD B2C and Azure AD sign-in in the same app. 

### appsettings.json

In the `appsettings.json` you can now have two authentication schemes. In this example, we'll do one for Azure AD and one for Azure AD B2C. Both apps are registered in their respective portals.

```csharp
{
    "AzureAdB2C": {
        "Instance": "https://fabrikamb2c.b2clogin.com",
        "ClientId": "fdb91ff5-5ce6-41f3-bdbd-8267c817015d",
        "Domain": "fabrikamb2c.onmicrosoft.com",
        "SignUpSignInPolicyId": "b2c_1_susi",
        "ResetPasswordPolicyId": "b2c_1_reset",
        "EditProfilePolicyId": "b2c_1_edit_profile", // Optional profile editing policy
        "CallbackPath": "/signin-oidc-b2c",
        "ClientSecret": "",
        "SignedOutCallbackPath": "/signout/B2C_1_susi"
    },
    "AzureAd": {
        "Instance": "https://login.microsoftonline.com/",
        "Domain": "msidentitysamplestesting.onmicrosoft.com",
        "TenantId": "7f58f645-c190-4ce5-9de4-e2b7acd2a6ab",
        "ClientId": "86699d80-dd21-476a-bcd1-7c1a3d471f75",
        "ClientSecret": "",
        "ClientCertificates": [
        ],
        "CallbackPath": "/signin-oidc"
    },
    "DownstreamApi": {
        "BaseUrl": "https://graph.microsoft.com/v1.0",
        "Scopes": "user.read"
    },
    "DownstreamB2CApi": {
        "BaseUrl": "https://fabrikamb2chello.azurewebsites.net/hello",
        "Scopes": "https://fabrikamb2c.onmicrosoft.com/helloapi/demo.read"
    },
    "Logging": {
        "LogLevel": {
            "Default": "Information",
            "Microsoft": "Warning",
            "Microsoft.Hosting.Lifetime": "Information"
        }
    },
    "AllowedHosts": "*"
}
```

### `Startup.cs`

In `Startup.cs` in `ConfigureServices`, we have two sections for `.AddAuthentication`, one for `AzureAd` and one for `AzureAdB2C`. Please note that `.AddAuthentication()` has no default scheme defined.

```csharp
services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme) // This means default scheme is "OpenIdConnect"
        .AddMicrosoftIdentityWebApp(Configuration.GetSection("AzureAd"), OpenIdConnectDefaults.AuthenticationScheme)
            .EnableTokenAcquisitionToCallDownstreamApi(Configuration.GetValue<string>("DownstreamApi:Scopes")?.Split(' '))
                .AddMicrosoftGraph(Configuration.GetSection("DownstreamApi"))
                .AddInMemoryTokenCaches();

services.AddAuthentication() // Note that we don't provide the default scheme (there is only one default)
        .AddMicrosoftIdentityWebApp(Configuration.GetSection("AzureAdB2C"), "B2C", "cookiesB2C")
            .EnableTokenAcquisitionToCallDownstreamApi(Configuration.GetValue<string>("DownstreamB2CApi:Scopes")?.Split(' '))
                .AddDownstreamWebApi("DownstreamB2CApi", Configuration.GetSection("DownstreamB2CApi"));
```

For the AAD sign-in, the web app will call Microsoft Graph, and for AzureAD B2C, the same ASP.NET Core web app will call a downstream B2C web API.

### Controllers

#### Example 

For the [MultipleAuthSchemes](https://github.com/AzureAD/microsoft-identity-web/tree/master/tests/DevApps/MultipleAuthSchemes) test app, there are two Home controllers. The one for B2C will now specify the authentication scheme in the Authorize attribute, as this is not the default authorization scheme, and it will pass-in the authentication scheme to the methods acquiring tokens or calling the downstream API (`IDownstreamWebApi`)

```csharp
    [Authorize(AuthenticationSchemes = "B2C")]
    public class HomeB2CController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IDownstreamWebApi _downstreamWebApi;

        public HomeB2CController(ILogger<HomeController> logger, IDownstreamWebApi downstreamWebApi)
        {
            _downstreamWebApi = downstreamWebApi;
            _logger = logger;
        }

        [AuthorizeForScopes(
            ScopeKeySection = "DownstreamB2CApi:Scopes", UserFlow = "b2c_1_susi")]
        public async Task<IActionResult> Index()
        {
            var value = await _downstreamWebApi.GetForUserAsync<Task>("DownstreamB2CApi", authenticationScheme:"B2C");
            return View(value);
        }
// more code here ...
```

In `_Layout.cshtml`:

```csharp
// These lines added 
<li class="nav-item">
    <a class="nav-link text-dark" asp-area="" asp-controller="HomeB2C" asp-action="Index">Sign-in B2C</a>
</li>
```

Now, I can support both AAD and AAD B2C login with the same ASP.NET Core web app. 


Note that the home controller (AAD) doesn't need to specify the auth scheme, as it's using the default auth scheme

```CSharp
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        private readonly GraphServiceClient _graphServiceClient;

        public HomeController(ILogger<HomeController> logger, GraphServiceClient graphServiceClient)
        {
             _logger = logger;
            _graphServiceClient = graphServiceClient;
       }

        [AuthorizeForScopes(ScopeKeySection = "DownstreamApi:Scopes"]
        public async Task<IActionResult> Index()
        {
            var user = await _graphServiceClient.Me.Request().GetAsync();
            ViewData["ApiResult"] = user.DisplayName;

            return View();
        }
```

#### Variation of the sample

If you don't specify any default scheme in the Startup.cs, all your controllers will need to specify the authentication scheme. For instance

```csharp
services.AddAuthentication() // No default scheme
        .AddMicrosoftIdentityWebApp(Configuration.GetSection("AzureAd"), "openid2")
            .EnableTokenAcquisitionToCallDownstreamApi(Configuration.GetValue<string>("DownstreamApi:Scopes")?.Split(' '))
                .AddMicrosoftGraph(Configuration.GetSection("DownstreamApi"))
                .AddInMemoryTokenCaches();

services.AddAuthentication() // No default scheme either
        .AddMicrosoftIdentityWebApp(Configuration.GetSection("AzureAdB2C"), "B2C", "cookiesB2C")
            .EnableTokenAcquisitionToCallDownstreamApi(Configuration.GetValue<string>("DownstreamB2CApi:Scopes")?.Split(' '))
                .AddDownstreamWebApi("DownstreamB2CApi", Configuration.GetSection("DownstreamB2CApi"));
```

Then in the AAD controller, you'll need to:
1. Specify the scheme to use in the `[Authorize]` attribute
2. Use the authentication scheme explicitly when:
   - calling IDownstreamWebApi methods (using the authenticationScheme parameter). See [example](https://github.com/AzureAD/microsoft-identity-web/blob/master/tests/MultipleAuthSchemes/Controllers/HomeB2CController.cs#L27)
   - calling the GraphServiceClient requests (using the `.WithAuthenticationScheme` method on the request). See [example](https://github.com/AzureAD/microsoft-identity-web/blob/4668bb3198f9002528eea11e66d2fa43dd645835/tests/MultipleAuthSchemes/Controllers/HomeController.cs#L30)
   - acquiring a token with the ITokenAcquisition methods (using the `authenticationScheme` parameter)

Here is a variation of our test app above, where the authentication scheme is explicitly set for the AAD controller calling Microsoft Graph.

```CSharp 
    [Authorize(AuthenticationSchemes = "openid2")]
    public class HomeController : Controller
    {
        private const string OpenIdScheme = "openid2";
        private readonly ILogger<HomeController> _logger;

        private readonly GraphServiceClient _graphServiceClient;

        public HomeController(ILogger<HomeController> logger, GraphServiceClient graphServiceClient)
        {
             _logger = logger;
            _graphServiceClient = graphServiceClient;
       }

        [AuthorizeForScopes(ScopeKeySection = "DownstreamApi:Scopes", AuthenticationScheme = OpenIdScheme)]
        public async Task<IActionResult> Index()
        {
            var user = await _graphServiceClient.Me.Request()
                .WithAuthenticationScheme(OpenIdScheme).GetAsync();
            ViewData["ApiResult"] = user.DisplayName;

            return View();
        }
```

## Troubleshooting

### Cookie schemes

In the case of Blazor server @sven5 reported that they needed to use cookie authentication as the default authentication scheme, and then pass-in `null` as the `cookieScheme` parameters in AddMicrosoftIdentityWebApp(). For details see [#549](https://github.com/AzureAD/microsoft-identity-web/issues/549#issuecomment-875566884). Thanks to @sven5 for sharing their findings.

The code is then as follows:

```csharp
services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options =>
{
        options.ExpireTimeSpan = new TimeSpan(7, 0, 0, 0);
});

services.AddAuthentication()
       .AddMicrosoftIdentityWebApp(Configuration.GetSection("AzureAd"), Microsoft.Identity.Web.Constants.AzureAd, null);

services.AddAuthentication()
       .AddMicrosoftIdentityWebApp(Configuration.GetSection("AzureAdB2C"), Microsoft.Identity.Web.Constants.AzureAdB2C, null);
```

### Backward compatibility

Support for multiple authentication schemes was introduced in Microsoft.Identity.Web 1.11.0. We've taken a lot of precautions to ensure the backward compatibility, but we didn't think of one scenario:

If you were injecting `IOptions<MicrosoftIdentityOptions>` in a controller before Microsoft.Identity.Web 1.11.0

```CSharp
 public OnboardingController(SampleDbContext dbContext, 
                             IOptions<MicrosoftIdentityOptions> microsoftIdentityOptions, 
                             IConfiguration configuration)
 {
  this.dbContext = dbContext;
  this.microsoftIdentityOptions = microsoftIdentityOptions.Value;
  this.configuration = configuration;
 }
```

you'll now need to inject an `IOptionsMonitor<MicrosoftIdentityOptions>`, and get the value corresponding to the authentication scheme (usually `OpenIdConnectDefaults.AuthenticationScheme` for web apps and `JwtBearerDefaults.AuthenticationScheme` for web APIs)

Therefore we need to have:

```CSharp
 public OnboardingController(SampleDbContext dbContext, 
                             IOptionsMonitor<MicrosoftIdentityOptions> microsoftIdentityOptions,
                             IConfiguration configuration)
 {
  this.dbContext = dbContext;
  this.microsoftIdentityOptions = microsoftIdentityOptions.Get(OpenIdConnectDefaults.AuthenticationScheme);
  this.configuration = configuration;
 }
```
