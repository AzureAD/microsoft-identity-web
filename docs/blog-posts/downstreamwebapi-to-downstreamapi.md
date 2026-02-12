## Migrating from DownstreamWebApi to DownstreamApi

### History

Microsoft.Identity.Web 1.x had introduced an interface **IDownstreamWebApi** that called an API taking care of the authentication details (getting the token, adding the authorization header, ...). This interface grew organically based on your feature requests, and it became obvious that we needed to make public API breaking changes to enable all the scenarios you have asked for over the past couple of years.

Rather than changing this existing API, the Microsoft.Identity.Web team has decided to build another interface, taking into account all your feedback. **IDownstreamApi** was born. We've deprecated the old interface, and the future efforts will be on the new implementation, but this choice should give you time to migrate if you choose to do so.

This article explains:

- how to migrate from **IDownstreamWebApi** to **IDownstreamApi**
- what are the differences between **IDownstreamWebApi** and **IDownstreamApi**

### How to migrate from IDownstreamWebApi and IDownstreamApi

To migrate your existing code using **IDownstreamWebApi** to Microsoft.Identity.Web 2.x and **IDownstreamApi** you will need to:

1. add a reference to the Microsoft.Identity.Web.DownstreamApi NuGet package
1. in the code doing the initialization of the application (usually **startup.cs** or **program.cs**) replace:

   ```csharp
   .AddDownstreamWebApi("serviceName", Configuration.GetSection("SectionName"))
   ```

   by

   ```csharp
   .AddDownstreamApi("serviceName", Configuration.GetSection("SectionName"))
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
    > If you forget to change the Scopes to an array, when you try to use the IDownstreamApi the scopes will appear null, and IDownstreamApi will attempt an anonymous (unauthenticated) call to the downstream API, which will result in a 401/unauthenticated.

1. in the controller:

   - add `using namespace Microsoft.Identity.Abstractions`
   - inject `IDownstreamApi` instead of `IDownstreamWebApi`
   - Replace `CallWebApiForUserAsync` by `CallApiForUserAsync`
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

The following sample illustrates the usage of IDownstreamApi: [ASP.NET Core web app calling web API/TodoListController](https://github.com/AzureAD/microsoft-identity-web/pull/2036/files).

### Differences between IDownstreamWebApi and IDownstreamApi
