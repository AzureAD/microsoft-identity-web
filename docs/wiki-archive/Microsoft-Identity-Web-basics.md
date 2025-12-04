# Why use Microsoft.Identity.Web

Microsoft Identity Web provides the glue between the ASP.NET Core middleware and MSAL .NET to bring a clearer, more robust developer experience, which also leverages the power of the Microsoft identity platform and leverages OpenId Connect middleware, which means developers can develop applications which allow several identity providers, including integration with Microsoft Entra ID, Microsoft Azure AD B2C, and Microsoft Entra External IDs.

Microsoft Identity Web leverages [Microsoft Authentication Library (MSAL)](https://github.com/azuread/microsoft-authentication-library-for-dotnet), which fetches the tokens and provides token cache extensibility.

When you run the following commands:

   `dotnet new webapp --auth` or `dotnet new webapi --auth`

the ASP.NET Core application that is produced uses Microsoft.Identity.Web

In the same way, when you use the File | New project and choose ASP.NET Core web app, or ASP.NET Core web API in Visual Studio, this produces apps that use Microsoft.Identity.Web.


> Historical perspective
> Microsoft.Identity.Web is a simpler way to use Azure AD in ASP.NET Core web apps and web APIs. It doesn't replace ASP.NET Identity in any way, it doesn't replace AddOpenIdConnect, AddJwtBearer or AddCookie or any of the lower level primitives, but it uses and configure them correctly for Azure AD. It doesn't work with non-Azure identity providers. It also replaced AzureAD.UI and AzureADB2C.UI which were obsoleted in .NET 5.0

Here are the available ASP.NET Core project templates and options you can use to create applications: 

![image](https://user-images.githubusercontent.com/13203188/107696478-4acf2500-6cb2-11eb-9e78-2f211cd3f6ab.png)

## High level architecture

Microsoft identity web is a library that provides a higher-level API and coordinates:
- **ASP.Net Core** and its authentication and authorization middleware, 
- **Identity.Model** (validates tokens), 
- **MSAL.NET** (acquires tokens),
- **The Azure SDK** (used to fetch certificates from **KeyVault** using **Managed Identity** when deployed to Azure, or your developer credentials when run on your local dev box)

![image](https://user-images.githubusercontent.com/13203188/110962124-e3f46880-8350-11eb-8023-330646c6c16b.png)
