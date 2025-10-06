## Adding logging

### Enable logging

See how to enable [Logging](Logging)

### Enable web app events diagnostics

To troubleshoot your web app, you can set the `subscribeToOpenIdConnectMiddlewareDiagnosticsEvents` optional boolean to `true` when you call `AddMicrosoftIdentityWebAppAuthentication` or `AddMicrosoftIdentityWebApp`. This displays in the output window the progression of the OpenID connect message through the OpenID Connect middleware (from the reception of the message from Azure Active directory to the availability of the user identity in `HttpContext.User`).

<img alt="OpenIdConnectMiddlewareDiagnostics" src="https://user-images.githubusercontent.com/13203188/62538366-75ac0380-b807-11e9-9ce0-d0eec9381b78.png" width="75%"/>

## If your app works locally, but not when deployed.

### Did you think of adding a redirect URI?

When you develop your application locally, and then deploy it somewhere (for instance to app services), you need to add a new redirect URI for your application as deployed. For instance if you deployed your app to app services, add a redirect URI in your app registration (Azure portal) for the deployed application by replacing `localhost:port` by the URL where your app is deployed in app service, that is something like `https://<your app service name>.azurewebsites.net/signin-oidc`

### If you enabled EasyAuth and get a GraphServiceException InvalidAuthenticationToken

If you get the following exeception:

```Text
Microsoft.Graph.ServiceException: Code: InvalidAuthenticationToken
Message: CompactToken parsing failed with error code: 80049217
```

make sure you've gone through this step: https://docs.microsoft.com/en-us/azure/app-service/scenario-secure-app-access-microsoft-graph-as-user?tabs=azure-resource-explorer%2Ccommand-line#configure-app-service-to-return-a-usable-access-token

### If your app is in a container, or behind proxys or load balancers

If your app uses app-proxy, app services in containers with linux, load balancers etc ..., see [troubleshooting container, proxys, load balancers](Deploying-Web-apps-to-App-services-as-Linux-containers)\

## AADSTS54005: OAuth2 Authorization code was already redeemed....error

If you hit the `AADSTS54005: OAuth2 Authorization code was already redeemed...` error when deploying a blazor web app, you need to add `<component type="typeof(App)" render-mode="Server" />` in the `_Host.cshtml` file, this is due to the pre-rendering done by blazor. See this [Stackoverflow post](https://stackoverflow.com/questions/60996170/blazor-scoped-service-initializing-twice) for more details.