﻿# Microsoft.Identity.Web.GraphServiceClient

Microsoft.Identity.Web 1.12 adds a couple of new libraries to call Microsoft Graph and Microsoft Graph Beta
using the version 5 of the Microsoft Graph SDK, which has breaking changes with respect to Microsoft SDK 4.x and earlier.
These new libraries Microsoft.Identity.Web.GraphServiceClient and Microsoft.Identity.Web.GraphServiceClientBeta are an alternative 
to the legacy Microsoft.Identity.Web.MicrosoftGraph and Microsoft.Identity.Web.MicrosoftGraphBeta NuGet packages, 
which are based on Microsoft Graph SDK 4.x.

Microsoft.Identity.Web.GraphServiceClient enables you to benefit from the latest features of the Microsoft Graph SDK,
including a simplified fluent API, and the possibility to use both Microsoft Graph and Microsoft Graph Beta in the same application.

Migrating from Microsoft.Identity.Web.MicrosoftGraph 2.x to Microsoft.Identity.Web.GraphServiceClient requires moving some of your code as
discussed in []()

## Usage

1. Reference Microsoft.Identity.Web.GraphServiceClient in your project.

1. In the startup method, add Microsoft Graph support to the service collection. 
   By default, the scopes are set to `User.Read` and the BaseUrl is "https://graph.microsoft.com/v1.0". 
   You can change them by passing a delegate to the `AddMicrosoftGraph` method (See below).

   Use the following namespace.
   ```csharp
   using Microsoft.Identity.Web;
   ```
   
   Add the Microsoft graph

   ```csharp
   services.AddMicrosoftGraph();
   ```

   or, if you have described Microsoft Graph options in your configuration file:
   ```json
   "AzureAd":
   {
    // more here
   },

   "DownstreamApis":
   {
     "MicrosoftGraph":
        {
            // Specify BaseUrl if you want to use Microsoft graph in a national cloud.
            // See https://learn.microsoft.com/graph/deployments#microsoft-graph-and-graph-explorer-service-root-endpoints
            // "BaseUrl": "https://graph.microsoft.com/v1.0",

            // Set RequestAppToken this to "true" if you want to request an application token (to call graph on 
            // behalf of the application). The scopes will then automatically
            // be ['https://graph.microsoft.com/.default'].
            // "RequestAppToken": false

            // Set Scopes to request (unless you request an app token).
            "Scopes": ["User.Read", "User.ReadBasic.All"]

            // See https://aka.ms/ms-id-web/downstreamApiOptions for all the properties you can set.
        }
   }
   ```
 
   The code to add Microsoft Graph based on the configuration is:

   ```csharp
   services.AddMicrosoftGraph();
   services.Configure<MicrosoftGraphOptions>(options => 
                                             services.Configuration.GetSection("DownstreamApis:MicrosoftGraph"));
   ```

   or 

   ```csharp
   services.AddMicrosoftGraph(options => 
                              services.Configuration.GetSection("DownstreamApis:MicrosoftGraph").Bind(options) );
   ```

2. Inject the GraphServiceClient from the constructor of controllers.
   ```csharp
   using Microsoft.Graph;   

   public class HomeController : Controller
   {
       private readonly GraphServiceClient _graphServiceClient;
       public HomeController(GraphServiceClient graphServiceClient)
       {
           _graphServiceClient = graphServiceClient;
       }
   }
   ```

3. Use Microsoft Graph SDK to call Microsoft Graph. For example, to get the current user's profile:
   ```csharp
   var user = await _graphServiceClient.Me.GetAsync();
   ```

4. You can override the default options in the GetAsync(), PostAsync() etc.. methods. 
   For example to get the mail folders of the current user, you'll need to request more scopes ("Mail.Read"). 
   If your app registred several authentication schemes in ASP.NET Core, you'll also need to specify
   which to authentication scheme to apply.

   ```csharp
    var mailFolders = await _graphServiceClient.Me.MailFolders.GetAsync(r =>
    {
        r.Options.WithScopes("Mail.Read")
                 .WithAuthenticationScheme(JwtBearerDefaults.AuthenticationScheme);
    });
    ```

   You could also write the same code as follows, which is more verbose, but enables you to set several options at once: 

   ```csharp
    var mailFolders = await _graphServiceClient.Me.MailFolders.GetAsync(r =>
    {
        r.Options.WithAuthenticationOptions(o =>
        {
            // Specify scopes for the request
            o.Scopes = new string[] { "Mail.Read" };

            // Specify the ASP.NET Core authentication scheme if needed (in the case
            // of multiple authentication schemes)
            o.AcquireTokenOptions.AuthenticationOptionsName = JwtBearerDefaults.AuthenticationScheme;
        });
    });
    ```
   
   If your app calls the Graph API on behalf of itself, you'll need to request an application token. 
   You do this by setting WithAppOnly. For instance to get the number of applications in the tenant:

   ```charp
   int? appsInTenant = await _graphServiceClient.Applications.Count.GetAsync(
                                                                    r => r.Options.WithAppOnly() );
   ```

   which is a shortcut for:
 
   ```charp
   int? appsInTenant = await _graphServiceClient.Applications.Count.GetAsync(r =>
   {
    r.Options.WithAuthenticationOptions(o =>
    {
        // Applications require app permissions, hence an app token
        o.RequestAppToken = true;
    });
   });
   ```

