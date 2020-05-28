## Web app calling Microsoft Graph using the Graph SDK

This test/sample illustrates a pattern to call Microsoft Graph using the Graph SDK:
-	Configuration of the `SingleTokenAcquisition` and the `MicrosoftGraphBaseUrl` settings in the configuration file: [appsettings.json#L9-11](./appsettings.json#L9-11)
o	Call to AddMicrosoftGraph(Configuration, Scopes, section) in the Startup.cs: [Startup.cs#L33](./Startup.cs#L33) 
o	Graph service injected by dependency injection in the constructor of a PageModel or a Controller: [Pages/Index.cshtml.cs#L20](./Pages/Index.cshtml.cs#L20)
o	Usage in a controller action/ page method: [Pages/Index.cshtml.cs#L26](./Pages/Index.cshtml.cs#L26)

-	For the moment, the AddMicrosoftGraph service is in the sample, but we’d consider adding a new NuGet package: **Microsoft.Identity.Web.MicrosoftGraph**. 
- For the moment, using the public Graph SDK, the AddMicrosoftGraph method uses an implementation of `IAuthenticationProvider`. See [MicrosoftGraphServiceExtensions.cs](./MicrosoftGraphServiceExtensions.cs) and [TokenAcquisitionCredentialProvider.cs](./TokenAcquisitionCredentialProvider.cs). 
  This might change when the Graph SDK GraphClientFactory has more Create methods. But this is an implementation detail.
