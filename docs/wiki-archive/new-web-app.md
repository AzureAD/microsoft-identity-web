# Create a new ASP.NET core web app

To create a new ASP.NET Core web app you can either use the command line, or the Visual Studio wizard.

## Create a new ASP.NET Core web app using the command line

You can create a web app that sign-in users. You can also add the capability for your web app to call Microsoft Graph, or any downstream API. Based on what you want to have, use the following commands, from a developer prompt.

|              |                Add web app               |   calls graph   |                  call downstream API                  |
|--------------|:----------------------------------------:|:---------------:|:-----------------------------------------------------:|
| Entra ID or Entra External IDs  | `dotnet new webapp --auth SingleOrg`     | `--calls-graph` | `--called-api-scopes <scopes> --called-api-url <url>` |
| Azure AD B2C | `dotnet new webapp --auth IndividualB2C` |       N/A       | `--called-api-scopes <scopes> --called-api-url <url>` |

ex:
The following command creates a new web app that calls Microsoft graph
```shell
dotnet new webapp --auth SingleOrg --calls-graph
```

The following command creates a new web app that calls a downstream API located at `https://localhost:12345/` accepting a scope `https://myapp.mydomain.com/read`

```shell
dotnet new webapp --auth SingleOrg --called-api-scopes https://myapp.mydomain.com/read --called-api-url https://localhost:12345/
```

You can also replace `webapp` by:
- `mvc` to have controllers and views instead of razor pages
- `razor` (which is the same as webapp)
- `blazorserver` to have a web app with blazor pages.
From the authentication point of view things will be the same.

AFter the command line runs, you 'll have the code for your web app. You now need to map the configuration of this code (in the appsettings.json file) to a app registration in Azure AD (new or existing). For this, check out the [msidentity-app-sync tool](https://github.com/AzureAD/microsoft-identity-web/blob/master/tools/app-provisioning-tool/README.md)


## Create a new ASP.NET Core web app using the Visual Studio wizard

1. In Visual Studio, choose **Create a new project**
1. In the Create a new project dialog, choose ASP.NET Core web app, and press **Next**
1. Provide a project name, a location, and a solution name, and press next
1. in the next dialog, in the Authentication type drop down, choose "Microsoft identity platform"

   <img src="https://user-images.githubusercontent.com/13203188/228958149-99c73e1e-a6e2-47f3-a340-08c35e97175c.png" width="60%" />

   then click **Create**

1. Once the code is generated, the 'Connected services' page automatically opens in Visual Studio, and proposes you to install the donet msidentity tool, that will handle the app registration for you. Click **Next**
1. Choose a tenant where to create an application. Depending on the tenant type (AAD or B2C), your code will be updated to be an AAD or an Azure AD B2C application.
1. Create a new app, or pick an existing app in the tenant.
1. Choose if you want to call Microsoft Graph or not. The code for your application will be updated accordingly
1. If you chose to call an API, the wizard will also provide you with options to store the application secret.
