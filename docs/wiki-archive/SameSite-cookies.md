SameSite is a standard that aims to prevent cross-site request forgery (CSRF) attacks. Originally drafted in 2016, it was updated in 2019. The latest version not being backwards compatible. The 2016 specification added a SameSite attribute to the HTTP cookies with possible values `Lax` and `Strict`. The 2019 version added a `None` value and set `Lax` as the default. See [links below](https://github.com/AzureAD/microsoft-identity-web/wiki/samesite-cookies#further-reading) for more information.

### Handling incompatible browsers
Since some previous versions of browsers are incompatible with new SameSite behavior, Microsoft Identity Web provides a workaround. [`HandleSameSiteCookieCompatibility`](https://github.com/AzureAD/microsoft-identity-web/blob/2f133d17230bf753acbd7b70ceb5a0a3378adaba/src/Microsoft.Identity.Web/CookiePolicyOptionsExtensions.cs#L23) method in [`CookiePolicyOptionsExtensions`](https://github.com/AzureAD/microsoft-identity-web/blob/2f133d17230bf753acbd7b70ceb5a0a3378adaba/src/Microsoft.Identity.Web/CookiePolicyOptionsExtensions.cs#L15) class verifies if the browser supports the `None` value. If it doesn't, the library tells ASP.NET not to set the SameSite attribute. [`DisallowsSameSiteNone`](https://github.com/AzureAD/microsoft-identity-web/blob/2f133d17230bf753acbd7b70ceb5a0a3378adaba/src/Microsoft.Identity.Web/CookiePolicyOptionsExtensions.cs#L77) method performs the parsing of the user agent. One overload of [`HandleSameSiteCookieCompatibility`](https://github.com/AzureAD/microsoft-identity-web/blob/2f133d17230bf753acbd7b70ceb5a0a3378adaba/src/Microsoft.Identity.Web/CookiePolicyOptionsExtensions.cs#L35) method does allow developers to specify their own implementation.

### Updating cookie options
If a developer wants to modify the behavior of ASP.NET authentication cookie, [`AddMicrosoftIdentityWebApp`](https://github.com/AzureAD/microsoft-identity-web/blob/2f133d17230bf753acbd7b70ceb5a0a3378adaba/src/Microsoft.Identity.Web/WebAppExtensions/WebAppAuthenticationBuilderExtensions.cs#L62) method accepts a configuration action. The code snippet below shows how the authentication cookie can be set to `SameSite=None`.

```csharp
services
    .AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(
        options => { 
            Configuration.Bind("AzureAdB2C");
        }, 
        options => {
            options.Cookie.SameSite = SameSiteMode.None;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.Cookie.IsEssential = true;
        });
```

Alternatively, a `Configure` or `PostConfigure` method can be used to achieve the same result (after the call to `AddMicrosofIdentitytWebApp`)

```CSharp
services.Configure<CookieAuthenticationOptions>(CookieAuthenticationDefaults.AuthenticationScheme, options => {
    options.Cookie.SameSite = SameSiteMode.None;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.IsEssential = true;
});
```

### Further reading

More information can be found in these articles:

- [Work with SameSite cookies in ASP.NET Core](https://docs.microsoft.com/en-us/aspnet/core/security/samesite?view=aspnetcore-3.1)
- [Upcoming SameSite Cookie Changes in ASP.NET and ASP.NET Core](https://devblogs.microsoft.com/aspnet/upcoming-samesite-cookie-changes-in-asp-net-and-asp-net-core/)
- [2019 SameSite spec](https://tools.ietf.org/html/draft-west-cookie-incrementalism-00)
