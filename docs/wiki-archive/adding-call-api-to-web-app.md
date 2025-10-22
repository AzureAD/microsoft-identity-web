# Calling a web API

To call a web API from your web app that signs-in users or your protected web API, you need to add a few lines:
- in the configuration file
- in Startup.cs
- in the controller

When you use Microsoft.Identity.Web, you have three usage options for calling an API:

- Option 1: Call Microsoft Graph with the Microsoft Graph SDK's `GraphServiceClient`
- Option 2: Call an Azure SDK using the Azure with `TokenAcquisitionTokenCredential`
- Option 3: Call a downstream web API with the helper class `IDownstreamWebApi`
- Option 4: Call a downstream web API without the helper class, acquiring a token yourself with `ITokenAcquisition`

See [A web app/API that calls web APIs: Code configuration](https://docs.microsoft.com/en-us/azure/active-directory/develop/scenario-web-app-call-api-app-configuration?tabs=aspnetcore#startupcs) to see what to change in the configuration file, and startup.cs. See also [Using client certificates](https://github.com/AzureAD/microsoft-identity-web/wiki/Certificates#client-certificates) if you want to use certificates instead of client secrests to authenticate your web app/API.

See [A web app/API that calls web APIs: Call a web API](https://docs.microsoft.com/en-us/azure/active-directory/develop/scenario-web-app-call-api-call-api?tabs=aspnetcore) for the changes to make in the controller.

# Advanced scenarios

## Requesting other scopes, tenant, authentication scheme when calling Microsoft Graph

See [Calling Graph](https://github.com/AzureAD/microsoft-identity-web/wiki/calling-graph) to learn how to specify delegated scopes or app permissions, specify a tenant, and or authentication scheme using `.WithScopes`, `.WithScopes(scopes)`, .WithAppOnly(bool, tenantId), and `.WithAuthenticationScheme(authenticationScheme)`

## Customizing the headers (or the HttpMessageRequest) when using `IDownstreamWebApi`

When using IDownstreamWebApi, you can override the Http headers by using the 

```CSharp
string response = await _downstreamWebApi.GetForUser<string>("DownstreamAPI",
   options => {
      options.RelativePath = "me";
      options.CustomizeHttpRequestMessage = message =>
      {
       var headers = message.Headers;
       // Do what you want to change the HttpHeaders.
       // The Authorization header is already populated when the delegate is called
       
      };
   });
```

