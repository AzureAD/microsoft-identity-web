## How to specify scopes and app-permissions for GraphServiceClient

When you want to call Microsoft.Graph from your web app or web API, you need to:
- specify AddMicrosoftGraph in the startup.cs
- inject GraphServiceClient in the controller, or Razor page or Blazor page.

When you call AddMicrosoftGraph, you specify (by configuration or programmatically) the scopes to request initially. You can request more scopes when
using a GraphServiceClient query, and you can specify that the query needed app permissions (instead of delegated permissions), or be for a specific tenant.

### specify the delegated scopes to use by using `.WithScopes(string[])` after the `Request()`. For instance:

  ```CSharp
  var users = await _graphServiceClient.Users
     .Request()
     .WithScopes("User.Read.All")
     .GetAsync();
  NumberOfUsers = messages.Count;
  ```

### specify that you want to use app permissions 
(that is https://graph.microsoft.com/.default) by using `.WithAppOnly()` after the `Request()`. For instance:

  ```CSharp
  var apps = await _graphServiceClient.Applications
       .Request()
       .WithAppOnly()
       .GetAsync();
  NumberOfApps = apps.Count;
  ```

  This later case requires the admin to have consented to these app-only permissions

### Specify a tenant:

#### Specify a tenant with app only scopes
```CSharp
// Apps in a specific tenant
var apps = await _graphServiceClient.Applications
                .Request()
                .WithAppOnly(true, tenantId)
                .GetAsync();
```

#### Specify a tenant with delegated permissions
         
```CSharp       
 //  messages for signed-in user in a specific tenant
 var messages = await _graphServiceClient.Me.Messages
                .Request()
                .WithAppOnly(false, tenantId)
                .GetAsync();
```