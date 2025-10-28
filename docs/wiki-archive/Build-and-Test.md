## Build and test

### Clone the repo

1. Navigate to the main page for the [Microsoft Identity Web](https://github.com/AzureAD/microsoft-identity-web) repo.
1. From the GitHub UI, click clone or download.
1. Alternatively open a dev command line and run:

   ```Shell
   git clone https://github.com/AzureAD/microsoft-identity-web.git
   ```

 The project is cloned into a local folder.

### Build it

1. Open **Microsoft.Identity.Web.sln** and build it in Visual Studio 2022. Note the [Troubleshooting building in Visual Studio](#troubleshooting-building-with-visual-studio) section below.

1. Alternatively, open a dev command line and build with

   ```Shell
   dotnet msbuild Microsoft.Identity.Web.sln
   ```

   or just,

   ```Shell
   dotnet msbuild
   ```

### Run unit tests

You won't be able to run the Integration tests because they require access to a Microsoft Key Vault which is locked down. These tests run daily as part of our Azure DevOps pipelines.

To run the unit tests from the assembly **Microsoft.Identity.Web.Test**. For this:

```Shell
cd tests\Microsoft.Identity.Web.Test
dotnet test
```

## Package Microsoft.Identity.Web

From Visual Studio or from the command line. If you wish to control the versioning, use the `p:ClientSemVer` property

```Shell
dotnet pack -p:ClientSemVer=1.0.0
```

If you executed the command above, you'll find the NuGet packages generated under:

- src\Microsoft.Identity.Web\bin\Debug\Microsoft.Identity.Web.1.0.0.nupkg
- src\Microsoft.Identity.Web.UI\bin\Debug\Microsoft.Identity.Web.UI.1.0.0.nupkg
- ProjectTemplates\bin\Debug\Microsoft.Identity.Web.ProjectTemplates.0.x.y.nupkg

The symbols are also generated:

- src\Microsoft.Identity.Web\bin\Debug\Microsoft.Identity.Web.1.0.0.snupkg
- src\Microsoft.Identity.Web.UI\bin\Debug\Microsoft.Identity.Web.UI.1.0.0.snupkg

## Troubleshooting building with Visual Studio

For the moment, Microsoft.Identity.Web leverages a preview version of .NET 8.0. To build in Visual Studio, you will need to enable Visual Studio to use preview SDKs. For this go to **Tools | Options | Environment| Preview features | Use previews of the .NET Core SDK (requires restart)**, and restart Visual Studio.

![image](https://user-images.githubusercontent.com/13203188/84913685-140d2480-b0bb-11ea-836c-ef6c547e381e.png)