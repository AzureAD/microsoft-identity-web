<!--
The following animated image shows how you can build the NuGet package containing the project templates for .NET Core, install them locally, and create a new web API. It also shows the important part of the code. You can open the image in a new tab if you want to get a full resolution image.

![Microsoft Identity Web-2](https://user-images.githubusercontent.com/13203188/81092446-e4d29780-8f00-11ea-993d-b4cea18c8b2e.gif)
-->

# Creating and configuring web APIs with project templates

- You can create applications with the project templates provided by Microsoft Identity Web. This is explained in this article
- Then you can configure your applications using [msidentity-app-sync](https://github.com/AzureAD/microsoft-identity-web/blob/master/tools/app-provisioning-tool/README.md) which is a dotnet global tool that creates/updates Azure AD or Azure AD B2C apps and updates your code configuration.

## Download or build the NuGet package containing the .NET Core template:

You have two ways of installing the templates:
- either from NuGet
- or build them from the repository

## Option 1: Install the templates in dotnet core

You can download the [Microsoft.Identity.Web.ProjectTemplates-1.8.2](https://www.nuget.org/api/v2/package/Microsoft.Identity.Web.ProjectTemplates/1.8.2) NuGet package from NuGet.org.
The following command will install the templates from NuGet.org (or anything referenced as .NET sources, for instance in **NuGet.config**)

```Shell
dotnet new -i Microsoft.Identity.Web.ProjectTemplates::1.8.2
```

## Option 2: Build and install the templates from the repository

Alternatively if you want to build it yourself clone the Microsoft.Identity.Web repo, and then

```Shell
dotnet pack /p:ClientSemVer=1.8.2
cd ProjectTemplates
cd bin
cd Debug
dotnet new -i Microsoft.Identity.Web.ProjectTemplates.1.4.0.nupkg
```

## Overview of the web API project templates

![image](https://user-images.githubusercontent.com/13203188/114165665-0f0cb080-992d-11eb-9c3c-d244fc7a6dee.png)

## Use the web API template

Microsoft identity platform web API

```Shell
mkdir webapi
cd webapi
dotnet new webapi2 --auth SingleOrg
```

Microsoft identity platform web API calling Microsoft Graph

```Shell
mkdir webapi-graph
cd webapi-graph
dotnet new webapi2 --auth SingleOrg --calls-graph
```

Microsoft identity platform web API calling a downstream API

```Shell
mkdir webapi-calls-api
cd webapi-calls-api
dotnet new webapi2 --auth SingleOrg --called-api-url "https://localhost:12345" --called-api-scopes "api://{someguid}/access_as_user"
```

AzureAD B2C B2C

```Shell
mkdir webapi-b2c
cd webapi-b2c
dotnet new webapi2 --auth IndividualB2C
```

[gRPC](grpc) Templates
- `dotnet new worker2 --auth SingleOrg` for AAD protected services
- `dotnet new worker2 --auth IndividualB2C` for Azure AD B2C protected services
- `dotnet new worker2 --auth SingleOrg --calls-graph`
- `dotnet new worker2 --auth SingleOrg --called-api-url URL --called-api-scopes SCOPES`

[Azure Functions](Azure-Functions) Templates
- `dotnet new func2 --auth SingleOrg` for AAD protected services
- `dotnet new func2 --auth IndividualB2C` for Azure AD B2C protected services
- `dotnet new func2 --auth SingleOrg --calls-graph`
- `dotnet new func2 --auth SingleOrg --called-api-url URL --called-api-scopes SCOPES`

## (optional) Uninstall the project templates

```Shell
cd ProjectTemplates
dotnet new -u Microsoft.Identity.Web.ProjectTemplates
