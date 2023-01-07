## Migrating from DownstreamWebApi to DowstreamRestApi

### History

Microsoft.Identity.Web 1.x had introduced an interface **IDownstreamWebApi** that called an API taking care of the authentication details (getting the token, adding the authorization header, ...). This interface grew organically based on your feature requests, and it became obvious that we needed to make public API breaking changes to enable all the scenarios you needed.

Rather than changing this existing API, the Microsoft.Identity.Web team has decided to build another interface, taking into account all your feedback. **IDownstreamRestApi** was born. We've deprecated the old interface, and the future efforts will be on the new implementation, but this choice should give you time to migrate if you choose to do so.

This article explains:

- how to migrate from **IDownstreamWebApi** to **IDownstreamRestApi**
- what are the differences between **IDownstreamWebApi** and **IDownstreamRestApi**

### How to migrate from IDownstreamWebApi and IDownstreamRestApi

To migrate your existing code using **IDownstreamWebApi** to Microsoft.Identity.Web 2.x and **IDownstreamRestApi** you will need to:

1. add a reference to the Microsoft.Identity.Web.AddDownstreamRestApi NuGet package
1. in the code doing the initialization of the application (usually **startup.cs** or **program.cs**) replace:

   ```csharp
   .AddDownstreamWebApi("serviceName", Configuration.GetSection("SectionName"))
   ```

   by

   ```csharp
   .AddDownstreamRestApi("serviceName", Configuration.GetSection("SectionName"))
   ```

1. in the configuration file (**appsettings.json**), in the section representing the downstream web API, change the **Scopes** value from being a string to being an array of strings:

    ```json
    "DownstreamApi1": {
        "BaseUrl": "https://myapi.domain.com",
        "Scopes": "https://myapi.domain.com/read  https://myapi.domain.com/write"
    },  
    ```
 
    becomes

    ```json
    "DownstreamApi1": {
        "BaseUrl": "https://myapi.domain.com",
        "Scopes": [
            "https://myapi.domain.com/read",
            "https://myapi.domain.com/write" 
        ]
    },  
    ```

    > [!WARNING]
    > If you forget to change the Scopes to an array, when you try to use the IDownstreamRestApi the scopes will appear null, and IDownstreamRestApi will attempt an anonymous (unauthenticated) call to the downstream Rest API, which will result in a 401/unauthenticated.

1. in the controller:

   - add `using namespace Microsoft.Identity.Abstractions`
   - inject `IDownstreamRestApi` instead of `IDownstreamWebApi`
   - Replace `CallWebApiForUserAsync` by `CallRestApiForUserAsync`
   - if you were using one of the method GetForUser, PutForUser, PostForUser, change the string that was expressing the relative path, to a delegate setting this relative path:

     ```csharp
      Todo value = await _downstreamWebApi.GetForUserAsync<Todo>(ServiceName,
                                                                 $"api/todolist/{id}");
     ```

     becomes

     ```csharp
      Todo value = await _downstreamWebApi.GetForUserAsync<Todo>(
           ServiceName,
           options => options.RelativePath = $"api/todolist/{id}";);
     ```

### Example code

The following pull request illustrate the update of the Microsoft.Identity.Web test apps from IDownstreamWebApi to IDownstreamRestApi: [Update test apps and integration tests to DownstreamRestApi#2036](https://github.com/AzureAD/microsoft-identity-web/pull/2036/files).

### Differences between IDownstreamWebApi and IDownstreamRestApi
