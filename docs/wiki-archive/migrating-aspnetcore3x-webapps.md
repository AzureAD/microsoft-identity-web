# Migrating ASP.NET Core 3.1 web apps to Microsoft identity web


### Web apps that sign in users - Startup.cs

#### With Azure AD

ASP.NET Core 3.1 web app templates (`dot net new mvc -auth`) create web apps that sign in users with the Azure AD v1.0 endpoint, allowing users to sign in with their organizational accounts (also called *Work or school accounts*).

The Microsoft.Identity.Web library adds `ServiceCollection` and `AuthenticationBuilder` extension methods for use in the ASP.NET Core web app **Startup.cs** file. These extension methods enable the web app to sign in users with the Microsoft identity platform and, optionally, enable the web app to call APIs on behalf of the signed-in user.


##### Migrating from previous versions / adding authentication
Assuming you have a similar configuration in `appsettings.json` to enable the web app:

```Json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "Domain": "msidentitysamplestesting.onmicrosoft.com",
    "TenantId": "7f58f645-c190-4ce5-9de4-e2b7acd2a6ab",
    "ClientId": "86699d80-dd21-476a-bcd1-7c1a3d471f75",
    "CallbackPath": "/signin-oidc",
    "SignedOutCallbackPath ": "/signout-callback-oidc",

    // Only if you want to call an API
    "ClientSecret": "[Copy the client secret added to the app from the Azure portal]"
  },
...
}
```

To enable users to sign in with the Microsoft identity platform:

1. Add the Microsoft.Identity.Web and Microsoft.Identity.Web.UI NuGet packages (currently in Preview)
2. Remove the AzureAD.UI and AzureADB2C.UI NuGet packages
3. Replace this code in your web application's *Startup.cs* file:

   ```CSharp
   using Microsoft.Identity.Web;

   public class Startup
   {
     ...
     public void ConfigureServices(IServiceCollection services)
     {
      ...
      services.AddAuthentication(AzureADDefaults.AuthenticationScheme)
           .AddAzureAD(options => Configuration.Bind("AzureAd", options));
      ...
     }
     ...
   }
   ```

   ... by the following code:

   ```CSharp
   using Microsoft.Identity.Web;

   public class Startup
   {
     ...
     public void ConfigureServices(IServiceCollection services)
     {
      ...
         services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
                 .AddMicrosoftIdentityWebApp(Configuration);
      ...
     }
     ...
   }

   ```

##### Using the sign-in/sign-out UI

   This method adds authentication with the Microsoft identity platform. This includes validating the token in all scenarios (single- and multi-tenant applications) in the Azure public and national clouds.


You also need to call AddMicrosoftIdentityUI() if you want to benefit from the sign-in / sign-out.

For instance for Razor pages, you'd want something like the following in `Startup.ConfigureServices(IServiceCollection services)`:

```CSharp
  services.AddRazorPages().AddMvcOptions(options =>
  {
   var policy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build();
                options.Filters.Add(new AuthorizeFilter(policy));
  }).AddMicrosoftIdentityUI();
```

Finally, in `Startup.Configure(IApplicationBuilder app, IWebHostEnvironment env)` you want to make sure that you map the controllers which are provided by Microsoft.Identity.Web.UI. 

```CSharp
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
     // More code

     app.UseAuthentication();
     app.UseAuthorization();

     // More code
     app.UseEndpoints(endpoints =>
     {
      endpoints.MapRazorPages();  // If Razor pages
      endpoints.MapControllers(); // Needs to be added
     });
    }
```

#### With Azure AD B2C

The principle is the same, except that the appsettings.json has generally a section named "AzureAdB2C" (but you can choose the name you want, provided it's consistent with what you use in the `AddMicrosoftIdentityWebApp` method, and you need to declare policies.

```json
{
  "AzureAdB2C": {
    "Instance": "https://fabrikamb2c.b2clogin.com",
    "ClientId": "fdb91ff5-5ce6-41f3-bdbd-8267c817015d",
    "Domain": "fabrikamb2c.onmicrosoft.com",
    "SignedOutCallbackPath": "/signout/B2C_1_susi",
    "SignUpSignInPolicyId": "b2c_1_susi",
    "ResetPasswordPolicyId": "b2c_1_reset",
    "EditProfilePolicyId": "b2c_1_edit_profile", // Optional profile editing policy
```

The startup.cs file is then:

```CSharp
using Microsoft.Identity.Web;

public class Startup
{
  ...
  public void ConfigureServices(IServiceCollection services)
  {
   ...
   services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
      .AddMicrosoftIdentityWebApp(Configuration, "AzureAdB2C");
   ...
  }
  ...
}
```

See also:

- [ASP.NET Core Web app incremental tutorial](https://github.com/Azure-Samples/active-directory-aspnetcore-webapp-openidconnect-v2/tree/master/1-WebApp-OIDC/1-5-B2C) chapter 1.5, *Sign in users with Azure AD B2C*
