To troubleshoot your web API, you can set the `subscribeToJwtBearerMiddlewareDiagnosticsEvents` optional boolean to `true` when you call `AddMicrosoftIdentityWebApiAuthentication` or `AddMicrosoftIdentityWebApi` (used to be `AddProtectedWebApi` in Microsoft.Identity.Web 0.1.x). Enabling these diagnostics displays in the output window the progression of the OAuth 2.0 message through the JWTBearer middleware (from the reception of the message from Azure Active directory to the availability of the user identity in `HttpContext.User`).

<img alt="JwtBearerMiddlewareDiagnostics" src="https://user-images.githubusercontent.com/13203188/62538382-7d6ba800-b807-11e9-9540-560e7129197b.png" width="65%"/>

Web API:
```csharp
 services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
              .AddMicrosoftIdentityWebApi(Configuration, "AzureAd", subscribeToJwtBearerMiddlewareDiagnosticsEvents: true)
                  .EnableTokenAcquisitionToCallDownstreamApi()
                      .AddInMemoryTokenCaches();
```

Web app:
```csharp
 services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
              .AddMicrosoftIdentityWebApp(Configuration, "AzureAd", subscribeToOpenIdConnectMiddlewareDiagnosticsEvents: true)
                 .EnableTokenAcquisitionToCallDownstreamApi()
                     .AddDownstreamWebApi("TodoList", Configuration.GetSection("TodoList"))
                     .AddInMemoryTokenCaches();
```
In both cases, you can set a breakpoint in the methods of the  `OpenIdConnectMiddlewareDiagnostics` and `JwtBearerMiddlewareDiagnostics` classes respectively to observe values in the debugger.

Example:
```csharp
Microsoft.Identity.Web.Resource.JwtBearerMiddlewareDiagnostics: Debug: Begin OnMessageReceivedAsync. 
Microsoft.Identity.Web.Resource.JwtBearerMiddlewareDiagnostics: Debug: End OnMessageReceivedAsync. 
Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerHandler: Information: Successfully validated the token.
Microsoft.Identity.Web.Resource.JwtBearerMiddlewareDiagnostics: Debug: Begin OnTokenValidatedAsync. 
Microsoft.Identity.Web.Resource.JwtBearerMiddlewareDiagnostics: Debug: End OnTokenValidatedAsync. 
```