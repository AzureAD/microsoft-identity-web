# How to test the Microsoft.Identity.Web project templates (locally or from a NuGet package)

## Introduction

- Before we release Microsoft.Identity.Web project templates (usually for each release of Microsoft.Identity.Web), we want to make sure that we test them from the release build.
- Before we commit changes in the project templates (under ProjectTemplates\templates) in the Microsoft.Identity.Web repo, we want to make sure that we test them in depth from the repo. Changes can be changes of major versions of package references of Microsoft.Identity.Web, sync from ASP.NET Core templates. This can also help detecting product bugs so we can do the same before releases of Microsoft.Identity.Web.

## Principle

In this article you will:

- Configure the version of the templates to test by setting the ClientSemVer environment variable.
- Run a script that will:
  - Generate C# projects corresponding to all the templates in various configurations (no auth, single-org, single-org calling graph, single-org calling web API, Individual B2C, Individual B2C calling web API (for the web API and the Blazorwasm hosted templates, as B2C does not support OBO)).
  - Configure the projects with existing Azure AD and B2C apps and client secrets. This is done by a configuration file named `configuration.json`. You will need to add the client secrets (see below).
  - Build the generated projects (which are grouped in a solution named `test.sln`).
- Manually test (for now) the generated projects.

## How to generate the test projects

> If testing a release build, start with step 6 in "Testing the templates from a Nuget package", then go to step 4, and add the secrets.

In a Developer Command Prompt:

1. cd to the root of the repo (for instance `cd C:\gh\microsoft-identity-web`)

2. Set the version of the templates to test.

   `Set ClientSemVer=1.1.0`

3. Change the directory to ProjectTemplates

   `cd ProjectTemplates`

4. Add client secrets to the Configuration.json file

   `"B2C_Client_ClientSecret": "sercret_goes_here",`

   `"AAD_Client_ClientSecret": "sercret_goes_here",`
  
   `"AAD_WebApi_ClientSecret": "sercret_goes_here"`

5. Go back to the root of the repo

   `cd ..`

   Then perform the following steps. They are different depending on whether you test the templates from the repo, or from a NuGet package that you downloaded (release build)

   <table border = "2">
    <tr>
        <th>If you are testing the templates from the local repo</th>
        <th>If you are testing the templates from a NuGet package</th>
    </tr>
    <tr>
        <td>6. Delete the NuGet packages from ProjectTemplates\bin\Debug (to be sure to test the right one)</td>
        <td>6. Do a git clone of the repostitory into a short file path. </td>
    </tr>
    <tr>
        <td><code>del ProjectTemplates\bin\Debug\*.nupkg</code></td>
        <td><code>cd C:\ </code><br />
        <code>mdkir git</code><br />
        <code>cd C:\git</code><br />
        <code>git clone https://github.com/AzureAD/microsoft-identity-web idweb</code></td>
    </tr>
    <tr>
        <td>7. Build the repo. This builds everything and generates the NuGet packages</td>
        <td>7. Copy the NuGet package containing the templates (Microsoft.Identity.Web.ProjectTemplates.version.nupkg) downloaded from the release build and paste it under the <code>ProjectTemplates\bin\Debug</code> folder of the repo. 
        
    The version should be the same as the value of <code>ClientSemVer</code> you set in step For instance if you downloaded the <code>Packages.zip</code> file from the  AzureDevOps build and saved it in your Downloads folder before unzipping it, you could run the following command: </td>
    </tr>
    <tr>
        <td><code>dotnet pack Microsoft.Identity.Web.sln</code></td>
        <td><code>mkdir ProjectTemplates\bin\Debug

    copy "%UserProfile%\Downloads\Packages\Packages\Microsoft.Identity.Web.ProjectTemplates.%ClientSemVer%.nupkg" ProjectTemplates\bin\Debug</code></td>
    </tr>
    <tr>
        <td>8. Go to the ProjectTemplates folder</td>
        <td>8. Go to the ProjectTemplates folder</td>
    </tr>
    <tr>
        <td><code>cd ProjectTemplates</code></td>
        <td><code>cd ProjectTemplates</code></td>
    </tr>
    <tr>
        <td>9. Ensure that the NuGet packages that will be picked-up are the ones generated from the <b>build for the repo</b>. For this you can select the corresponding folders in the Visual Studio NuGet package options UI, or just copy the <a href="https://github.com/AzureAD/microsoft-identity-web/blob/master/ProjectTemplates/nuget.config.local-build#L22-L24">Nuget.config.local-build file</a> to nuget.config into the same folder</td>
        <td>9. Ensure that the NuGet packages that will be restored in the test projects are the ones <b>generated from the release build</b>. For this you can select the corresponding folder in the Visual Studio NuGet package options UI, or just copy the <a href="https://github.com/AzureAD/microsoft-identity-web/blob/master/ProjectTemplates/nuget.config.release-build">Nuget.config.release-build </a> file to nuget.config into the same folder</td>
    </tr>
    <tr>
        <td>
        <p>
        <code>copy nuget.config.local-build nuget.config</code>
        </td>
        <td><p><code>copy nuget.config.release-build nuget.config</code></td>
    </tr>
    <tr>
        <td>10. From ProjectTemplates folder, run the <code>Test-templates.bat</code> script:</td>
        <td>10. From ProjectTemplates folder, run the <code>Test-templates.bat</code> script with an argument to tell the script to pick-up the existing <code>Microsoft.Identity.Web.ProjectTemplates.%ClientSemVer%.nupkg</code> file instead of regenerating it. </td>
    </tr>
    <tr>
        <td><code>Test-templates.bat</code></td>
        <td><code>Test-templates.bat DontGenerate</code></td>
    </tr>
   </table>

11. Don't commit the changes to the `configuration.json` (secrets) and the `NuGet.Config` (folder to pick-up NuGet packages from, as they depend on your local disk layout).

## How to test the configured projects manually

Once the projects are generated from the templates, test them manually.

`cd bin\debug\tests`

`Tests.sln`

Test each project in the solution:

- Starting by the no-auth (we don't want to break this scenario)
- Then the AAD simple, AAD with Microsoft Graph, and AAD with web API (the API is really Microsoft Graph so no need to start a web API)
- Then the B2C simple templates
- To test the B2C-calls-web-api templates, you'll need to run the TodoListService of the B2CWebAppCallsWebApi test app in the Microsoft.Identity.Web solution
  - Note that we could do with testing the B2C-calls-web-api against the web API deployed in Azure, but testing it against our test project has the interest of enabling debugging
  - Also, run the TodoListService under IIS Express.
- To test the web APIs templates … TBD …
