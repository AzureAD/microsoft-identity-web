> This flow is only supported by AAD, not by AAD B2C

This flow enables web apps to request an additional "spa auth code" from the eSTS /token endpoint, and this authorization code can be redeemed silently by the front end running in the browser. This feature is intended for applications that perform server-side (web apps) and browser-side (SPA) authentication, using Microsoft.Identity.Web, and MSAL.js in the browser (e.g., an ASP.net web application hosting a React single-page application). In these scenarios, the application will likely need authentication both browser-side (e.g., a public client using MSAL.js) and server-side (e.g., a web app using Microsoft.Identity.Web), and each application context will need to acquire its own tokens.

Today, applications using this architecture will first interactively authenticate the user via the web application, and then attempt to silently authenticate the user a second time with the front end spa. Unfortunately, this process is both relatively slow, and the silent network request made client-side (in a hidden iframe) will deterministically fail if third-party cookies are disabled/blocked. By acquiring a second authorization code server-side, MSAL.js can skip hidden iframe step, and immediately redeem the authorization code against the /token endpoint. This mitigates issued caused by third-party cookie blocking, and is also more performant.

For more information on hybrid SPA, see [the documentation here](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/wiki/SPA-Authorization-Code).

For more information on how to configure your hybrid spa to call protected downstream apis see [the documentation here](https://learn.microsoft.com/en-us/entra/identity-platform/scenario-web-app-call-api-app-configuration?tabs=aspnetcore).

## How to enable hybrid spa in Microsoft.Identity.Web?

First, in your Startup.cs, add the following:
```csharp
 services.AddSession(options =>
 {
       options.Cookie.IsEssential = true;
 });
```

and then in `Configure` add:
```csharp
app.UseSession();
```
before 
```csharp
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
```

Next, in your appsettings.json in the AzureAd section, add:
```csharp
"WithSpaAuthCode": true,
```

Finally, in the controller, get the hybrid spa code from the `HttpContext.Session`
```csharp
public async Task<ActionResult> Index()
{
      var value = await _downstreamWebApi.GetForUserAsync<IEnumerable<Todo>>(ServiceName, "api/todolist");
      var code = HttpContext.Session.GetString(Constants.SpaAuthCode);
      // send the spa code in the view to the frontend JS script
      return View(value);
}
```

The hybrid spa code is only redeemable once. It is the responsibility of the backend (MSAL.net / MS Identity Web) to acquire the spa authorization code and provide it to the frontend (MSAL.js), and then MSAL.js will redeem the spa code for its own set of tokens (including an RT which can be used to get more tokens). Once the spa has loaded, it doesn't need another spa authorization code from the backend, as it can get its own authorization code (i.e. once the RT has expired). MSAL.js will not cache/store the spa authorization code once it has been used.

And if the user has already logged in (i.e. have already redeemed the spa auth code), they do not need to go through the hybrid spa flow a second time.