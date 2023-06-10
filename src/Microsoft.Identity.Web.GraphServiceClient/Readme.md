# Microsoft.Identity.Web.MicrosoftGraph

## Usage

1. Add Microsoft Graph to the service collection. By default, the scopes are set to `User.Read` and the
   BaseUrl is "https://graph.microsoft.com/v1.0". 
   You can change them by passing a delegate to the `AddMicrosoftGraph` method (See below).
   
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
             // Change this to "https://graph.microsoft.com/beta" if you want to use Graph Beta.
             "BaseUrl": "https://graph.microsoft.com/v1.0",

             // Change this to "true" if you want to request an application token (to call graph on 
             // behalf of the application). The scopes will then automatically
             // be ['https://graph.microsoft.com/.default'].
             "RequestAppToken": false

             // Scopes aren't needed if you request an app token.
             "Scopes": ["User.Read", "User.ReadBasic.All"]

             // See https://aka.ms/ms-id-web/downstreamApiOptions for all the properties you can set.
        }
   }
   ```
 
     The code to add Microsoft Graph based on the configuration is:

   ```csharp
   services.AddMicrosoftGraph(options => 
                              services.Configuration.GetSection("DownstreamApis:MicrosoftGraph").Bind(options) );
   ```

   or 

   ```csharp
   services.AddMicrosoftGraph();
   services.Configure<MicrosoftGraphOptions>(options => 
                                             services.Configuration.GetSection("DownstreamApis:MicrosoftGraph"));
   ```


2. Inject the GraphServiceClient from the constructor of controllers.
   ```csharp
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
   If your app registred several authentication schemes, you'll also need to specify
   which to authentication scheme to apply.

   ```csharp
    var mailFolders = await _graphServiceClient.Me.MailFolders.GetAsync(r =>
    {
        r.Options.WithAuthenticationOptions(o =>
        {
            // Specify scopes for the request
            o.Scopes = new string[] { "Mail.Read" };

            // Specify the ASP.NET Core authentication scheme if needed (in the case
            // of multiple authentication schemes)
            o.AuthenticationOptionsName = JwtBearerDefaults.AuthenticationScheme;
        });
    });
    ```
   
   If you call a Graph API on behalf of your application, you'll need to request an application token. You can do this by setting
   ```charp
   int? appsInTenant = await _graphServiceClient.Applications.Count.GetAsync(r =>
   {
    r.Options.WithAuthenticationOptions(o =>
    {
        // Applications require app permissions, hence an app token
        o.AppOnlyToken = true;
    });
   });
   ```

## Migrate from Microsoft.Identity.Web.MicrosoftGraph 2.x to 3.x

### Breaking changes

Microsoft.Identity.Web.MicrosoftGraph 3.x is based on Microsoft.GraphSDK 5.x, which introduces breaking changes.
The Request() method has disappeared.

   ```csharp
   var user = await _graphServiceClient.Me.Request().GetAsync();
   ```

