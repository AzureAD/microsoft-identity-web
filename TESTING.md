# How to test the Microsoft.Identity.Web project templates (locally or from a NuGet package)

## Introduction

- Before we release Microsoft.Identity.Web project templates (usually for each release of Microsoft.Identity.Web), we want to make sure that we test them from the release build.
- Before we commit changes in the project templates (under ProjectTemplates\templates) in the Microsoft.Identity.Web repo, we want to make sure that we test them in depth from the repo. Changes can be changes of major versions of package references of Microsoft.Identity.Web, sync from ASP.NET Core templates. This can also help detecting product bugs so we can do the same before releases of Microsoft.Identity.Web.

## Principle

In this article you will:

- Configure the version of the templates to test by setting the MicrosoftIdentityWebVersion environment variable.
- Run a script that will:
  - Generate C# projects corresponding to all the templates in various configurations (no auth, single-org, single-org calling graph, single-org calling web API, Individual B2C, Individual B2C calling web API (for the web API and the Blazorwasm hosted templates, as B2C does not support OBO)).
  - Configure the projects with existing Azure AD and B2C apps and client secrets. This is done by a configuration file named `configuration.json`. You will need to add the client secrets (see below).
  - Build the generated projects (which are grouped in a solution named `test.sln`).
- [Manually test (for now) the generated projects](#How-to-test-the-configured-projects-manually).
- Test projects based on the templates can be generated from two sources:
    - A [NuGet package created from a release build](#How-to-generate-the-test-projects-from-a-NuGet-Package)
    - The [repo directly](#How-to-generate-the-test-projects-for-testing-templates-from-the-local-repo)
  
## How to generate the test projects from a NuGet Package
> For example, testing for a release with a build from AzureDevOps. 

In a Developer Command Prompt:

1. Do a git clone of the repostitory into a short file path.

    `cd C:\`

    `git clone https://github.com/AzureAD/microsoft-identity-web idweb`

2. cd to the root of the repo (for instance `cd C:\idweb`)

3. Set the version of the templates to test.

   `Set MicrosoftIdentityWebVersion=2.4.0`

4. In ProjectTemplates open the Configuration.json file and add the client secrets (or your own config file) .

   `"B2C_Client_ClientSecret": "secret_goes_here",`

   `"AAD_Client_ClientSecret": "secret_goes_here",`
  
   `"AAD_WebApi_ClientSecret": "secret_goes_here"`

5. Build the repo. 

    `dotnet build Microsoft.Identity.Web.sln`

6. Copy the NuGet package containing the templates (Microsoft.Identity.Web.ProjectTemplates.version.nupkg) downloaded from the release build and paste it under the `ProjectTemplates\bin\Debug` folder of the repo.

    The version should be the same as the value of `MicrosoftIdentityWebVersion` you set earlier. Also, if you downloaded the `Packages.zip` file from the  AzureDevOps build and saved it in your Downloads folder before unzipping it, you could run the following command: 
    `copy "%UserProfile%\Downloads\Packages\Packages\Microsoft.Identity.Web.ProjectTemplates.%MicrosoftIdentityWebVersion%.nupkg" ProjectTemplates\bin\Debug`

7. Go to the ProjectTemplates folder `cd ProjectTemplates`

8. Ensure that the NuGet packages that will be restored in the test projects are the ones <b>generated from the release build</b>. For this you can select the corresponding folder in the Visual Studio NuGet package options UI, or just copy the <a href="https://github.com/AzureAD/microsoft-identity-web/blob/master/ProjectTemplates/nuget.config.release-build">Nuget.config.release-build </a> file to nuget.config into the same folder:

    `copy nuget.config.release-build nuget.config`

9. From ProjectTemplates folder, run the `Test-templates.bat` script with an argument to tell the script to pick-up the existing `Microsoft.Identity.Web.ProjectTemplates.%MicrosoftIdentityWebVersion%.nupkg` file instead of regenerating it.

    `Test-templates.bat DontGenerate`
    
10. Don't commit the changes to the `configuration.json` (secrets) and the `NuGet.Config` (folder to pick-up NuGet packages from, as they depend on your local disk layout).

## How to generate the test projects for testing templates from the local repo

In a Developer Command Prompt:

1. cd to the root of the repo (for instance `cd C:\idweb`)

2. Set the version of the templates to test.

   `Set MicrosoftIdentityWebVersion=2.4.0`

3. Add client secrets to the `ProjectTemplates\Configuration.json` file

   `"B2C_Client_ClientSecret": "secret_goes_here",`

   `"AAD_Client_ClientSecret": "secret_goes_here",`
  
   `"AAD_WebApi_ClientSecret": "secret_goes_here"`

4.  Delete the NuGet packages from `ProjectTemplates\bin\Debug` (to be sure to test the right one)

    `del ProjectTemplates\bin\Debug\*.nupkg`
        
5. Build the repo. This builds everything and generates the NuGet packages.

     `dotnet pack Microsoft.Identity.Web.sln` 

6.  Go to the ProjectTemplates folder

    `cd ProjectTemplates`

7. Ensure that the NuGet packages that will be picked-up are the ones generated from the <b>build for the repo</b>. For this you can select the corresponding folders in the Visual Studio NuGet package options UI, or just copy the <a href="https://github.com/AzureAD/microsoft-identity-web/blob/master/ProjectTemplates/nuget.config.local-build#L22-L24">Nuget.config.local-build file</a> to nuget.config into the same folder.

    `copy nuget.config.local-build nuget.config`

8. From ProjectTemplates folder, run the `Test-templates.bat` script:

    `Test-templates.bat`

11. Don't commit the changes to the `configuration.json` (secrets) and the `NuGet.Config` (folder to pick-up NuGet packages from, as they depend on your local disk layout).

## How to test the configured projects manually

Once the projects are generated from the templates, test them manually.

`cd bin\debug\tests`

`Tests.sln`

## Test each project in the solution

- Starting by the no-auth (we don't want to break this scenario)
- Then the AAD simple, AAD with Microsoft Graph, and AAD with web API (the API is really Microsoft Graph so no need to start a web API)
- Then the B2C simple templates
- To test the B2C-calls-web-api templates, you'll need to run the TodoListService of the B2CWebAppCallsWebApi test app in the Microsoft.Identity.Web solution
  - Note that we could do with testing the B2C-calls-web-api against the web API deployed in Azure, but testing it against our test project has the interest of enabling debugging
  - Also, run the TodoListService under IIS Express.

### To test the SingleOrg web APIs

1. Change the `appsettings.json` of the **mvc-single-org-callswebapi** project so that DownstreamApi section becomes:

   ```json
   "DownstreamApi": {
     "BaseUrl": "https://localhost:44351/WeatherForecast",
     "Scopes": "api://a4c2469b-cf84-4145-8f5f-cb7bacf814bc/access_as_user"
    },
   ```

2. In the solution properties change the startup projects to be:

   - **mvc2-singleorg-callswebapi** and **webapi2-singleorg** and run the projects
   - then **mvc2-singleorg-callswebapi** and **webapi2-singleorg-callsgraph** and run the projects
   - then **mvc2-singleorg-callswebapi** and **webapi2-singleorg-callswebapi** and run the projects

### To test the B2C web API

1. Change the `appsettings.json` of the **mvc2-b2c-callswebapi** project so that DownstreamApi section becomes:

   ```json
   "DownstreamApi": {
     "BaseUrl": "https://localhost:44332/WeatherForecast",
     "Scopes": "https://fabrikamb2c.onmicrosoft.com/tasks/access_as_user"
    },
   ```

2. In the solution properties change the startup projects to be:

   - **mvc2-b2c-callswebapi** and **webapi2-b2c** and run the projects.

### To test the SingleOrg Azure function

1. Change the `appsettings.json` of the **mvc-single-org-callswebapi** project so that DownstreamApi section becomes:

   ```json
   "DownstreamApi": {
     "BaseUrl": "http://localhost:7071/api/SampleFunc",
     "Scopes": "api://a4c2469b-cf84-4145-8f5f-cb7bacf814bc/access_as_user"
    },
   ```

2. In the solution properties change the startup projects to be:

   - **mvc-single-org-callswebapi** and **func2-singleorg** and run the projects.
   - **mvc-single-org-callswebapi** and **func2-singleorg-callsgraph** and run the projects.
   - **mvc-single-org-callswebapi** and **func2-singleorg-callswebapi** and run the projects.
