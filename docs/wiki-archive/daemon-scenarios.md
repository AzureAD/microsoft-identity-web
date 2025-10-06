# Daemon scenarios

Microsoft.Identity.Web supports daemon scenarios, that is a console app or worker role, or web app or web API can call a downstream API on behalf of itself instead of on behalf of a user.

## Samples

The following samples demonstrate applications that accesses the Microsoft Graph API or a downstream API with its own identity (with no user).

> | Language /<br/>Platform | Code sample(s) <br/>on GitHub | Auth <br/>libraries | Auth flow |
> | ----------------------- | ----------------------------- | ------------------- | --------- |
> | .NET Core | &#8226; [Call Microsoft Graph](https://github.com/Azure-Samples/active-directory-dotnetcore-daemon-v2/tree/master/1-Call-MSGraph) <br/> &#8226; [Call web API](https://github.com/Azure-Samples/active-directory-dotnetcore-daemon-v2/tree/master/2-Call-OwnApi) <br/> &#8226; [Using managed identity to call MSGraph](https://github.com/Azure-Samples/active-directory-dotnetcore-daemon-v2/tree/master/5-Call-MSGraph-ManagedIdentity) <br/> &#8226; [Using managed identity to call an API](https://github.com/Azure-Samples/active-directory-dotnetcore-daemon-v2/tree/master/6-Call-OwnApi-ManagedIdentity) <br/> &#8226; [Worker role calling an API](https://github.com/AzureAD/microsoft-identity-web/tree/master/tests/DevApps/ContosoWorker) | Microsoft.Identity.Web | Client credentials grant|




```CSharp
public async Task<string> ITokenAcquisition.GetAccessTokenForAppAsync(string scope, string? tenant = null)
```

- The value passed for the `scope` parameter should be the resource identifier (application ID URI) of the resource you want, affixed with the .default suffix. For the Microsoft Graph example, the value is `https://graph.microsoft.com/.default`
This value tells the Microsoft identity platform endpoint that of all the direct application permissions you have configured for your app, the endpoint should issue a token for the ones associated with the resource you want to use. To learn more about the /.default scope, see the [consent](https://docs.microsoft.com/azure/active-directory/develop/v2-permissions-and-consent#the-default-scope) documentation
- The `tenant` parameter is optional and should only be used in the case where your application needs to access resources in several known tenants. If you use this parameter be sure to pass a tenantId (GUID) or a domain name, but not `organizations`, `common` or `consumers`, otherwise you'll get an ArgumentException (IDW10405) see below.

### Troubleshooting

- "IDW10405: 'tenant' parameter should be a tenant ID or domain name, not 'common', 'organizations' or 'consumers'.": means that you have passed a value to the `tenant` parameter, that does not uniquely describe a tenant. You need to pass-in null, or a GUID or a domain name.
- "IDW10404: 'scope' parameter should be of the form 'AppIdUri/.default'." The value of the scope you passed-in does not end with "/.default". See the scope parameters above.

## IDownstreamWebApi.CallWebApiForAppAsync

Your controller or Blazor page or Razor page will inject a IDownstreamWebApi instance, and call:

```CSharp
public Task<HttpResponseMessage> CallWebApiForAppAsync(
            string serviceName,
            Action<DownstreamWebApiOptions>? downstreamWebApiOptionsOverride = null,
            StringContent? content = null);
```

- `serviceName` is the name of the service registered in the Startup.cs by a call to AddDownstreamApi.
- `downstreamWebApiOptionsOverride` accepts a delegate that enables you to override default values passed-in to the underlying token acquisition interface
- `content` is the input sent to the web API you call.

