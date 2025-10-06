# Creating and configuring web apps with project templates

- You can create applications with the project templates provided by Microsoft Identity Web. This is explained in this article
- Then you can configure your applications using [msidentity-app-sync](https://github.com/AzureAD/microsoft-identity-web/blob/master/tools/app-provisioning-tool/README.md) which is a dotnet global tool that creates/updates Azure AD or Azure AD B2C apps and updates your code configuration.

## Download or build the NuGet package containing the .NET Core template:

You have two ways of installing the templates:
- either from NuGet
- or build them from the repository

### Option 1: Install the templates in dotnet core

You can download the [Microsoft.Identity.Web.ProjectTemplates-1.15.2](https://www.nuget.org/api/v2/package/Microsoft.Identity.Web.ProjectTemplates/1.15.2) NuGet package from NuGet.org.
The following command will install the templates from NuGet.org (or anything referenced as .NET sources, for instance in **NuGet.config**)

```Shell
dotnet new -i Microsoft.Identity.Web.ProjectTemplates::1.15.2
```

### Option 2: Build and install the templates from the repository

Alternatively if you want to build it yourself clone the Microsoft.Identity.Web repo, and then

```Shell
dotnet pack /p:ClientSemVer=1.15.2
cd ProjectTemplates
cd bin
cd Debug
dotnet new -i Microsoft.Identity.Web.ProjectTemplates.1.15.2.nupkg
```
## Overview of the web app templates

![image](https://user-images.githubusercontent.com/13203188/114165522-dff63f00-992c-11eb-8485-c7a94a6b1a7b.png)


## Use the Web app MVC template

Web MVC app (Microsoft identity platform, Single Org)

```Shell
mkdir mvcwebapp
cd mvcwebapp
dotnet new mvc2 --auth SingleOrg
```

Web MVC app (Microsoft identity platform, Multiple Orgs)

```Shell
mkdir mvcwebapp-multi-org
cd mvcwebapp-multi-org
dotnet new mvc2 --auth MultiOrg
```

Web MVC app (Azure AD B2C)

```Shell
mkdir mvcwebapp-b2c
cd mvcwebapp-b2c
dotnet new mvc2 --auth  IndividualB2C
```

Web MVC app calling Microsoft Graph

```Shell
mkdir mvcwebapp-graph
cd mvcwebapp-graph
dotnet new mvc2 --auth  SingleOrg --calls-graph
```

Web MVC app calling a web API

```Shell
mkdir mvcwebapp-calls-api
cd mvcwebapp-calls-api
dotnet new mvc2 --auth  SingleOrg --called-api-url "https://localhost:12345" --called-api-scopes "api://{someguid}/access_as_user"
```

## Use the Web app Razor template

Razor Web app (Microsoft identity platform, Single Org)

```Shell
mkdir webapp
cd webapp
dotnet new webapp2 --auth SingleOrg
```

Razor Web app (Microsoft identity platform, Multiple Orgs)"

```Shell
mkdir webapp-multi-org
cd webapp-multi-org
dotnet new webapp2 --auth MultiOrg
```

Razor Web app Azure AD B2C

```Shell
mkdir webapp-b2c
cd webapp-b2c
dotnet new webapp2 --auth  IndividualB2C
```

Web Razor app calling Microsoft Graph

```Shell
mkdir webapp-graph
cd webapp-graph
dotnet new webapp2 --auth  SingleOrg --calls-graph
```

Web Razor app calling a web API

```Shell
mkdir webapp-calls-api
cd webapp-calls-api
dotnet new webapp2--auth  SingleOrg --called-api-url "https://localhost:12345" --called-api-scopes "api://{someguid}/access_as_user"
```

## Use the web app Blazor server template

Blazor server web app (Microsoft identity platform, Single Org)

```Shell
mkdir blazorserver
cd blazorserver
dotnet new blazorserver2 --auth SingleOrg
```

Blazor server web app (Microsoft identity platform, Multiple Orgs)

```Shell
mkdir blazorserver-multi-org
cd blazorserver-multi-org
dotnet new blazorserver2 --auth MultiOrg
```

Blazor server Web app Azure AD B2C

```Shell
mkdir blazorserver-b2c
cd blazorserver-b2c
dotnet new blazorserver2 --auth IndividualB2C
```

Blazor server web app calling Microsoft Graph

```Shell
mkdir blazorserver-graph
cd blazorserver-graph
dotnet new blazorserver2 --auth  SingleOrg --calls-graph
```

Blazor server web app calling a web API

```Shell
mkdir blazorserver2 -calls-api
cd blazorserver2 -calls-api
dotnet new blazorserver2 --auth  SingleOrg --called-api-url "https://localhost:12345" --called-api-scopes "api://{someguid}/access_as_user"
```

## Use of the Blazor web assembly template

Blazor web assembly - single-org

```Shell
mkdir blazorwasm2-singleorg
cd blazorwasm2-singleorg
dotnet new blazorwasm2 --auth SingleOrg
```

Blazor web assembly single-org, calling Microsoft graph"

```Shell
mkdir blazorwasm2-singleorg-callsgraph
cd blazorwasm2-singleorg-callsgraph
dotnet new blazorwasm2 --auth SingleOrg --calls-graph
```

Blazor web assembly single-org, calling a downstream web API"

```Shell
mkdir blazorwasm2-singleorg-callswebapi
cd blazorwasm2-singleorg-callswebapi
dotnet new blazorwasm2 --auth SingleOrg --called-api-url "https://graph.microsoft.com/beta/me" --called-api-scopes "user.read"
```

Blazor web assembly, single-org, with hosted Blazor web server web API

```Shell
mkdir blazorwasm2-singleorg-hosted
cd blazorwasm2-singleorg-hosted
dotnet new blazorwasm2 --auth SingleOrg  --hosted
```

Blazor web assembly, single-org, with hosted Blazor web server web API calling microsoft graph"

```Shell
mkdir blazorwasm2-singleorg-callsgraph-hosted
cd blazorwasm2-singleorg-callsgraph-hosted
dotnet new blazorwasm2 --auth SingleOrg --calls-graph --hosted
```

Blazor web assembly, single-org, with hosted Blazor web server web API calling a downstream web api

```Shell
mkdir blazorwasm2-singleorg-callswebapi-hosted
cd blazorwasm2-singleorg-callswebapi-hosted
dotnet new blazorwasm2 --auth SingleOrg --called-api-url "https://graph.microsoft.com/beta/me" --called-api-scopes "user.read" --hosted
```

Blazor web assembly, B2C

```Shell
mkdir blazorwasm2-b2c
cd blazorwasm2-b2c
dotnet new blazorwasm2 --auth IndividualB2C
```

Blazor web assembly, B2C, with hosted Blazor web server B2C web API

```Shell
mkdir blazorwasm2-b2c-hosted
cd blazorwasm2-b2c-hosted
dotnet new blazorwasm2 --auth IndividualB2C  --hosted
```

### Example of a fully configured B2C Razor Web app

1. Create the app

   ```shell
   dotnet new webapp2 --auth IndividualB2C --aad-b2c-instance "https://fabrikamb2c.b2clogin.com" --client-id "90c0fe63-bcf2-44d5-8fb7-b8bbc0b29dc6" --domain "fabrikamb2c.onmicrosoft.com" --susi-policy-id "b2c_1_susi" --reset-password-policy-id "b2c_1_reset" --edit-profile-policy-id "b2c_1_edit_profile"  
   ```

2. In the launchSettings.json, change the sslPort to 44316

3. run the Web app: `dotnet run`

4. navigate to `https://localhost:44316` and sign-in to the application

## (optional) Uninstall the templates

Navigate back to `ProjectTemplates\bin\Debug` and run:

```Shell
dotnet new -u Microsoft.Identity.Web.ProjectTemplates
```

## Update dotnet core templates

First un-install:

Navigate back to `ProjectTemplates\bin\Debug` and run:

```Shell
dotnet new -u Microsoft.Identity.Web.ProjectTemplates
```

Then install the new templates:

```Shell
dotnet new -i Microsoft.Identity.Web.ProjectTemplates.1.9.1.nupkg
```
