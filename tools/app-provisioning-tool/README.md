# ms-identity-app 
Command line tool that creates Microsoft identity platform applications in a tenant (AAD or B2C) and updates the configuration code of you ASP.NET Core applications (mvc, webapp, blazorwasm, blazorwasm hosted, blazorserver). The tool can also be used to update code from an existing AAD/AAD B2C application.

## Installing/Uninstalling the tool

1. Build the repository and create the NuGet package (from the `src\DotnetTool` folder):
 
   ```Shell
   dotnet pack
   ```
   
2. Run the following in a developer command prompt in the `src\DotnetTool` folder:
   
   ```Shell
   dotnet tool install --global --add-source ./nupkg ms-identity-app
   ```

If later you want to uninstall the tool, just run:
```Shell
dotnet tool uninstall --global ms-identity-app
```

## Pre-requisites to using the tool

Have an AAD or B2C tenant (or both). 
- If you want to add an AAD registration, you are usually already signed-in in Visual Studio in a tenant. If needed you can create your own tenant by following this quickstart [Setup a tenant](https://docs.microsoft.com/azure/active-directory/develop/quickstart-create-new-tenant). But be sure to sign-out and sign-in from Visual Studio or Azure CLI so that this tenant is known in the shared token cache.

- If you want to add a AAD B2C registration you'll need a B2C tenant, and explicity pass it to the `--tenant-id` option of the tool. To create a B2C tenant, see [Create a B2C tenant](https://docs.microsoft.com/azure/active-directory-b2c/tutorial-create-tenant)


## Using the tool

```text
ms-identity-app:
  Creates or updates an AzureAD/Azure AD B2C application, and updates the code, using
   the developer credentials (Visual Studio, Azure CLI, Azure RM PowerShell, VS Code)

Usage:
  ms-identity-app [options]

Options:
  --tenant-id <tenant-id>            Azure AD or Azure AD B2C tenant in which to create/update the app.
                                      If specified, the tool will create the application in the specified tenant.
                                      Otherwise it will create the app in your home tenant ID [default: ]
  --username <username>              Username to use to connect to the Azure AD or Azure AD B2C tenant.
                                      It's only needed when you are signed-in in Visual Studio, or Azure CLI with several identities.
                                      In that case username is used to disambiguate which identity to use. [default: ]
  --client-id <client-id>            Client ID of an existing application from which to update the code. This is
                                      used when you don't want to register a new app in AzureAD/AzureAD B2C, but want to configure the
                                      code from an existing application (which can also be updated by the tool) [default: ]
  --unregister                       Unregister the application, instead of registering it [default: False]
  --folder <folder>                  When specified, will analyze the application code in the specified folder.
                                      Otherwise analyzes the code in the current directory [default: ]
  --client-secret <client-secret>    Client secret to use as a client credential [default: ]
  --version                          Show version information
  -?, -h, --help                     Show help and usage information
```

If you use PowerShell, or Bash, you can also get the completion in the shell, provivided you install [dotnet-suggest](https://www.nuget.org/packages/dotnet-suggest/). See https://github.com/dotnet/command-line-api/blob/main/docs/dotnet-suggest.md on how to configure the shell so that it leverages dotnet-suggest.

## Scenarios


### Registering a new AAD app and configuring the code using your dev credentials

Given existing code which is not yet configured: 
- detects the kind of application (web app, web api, blazor server, blazor web assembly, hosted or not)
- detects the IDP (AAD or B2C*)
- creates a new app registration in the tenant, using your developer credentials if possible (and prompting you otherwise). Ensures redirect URIs are registered for all the launchsettings ports.
- updates the configuration files (and program.cs for Blazor apps)

<table>
 <tr>
  <td>
   <code>
dotnet new webapp --auth SingleOrg
    
ms-identity-app
   </code>
  </td>
  <td>Creates a new app <b>in your home tenant</b> and updates code</td>
 </tr>
 
 <tr>
  <td>
   <code>
dotnet new webapp --auth SingleOrg

ms-identity-app --tenant-id testprovisionningtool.onmicrosoft.com
   </code>
  </td>
  <td>Creates a new app <b>in a different tenant</b> and updates code</td>
 </tr> 
 
  <tr>
  <td>
   <code>
dotnet new webapp --auth SingleOrg

ms-identity-app --username username@domain.com
   </code>
  </td>
  <td>Creates a new app <b>using a different identity</b> and updates code</td>
 </tr> 
 
 </table>
 
 ### Registering a new AzureAD B2C app and configuring the code using your dev credentials

<table>
 <tr>
  <td>
   <code>
dotnet new webapp --auth SingleOrg

ms-identity-app --tenant-id fabrikamb2c.onmicrosoft.com
   </code>
  </td>
  <td>Creates a new Azure AD B2C app and updates code</td>
 </tr> 
 
  <tr>
  <td>
   <code>
dotnet new webapp --auth SingleOrg

ms-identity-app --tenant-id fabrikamb2c.onmicrosoft.com  --username username@domain.com
   </code>
  </td>
  <td>Creates a new app Azure AD B2C app <b>using a different identity</b> and updates code</td>
 </tr> 
 
 </table>
 
 
 ### Configuring code from an existing application
 
 ```Shell
dotnet new webapp --auth SingleOrg

ms-identity-app [--tenant-id <tenantId>] --client-id <clientId>
 ```

 ### Adding code and configuration to an app which is not authentication/authorization enabled yet
 
 This scenario is on the backlog, but not yet supported

## Supported frameworks

The tool supports ASP.NET Core applications created with .NET 5.0 and netcoreapp3.1. In the case of netcoreapp3.1, for blazorwasm applictions, the redirect URI created for the app is a "Web" redirect URI (as Blazor web assembly leverages MSAL.js 1.x in netcoreapp3.1), whereas in net5.0 it's a "SPA" redirect URI (as Blazor web assembly leverages MSAL.js 2.x in net5.0) 

```Shell
dotnet new blazorwasm --auth SingleOrg --framework netcoreapp3.1
ms-identity-app
dotnet run -f netstandard2.1
```
