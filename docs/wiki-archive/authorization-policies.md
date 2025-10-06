

## Validating scopes

You have several ways of specifying that scopes are required to call a web API:
- using the `RequiredScopes` on a controller, or a controller action
- defining ASP.NET Core [authorization policies](https://docs.microsoft.com/aspnet/core/security/authorization/policies) at the level of your application, and enforcing them for the app, or on any `[Authorize]` attribute (using the Policy= property of that attribute). Alternatively you can define this policy as the default policy and it would apply to all `[Authorize]` without specifying a particular policy

### With the RequiredScopes attribute

You can use the `RequiredScopes` attribute when you have used Microsoft.Identity.Web `AddMicrosoftIdentityWebApi` as it registers the scope handler.

The `RequiredScopes` have two exclusive parameters:
- scopes (passed directly in the constructor of the attribute
- configuration entry pointing to scopes. You use the `RequiredScopesConfigurationKey` property.

This code snippet expresses that the controller requires

```csharp
[Authorize]
[RequiredScope("access_as_user")]
public class MyController : ApiController
{
}
```

```csharp
[Authorize]
[RequiredScope(RequiredScopesConfigurationKey="AzureAd:Scopes")]
public class MyController : ApiController
{
}
```

### With authorization policies

```csharp
public void ConfigureServices(IServiceCollection services)
{
  services.AddAuthentication(...)
  /// ...

  policy="MyPolicy";
  services.AddAuthorization(options =>
  {
   options.AddPolicy(policy, policyBuilder => {
     policyBuilder.Requirements.Add(new ScopeAuthorizationRequirement() { RequiredScopesConfigurationKey = $"{ConfigSectionName}:Scope" });
    });
  });
```

Instead of adding a requirement you can also use `RequireScope`

```csharp
  policy="MyPolicy";
  services.AddAuthorization(options =>
  {
   options.AddPolicy(policy, policyBuilder => {
     policyBuilder.RequireScope(scopes?.Split(' '));
    });
  });
```

and then on the controller or controller action:

```csharp
[Authorize(Policy="MyPolicy")]
public class MyController : ApiController
{
}
```

If you choose to set the policy as a default policy, you don't need to specify the policy in the Authorize attribute.

```csharp
  policyName="MyPolicy";
  services.AddAuthorization(options =>
  {
   options.AddPolicy(policyName, policyBuilder => {
     policyBuilder.RequireScope(scopes?.Split(' '));
    });
   builder.DefaultPolicy = builder.GetPolicy(policyName);
```

```csharp
[Authorize]
public class MyController : ApiController
{
}
```

## Validating scopes or app permissions

This is the same as for the validation of scopes, but use `RequiredScopeOrAppPermission` attribute, and the ScopeOrAppPermission requirement or policy.


## Filtering tenants

The recommendation is to use the ASP.NET Core [authorization policies](https://docs.microsoft.com/aspnet/core/security/authorization/policies). The following code snippet shows how to ensure that all the request are filtered for users belonging to particular tenants.

```csharp
public void ConfigureServices(IServiceCollection services)
{
  services.AddAuthentication(...)
  /// ...

 // To add authorization
 services.AddAuthorization(builder =>
 {
  string policyName = "user belongs to specific tenant";
  string[] allowedTenants = 
  {
   "14c2f153-90a7-4689-9db7-9543bf084dad",
   "af8cc1a0-d2aa-4ca7-b829-00d361edb652",
   "979f4440-75dc-4664-b2e1-2cafa0ac67d1",
  };

  builder.AddPolicy(policyName, b => {
    b.RequireClaim("http://schemas.microsoft.com/identity/claims/tenantid",
                   allowedTenants);
                });
  builder.DefaultPolicy = builder.GetPolicy(policyName);
 });
}
```

## Other forms of authorization

See [Claims-based authorization in ASP.NET Core](https://docs.microsoft.com/aspnet/core/security/authorization/claims), [Policy-based authorization in ASP.NET Core](https://docs.microsoft.com/aspnet/core/security/authorization/policies)