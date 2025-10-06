# Calling APIs with Managed Identity

Starting from Microsoft.Identity.Web version 2.17.0, apps can use managed identities to acquire a security token, call a downstream API, and/or call Microsoft Graph. This works with both system-assigned and user-assigned identities. If you'd like to learn more about managed identities for Azure resources, [click here](https://learn.microsoft.com/en-us/entra/identity/managed-identities-azure-resources/overview).

## Daemon App Example Without Managed Identity 

The below code is for a simple daemon application to call a downstream API on behalf of the client itself. For more details see [daemon console app calling your own API](https://github.com/Azure-Samples/active-directory-dotnetcore-daemon-v2/tree/master/2-Call-OwnApi)

```csharp
﻿// More here ..

// Get the Token acquirer factory instance. By default it reads an appsettings.json
// file if it exists in the same folder as the app (make sure that the 
// "Copy to Output Directory" property of the appsettings.json file is "Copy if newer").
var tokenAcquirerFactory = TokenAcquirerFactory.GetDefaultInstance();

// Create a downstream API service named 'MyApi' which comes loaded with several
// utility methods to make HTTP calls to the DownstreamApi configurations found
// in the "MyWebApi" section of your appsettings.json file.
tokenAcquirerFactory.Services.AddDownstreamApi("MyApi",
    tokenAcquirerFactory.Configuration.GetSection("MyWebApi"));
var sp = tokenAcquirerFactory.Build();

// Extract the downstream API service from the 'tokenAcquirerFactory' service provider.
var api = sp.GetRequiredService<IDownstreamApi>();

// You can use the API service to make direct HTTP calls to your API. Token
// acquisition is handled automatically based on the configurations in your
// appsettings.json file.
var result = await api.GetForAppAsync<IEnumerable<TodoItem>>("MyApi");
Console.WriteLine($"result = {result?.Count()}");
```

the appsettings.json is something like the following:

```json
{
 "AzureAd": {
   "Instance": "https://login.microsoftonline.com/", 
   "TenantId": "[Enter here the tenantID or domain name for your Azure AD tenant]",
   "ClientId": "[Enter here the ClientId for your application]",
   "ClientCredentials": [
    {
       "SourceType": "ClientSecret",
       "ClientSecret": "[Enter here a client secret for your application]"
    }
   ]
    },

    "MyWebApi": {
       "BaseUrl": "https://localhost:44372/",
       "RelativePath": "api/TodoList",
       "RequestAppToken": true,
       "Scopes": [ "[Enter here the scopes for your web API]" ]  // . E.g. 'api://<API_APPLICATION_ID>/.default'
    }
}
```

## Daemon App Example With Managed Identity 

If you want to access an Azure resource using a managed identity, the recommended way is to use the Azure SDK instead of Id Web. See [DefaultAzureCredentials](https://learn.microsoft.com/en-us/dotnet/api/azure.identity.defaultazurecredential?view=azure-dotnet) for more information.

However, if you use managed identity to call your own downstream API, the API will no longer be called on behalf of the client app, but on behalf of the managed identity (associated with the Azure compute (VM, function, etc ..) running the app. In Microsoft.Identity.Web a managed identity is specified by either configuration or code as shown in the examples below.

### From the configuration

To use a managed identity for authentication via configuration, you'll alter the configuration file (appsettings.json) 

#### User assigned managed identity

To specify a user assigned managed identity, use the following configuration in the appsettings.json file instead of the "AzureAd" section. This information will flow through to any request made using either the DownstreamApi, or AuthorizationHeaderProviderOptions class.


```json
  "AcquireTokenOptions": {
    "ManagedIdentity": {
      "UserAssignedClientId": "GUID-of-the-user-assigned-managed-identity"
    }
  }
```

for instance, the daemon app becomes.

```diff
{
 "AzureAd": {
-   "Instance": "https://login.microsoftonline.com/", 
-   "TenantId": "[Enter here the tenantID or domain name for your Azure AD tenant]",
-   "ClientId": "[Enter here the ClientId for your application]",
-   "ClientCredentials": [
-    {
-       "SourceType": "ClientSecret",
-       "ClientSecret": "[Enter here a client secret for your application]"
-    }
-   ]
    },

    "MyWebApi": {
       "BaseUrl": "https://localhost:44372/",
       "RelativePath": "api/TodoList",
       "RequestAppToken": true,
+      "AcquireTokenOptions": {
+         "ManagedIdentity": {
+             "UserAssignedClientId": "GUID-of-the-user-assigned-managed-identity"
+         }
+       }
       "Scopes": [ "[Enter here the scopes for your web API]" ]  // . E.g. 'api://<API_APPLICATION_ID>/.default'
    }
}
```

the code itself does not change.

#### System assigned managed identity

To use a system assigned managed identity, do the same as above, but don't include the line with `UserAssignedClientId`:

```json
      "AcquireTokenOptions": {
         "ManagedIdentity": {
         }
       }
```

### Specifying managed identity by code

#### with IDownstreamApi

```diff
var api = sp.GetRequiredService<IDownstreamApi>();
- var result = await api.GetForAppAsync<IEnumerable<TodoItem>>("MyApi"); 
+ var result = await api.GetForAppAsync<IEnumerable<TodoItem>>("MyApi", 
+      options => {
+          options.AcquireTokenOptions = new ()
+          {
+            ManagedIdentity = new() {
+            // UserAssignedClientId = "GUID-of-the-user-assigned-managed-identity" if needed.
+          }
+        }
+      });
Console.WriteLine($"result = {result?.Count()}");
```


#### With IAuthorizationProvider, and Microsoft Graph SDK

You'd do the same when using [IAuthorizationProvider.GetAuthorizationProviderForApp()](https://github.com/AzureAD/microsoft-identity-abstractions-for-dotnet/blob/f3328d9b3527baefb93d1957d82ef3bc529dfa13/src/Microsoft.Identity.Abstractions/DownstreamApi/IAuthorizationHeaderProvider.cs#L48C22-L52).

With MicrosoftGraph, you would use the .WithTokenAcquisitionOptions():

```csharp
var users = await _graphServiceClient.Users.GetAsync(r =>
 {
     r.Options.WithAuthenticationOptions(o =>
     {
         // Specify scopes for the request
         o.Scopes = new string[] { "https://graph.microsoft.com/.default" };

         // Specify the ASP.NET Core authentication scheme if needed (in the case
         // of multiple authentication schemes)
         o.AcquireTokenOptions.ManagedIdentity = new()
         {
           // UserAssignedClientId = "GUID-of-the-user-assigned-managed-identity"
         }
     });
 });
```

