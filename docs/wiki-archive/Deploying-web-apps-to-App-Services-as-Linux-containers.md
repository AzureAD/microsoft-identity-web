# Issue with HTTP redirect URI whereas HTTPS is registered

## What is the issue?

Normally, Microsoft Identity Web computes the redirect URI automatically depending on the deployed URL.

However, when you deploy web apps to App Services as Linux containers, your application will be called by App Services on an HTTP address, whereas its registered redirect URI in the app registration will be HTTPS.

This means that when a user browses to the web app, they will be redirected to `login.microsoftonline.com` as expected, but with `redirect_uri=http://<your app service name>.azurewebsites.net/signin-oidc` instead of `redirect_uri=https://<your app service name>.azurewebsites.net/signin-oidc`.

## How to fix it?

If you are on Azure App Service (Linux container), just set the following environment variable:
```"ASPNETCORE_FORWARDEDHEADERS_ENABLED"=true```

Otherwise, in order to get the right result, the guidance from the ASP.NET Core team for working with proxies is in [Configure ASP.NET Core to work with proxy servers and load balancers](https://docs.microsoft.com/en-us/aspnet/core/host-and-deploy/proxy-load-balancer). You should address the issue centrally by using `UseForwardedHeaders` to fix the request fields, like scheme.

The container scenario should have been addressed by default in .NET Core 3.0. See [Forwarded Headers Middleware Updates in .NET Core 3.0 preview 6](https://devblogs.microsoft.com/aspnet/forwarded-headers-middleware-updates-in-net-core-3-0-preview-6). If there are issues with this for you, please contact the ASP .NET Core team <https://github.com/dotnet/aspnetcore>, as they will be the right team to assist with this.


## Historical perspective

For more examples of the issue, as well as the history of how Microsoft Identity Web attempted to manage the issue in the past, see [issue #115](https://github.com/AzureAD/microsoft-identity-web/issues/115).

# Issue with scaled out web apps in App Services

## What is the issue?

When scaling out a web app without App Service ARR affinity, after the user signs-in, the code is redirected back and forth between app service backend instances, causing a redirect loop. Enabling [logging](Logging) enables you to see that ASP.NET Core emits the following error:

`Error from RemoteAuthentication: Unable to unprotect the message.State`

This causes an authorization failure and Microsoft.Identity.Web redirects back to Azure AD which re-authenticates the user and redirects them back to the app, this time hitting the other App Service backend instance and causing the same error above.

For details, see https://github.com/AzureAD/microsoft-identity-web/issues/1160

If you enable ARR affinity things go well.

## Why is this happening? state parameter

Upon receiving the response from Azure AD, the ASP.NET Core middleware takes care of validating the ‘state’ parameter to prevent cross-site forgery attack. The OpenID Connect OWIN middleware use .NET Data Protection API to encrypt the value stored in the ‘state’ parameter. However, data protection (DP) keys are not automatically sync'd across backend App Service instances of the same app

## How to fix?

Add your own encryption key shared across instances, for instance hosted in blob storage and secured via Microsoft Key Vault. By ensuring the DP key is shared across all instances of the Linux App Service the Open ID Connect message `state` parameter is being properly decrypted on any backend instance of the web app. 

Example of the startup.cs:

```CSharp
services.AddDataProtection()
    .SetApplicationName("MyApp")
    .SetDefaultKeyLifetime(TimeSpan.FromDays(30))
    .PersistKeysToAzureBlobStorage(new Uri("https://mystore.blob.core.windows.net/keyrings/master.xml"), new DefaultAzureCredential())
    .ProtectKeysWithAzureKeyVault(new Uri("https://myvault.vault.azure.net/keys/MasterEncryptionKey"), new DefaultAzureCredential());
```

# Issues with load balancing across multiple regions, using Front Door

See [Azure AD issues with load balancing across multiple regions](https://stackoverflow.microsoft.com/questions/234690) on stack overflow. 

The user sometimes get the error: 

```
Status.AppServices.Middleware.ProductionExceptionMiddleware: Unhandled exception occurredSystem.Exception: An error was encountered while handling the remote login.
---> System.Exception: Unable to unprotect the message.State.
```

To fix it, set up an additional HTTP-only routing rule in Front Door that redirects all HTTP traffic to the HTTPS-exclusive rule

## Troubleshooting

See [Configure ASP.NET Core to work with proxy servers and load balancers | Troubleshooting](https://docs.microsoft.com/aspnet/core/host-and-deploy/proxy-load-balancer?view=aspnetcore-5.0#troubleshoot)

For troubleshooting Azure front door, see also https://github.com/AzureAD/microsoft-identity-web/issues/1076#issuecomment-808707902

For troubleshooting with Azure AD Application Gateway, see https://github.com/AzureAD/microsoft-identity-web/issues/1199

### If you are running in Azure ARC
Try setting the environment variable `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true` on your service. This is something done by default in the Asp.Net Core container image, but it might not be happening in the ARC containers.
More background:
[Forward the scheme for Linux and non-IIS reverse proxies](https://docs.microsoft.com/en-us/aspnet/core/host-and-deploy/proxy-load-balancer?view=aspnetcore-6.0#forward-the-scheme-for-linux-and-non-iis-reverse-proxies). See also https://github.com/AzureAD/microsoft-identity-web/issues/1792