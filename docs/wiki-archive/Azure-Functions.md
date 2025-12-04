The Microsoft Identity Web library enables [Azure Functions](https://azure.microsoft.com/en-us/services/functions/) to work with the Microsoft identity platform, enabling them to process access tokens for both work and school and Microsoft personal accounts, as well as Azure AD B2C.

## Why use Microsoft.Identity.Web with Azure Functions?

From the point of view of Microsoft.Identity.Web, Azure Functions with HTTP trigger are very similar to web APIs.

This library adds `ServiceCollection` and `AuthenticationBuilder` extension methods for use in the ASP.NET Core web app **Startup.cs** file. These extension methods enable the web app to sign in users with the Microsoft identity platform and, optionally, enable the web app to call APIs on behalf of the signed-in user.

### Using the func2 project template

It also adds a project template to create an Azure Functions application: 

- `dotnet new func2 --auth SingleOrg` for AAD protected services
- `dotnet new func2 --auth IndividualB2C` for Azure AD B2C protected services

If you use these project templates, you'll get a fully functional application once you have filled in the configuration. 

In the case of AAD protected services, you can also create an Azure Function that calls Microsoft Graph or a downstream API.
- `dotnet new func2 --auth SingleOrg --calls-graph`
- `dotnet new func2 --auth SingleOrg --called-api-url URL --called-api-scopes SCOPES`

While the generated code demonstrates the usage of Microsoft.Identity.Web to get a token on the user's behalf using the on-behalf of flow, the code can be easily customized to support other scenarios Azure Functions and Microsoft.Identity.Web support. For instance, asking for an application permission token in a timer triggered Azure function. Additionally, getting a token on user's behalf is not possible with Azure AD B2C, as the service does not allow the on-behalf of flow (web APIs calling downstream APIs).

If you want to add authentication to an existing Azure Functions app, the following paragraph explain how to modify the code for your application.

## Azure Functions - Startup.cs

The `appsettings.json` needs to have a section describing the Microsoft.Identity.Platform application

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

To enable the Azure Function to be protected with the Microsoft identity platform:

1. Add the Microsoft.Identity.Web and Microsoft.Identity.Web.UI NuGet packages
1. Edit the `Startup.cs` file to add the authentication code. Add a constructor to get access to the `IConfiguration` object.
   
   ```CSharp
   public Startup(IConfiguration configuration)
   {
       Configuration = configuration;
   }

   public IConfiguration Configuration { get; }
   ```

   The authentication middleware goes into the `ConfigureServices()` method. Update it with the following code:

   ```CSharp
   services.AddAuthentication(sharedOptions =>
            {
                sharedOptions.DefaultScheme = Microsoft.Identity.Web.Constants.Bearer;
                sharedOptions.DefaultChallengeScheme = Microsoft.Identity.Web.Constants.Bearer;
            })
           .AddMicrosoftWebApi(Configuration);
   ```

1. In the `Configure()` method add authentication and authorization middleware
    ```CSharp
    // Get the azure function application directory. 'C:\whatever' for local and 'd:\home\whatever' for Azure
    var executionContextOptions = builder.Services.BuildServiceProvider()
        .GetService<IOptions<ExecutionContextOptions>>().Value;

    var currentDirectory = executionContextOptions.AppDirectory;

    // Get the original configuration provider from the Azure Function
    var configuration = builder.Services.BuildServiceProvider().GetService<IConfiguration>();

    // Create a new IConfigurationRoot and add our configuration along with Azure's original configuration 
    Configuration = new ConfigurationBuilder()
        .SetBasePath(currentDirectory)
        .AddConfiguration(configuration) // Add the original function configuration 
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
        .Build();

    // Replace the Azure Function configuration with our new one
    builder.Services.AddSingleton(Configuration);

    ConfigureServices(builder.Services);
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
    services.AddAuthentication(sharedOptions =>
            {
                sharedOptions.DefaultScheme = Microsoft.Identity.Web.Constants.Bearer;
                sharedOptions.DefaultChallengeScheme = Microsoft.Identity.Web.Constants.Bearer;
            })
           .AddMicrosoftWebApi(Configuration, "AzureAdB2C");
   ...
  }
  ...
}
```

### Verification of scopes or app roles

In the service methods, Azure Functions apps needs to verify that the the token used to called them has the right:

- scopes, in the case of APIs called on behalf of a user
- app roles, in the case of APIs called by daemon applications

#### Verify scopes in web APIs called on behalf of users

An Azure Functions service that is called on behalf of users needs to verify the scopes in the service method using the `VerifyUserHasAnyAcceptedScope` extension method on the HttpContext or use the `RequiredScope` attribute, which takes directly the scopes to validate, or a key to the configuration settings where to look for these scopes..

```CSharp
[FunctionName("SampleFunc")]
[RequiredScope("access_as_user")]
public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req)
{
     var (authenticationStatus, authenticationResponse) =
                await req.HttpContext.AuthenticateAzureFunctionAsync();
     if (!authenticationStatus)
                return authenticationResponse;

     using var response = await _downstreamWebApi.CallWebApiForUserAsync("DownstreamApi").ConfigureAwait(false);

...
}
```

#### Azure Functions called by daemon apps (using client credential flow)

A web API that accepts daemon applications needs to:
- either verify the application roles in the controller actions (See [application roles](https://docs.microsoft.com/azure/active-directory/develop/scenario-protected-web-api-verification-scope-app-roles#verify-app-roles-in-apis-called-by-daemon-apps))
- or be protected by an ACL-based authorization pattern to [control tokens without a roles claim](https://docs.microsoft.com/azure/active-directory/develop/v2-oauth2-client-creds-grant-flow#controlling-tokens-without-the-roles-claim)


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
