The Microsoft Identity Web library enables [gRPC](https://docs.microsoft.com/dotnet/architecture/cloud-native/grpc) services to work with the Microsoft identity platform, enabling them to process access tokens for both work and school and Microsoft personal accounts, as well as Azure AD B2C.

## Why use Microsoft.Identity.Web in gRPC applications?

Currently, ASP.NET Core 3.1 gRPC templates (`dot net new worker`) creates a new gRPC service, but does not propose to protect it with the Microsoft identity platform. From the point of view of Microsoft.Identity.Web, gRPC services are very similar to web APIs.

This library adds `ServiceCollection` and `AuthenticationBuilder` extension methods for use in the ASP.NET Core web app **Startup.cs** file. These extension methods enable the web app to sign in users with the Microsoft identity platform and, optionally, enable the web app to call APIs on behalf of the signed-in user.

### Using the worker2 project template

It also adds a project template to create a gPRC application: 

- `dotnet new worker2 --auth SingleOrg` for AAD protected services
- `dotnet new worker2 --auth IndividualB2C` for Azure AD B2C protected services

If you use these project templates, you'll get a fully functional application once you have filled in the configuration. 

In the case of AAD protected services, you can also create gRPC services that calls Microsoft Graph or a downstream API.
- `dotnet new worker2 --auth SingleOrg --calls-graph`
- `dotnet new worker2 --auth SingleOrg --called-api-url URL --called-api-scopes SCOPES`
This is not possible with Azure AD B2C, as the service does not allow the on-behalf of flow (web APIs calling downstream APIs)

If you want to add authentication to an existing gRPC app, the following paragraph explain how to modify the code for your application.

## gRPC - Startup.cs

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

To enable the gRPC service to be protected with the Microsoft identity platform:

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
   services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
           .AddMicrosoftWebApi(Configuration);
   services.AddAuthorization();
   ```

1. In the `Configure()` method add authentication and authorization middleware
   ```CSharp
   app.UseAuthentication();
   app.UseAuthorization();
   ```

See also:

- [gRPC tutorial](https://dev.to/425show/secure-grpc-service-with-net-core-and-azure-active-directory-4p6k)

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
   services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
           .AddMicrosoftWebApi(Configuration, "AzureAdB2C");
   ...
  }
  ...
}
```

### Verification of scopes or app roles

In the service methods, gRPC apps needs to verify that the the token used to called them has the right:

- scopes, in the case of APIs called on behalf of a user
- app roles, in the case of APIs called by daemon applications

#### Verify scopes in Web APIs called on behalf of users

A gRPC service that is called on behalf of users needs to verify the scopes in the service method using the `VerifyUserHasAnyAcceptedScope` extension method on the HttpContext.

```CSharp
[Authorize]
[RequiredScope("access_as_user")]
public override Task<HelloReply> SayHello(HelloRequest request, ServerCallContext context)
{
    var httpContext = context.GetHttpContext();
    return Task.FromResult(new HelloReply
    {
         Message = "Hello " + request.Name
    });
}
```

#### gRPC services called by daemon apps (using client credential flow)

A web API that accepts daemon applications needs to:
- either verify the application roles in the controller actions (See [application roles](https://docs.microsoft.com/azure/active-directory/develop/scenario-protected-web-api-verification-scope-app-roles#verify-app-roles-in-apis-called-by-daemon-apps)
- or be protected by an ACL-based authorization pattern to [control tokens without a roles claim](https://docs.microsoft.com/azure/active-directory/develop/v2-oauth2-client-creds-grant-flow#controlling-tokens-without-the-roles-claim)

##### Verification of app roles

```CSharp

[Authorize]
public override Task<HelloReply> SayHello(HelloRequest request, ServerCallContext context)
{
    var httpContext = context.GetHttpContext();
    HttpContext.ValidateAppRole("acceptedRole1")

    return Task.FromResult(new HelloReply
    {
         Message = "Hello " + request.Name
    });
}
```

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
