To see Microsoft Identity Web in action, or to learn how to sign-in users with a web app and call a protected web API, use this incremental tutorial on ASP .NET Core web apps which signs-in users (including in your org, many orgs, orgs + personal accounts, sovereign clouds) and calls web APIs (including Microsoft Graph), while leveraging Microsoft Identity Web. [See the incremental tutorial](https://github.com/Azure-Samples/active-directory-aspnetcore-webapp-openidconnect-v2).

- [Web app which signs in users](https://github.com/Azure-Samples/active-directory-aspnetcore-webapp-openidconnect-v2/tree/master/1-WebApp-OIDC)
- [Web app which signs in users and calls Graph](https://github.com/Azure-Samples/active-directory-aspnetcore-webapp-openidconnect-v2/tree/master/2-WebApp-graph-user)
- [Web app which signs in users and calls multiple web APIs](https://github.com/Azure-Samples/active-directory-aspnetcore-webapp-openidconnect-v2/tree/master/3-WebApp-multi-APIs)
- See the [incremental tutorial](https://github.com/Azure-Samples/active-directory-aspnetcore-webapp-openidconnect-v2) for even more samples, including B2C.
- [Azure Storage sample](https://github.com/Azure-Samples/storage-dotnet-azure-ad-msal). See in particular the [controller](https://github.com/Azure-Samples/storage-dotnet-azure-ad-msal/blob/master/Controllers/HomeController.cs), as well as the [TokenAcquisitionTokenCredential](https://github.com/Azure-Samples/storage-dotnet-azure-ad-msal/blob/master/TokenAcquisitionTokenCredential.cs) class which adapts a `ITokenAcquisition` to an Azure SDK `TokenCredential`.
- [Microsoft Graph Web hooks sample (for ASP.NET Core)](https://github.com/microsoftgraph/aspnetcore-webhooks-sample)


If you are interested in web APIs, see [Web-API-Samples](Web-api-Samples)