## You can now use both Microsoft Graph and Microsoft Graph Beta

You can now use both Microsoft Graph and Microsoft Graph Beta in the same application:

1. Reference both Microsoft.Identity.Web.GraphServiceClient and Microsoft.Identity.Web.GraphBetaServiceClient in your project

1. In the startup method, add Microsoft Graph and Graph Beta to the service collection:
 
    ```csharp
    services.AddMicrosoftGraph();
    services.AddMicrosoftGraphBeta();
    ```

1. In the controller or wherever you want to use them declare both GraphServiceClient and GraphBetaServiceClient
    and inject them in the constructor:
       
    ```csharp
    using GraphServiceClient = Microsoft.Graph.GraphServiceClient;
    using GraphBetaServiceClient = Microsoft.Graph.GraphBetaServiceClient;
    ```

## Migrate from Microsoft.Identity.Web.MicrosoftGraph 2.x to Microsoft.Identity.Web.GraphServiceClient

Microsoft.Identity.Web.GraphServiceClient is based on Microsoft.GraphSDK 5.x, which introduces breaking changes.
The Request() method has disappeared, and the extension methods it enabled are now part moved to the GetAsync(), GetPost(), etc methods.

If you don't want to change your code, you can still use the Request() method by adding Microsoft.Identity.Web.MicrosoftGraph to your project
instead of Microsoft.Identity.Web.GraphServiceClient. This package is based on Microsoft.GraphSDK 4.x.

```csharp
var user = await _graphServiceClient.Me.Request().GetAsync();
```

becomes with Microsoft.Graph 5.x

```csharp
var user = await _graphServiceClient.Me.GetAsync();
```

Here how to migrate from Microsoft.Identity.Web.MicrosoftGraph to Microsoft.Identity.Web.GraphServiceClient.

#### WithScopes()

```csharp
var messages = await _graphServiceClient.Users
                .Request()
                .WithScopes("User.Read.All")
                .GetAsync();
int NumberOfUsers = messages.Count;
```

With Microsoft.Identity.Web.GraphServiceClient, you need to call WithScopes() on the options of the builder.

```csharp
var messages = await _graphServiceClient.Users
                .GetAsync(b => b.Options.WithScopes("User.Read.All"));
int NumberOfUsers = messages.Value.Count;
```

#### WithAppOnlyToken()

In Microsoft.Identity.Web.MicrosoftGraph 2.x, you could request an application token by calling WithAppOnlyToken().

```csharp
var messages = await _graphServiceClient.Users
                .Request()
                .WithAppOnly()
                .GetAsync();
int NumberOfUsers = messages.Count;
```

With Microsoft.Identity.Web.GraphServiceClient, you need to call WithAppOnly() on the options of the builder.

```csharp
var messages = await _graphServiceClient.Users
                .GetAsync(b => b.Options.WithAppOnly() ));
int NumberOfUsers = messages.Value.Count;
```

#### WithAuthenticationOptions() in ASP.NET Core applications.

If you are using Microsoft.Identity.Web.MicrosoftGraph in an ASP.NET Core application, you can specify the authentication scheme
to use by calling WithAuthenticationScheme().

```csharp
var messages = await _graphServiceClient.Users
                .Request()
                .WithAuthenticationScheme(JwtBearerDefaults.AuthenticationScheme)
                .GetAsync();
int NumberOfUsers = messages.Count;
```

With Microsoft.Identity.Web.GraphServiceClient, this becomes:

```csharp
var messages = await _graphServiceClient.Users
                .GetAsync(b => b.Options.WithAuthenticationScheme(JwtBearerDefaults.AuthenticationScheme) ));
int NumberOfUsers = messages.Value.Count;
```